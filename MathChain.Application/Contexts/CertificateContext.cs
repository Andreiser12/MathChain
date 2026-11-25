using System.Security.Cryptography;
using System.Text;
using MathChain.Application.DTOs;
using MathChain.Application.Practice;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Exceptions;
using MathChain.Domain.Gateways;
using MathChain.Domain.Interfaces;

namespace MathChain.Application.Contexts
{
    public class CertificateContext : ICertificateContext
    {
        private const int MaximumCertificatesPerTransaction = 50;
        private const int MaximumTitleLength = 200;
        private const string NotConfiguredMessage = "Certificates are not configured on this server.";
        private const string NoTeacherAddress = "0x0000000000000000000000000000000000000000";

        private readonly ICertificateGateway _certificateGateway;
        private readonly ICertificateRegistry _registry;
        private readonly IClassRoomGateway _classRoomGateway;
        private readonly ICurriculumGateway _curriculumGateway;
        private readonly IPracticeExerciseGateway _practiceGateway;
        private readonly AccessGuard _access;
        private readonly TimeProvider _timeProvider;
        private readonly ISmartAccountService? _smartAccounts;
        private readonly IUserGateway? _userGateway;
        private readonly ICustodialAddressProvider? _custodialAddresses;

        public CertificateContext(ICertificateGateway certificateGateway, ICertificateRegistry registry,
            IClassRoomGateway classRoomGateway, ICurriculumGateway curriculumGateway, IPracticeExerciseGateway practiceGateway,
            AccessGuard access, TimeProvider timeProvider, ISmartAccountService? smartAccounts = null, IUserGateway? userGateway = null,
            ICustodialAddressProvider? custodialAddresses = null)
        {
            _smartAccounts = smartAccounts;
            _userGateway = userGateway;
            _custodialAddresses = custodialAddresses;
            _certificateGateway = certificateGateway;
            _registry = registry;
            _classRoomGateway = classRoomGateway;
            _curriculumGateway = curriculumGateway;
            _practiceGateway = practiceGateway;
            _access = access;
            _timeProvider = timeProvider;
        }

        public async Task<List<ClassCertificateDto>> IssueClassCertificatesAsync(Guid classRoomId, IssueCertificatesRequest? request)
        {
            var classRoom = await _access.RequireOwnerAsync(classRoomId);
            EnsureCanIssue();

            var enrollments = await _classRoomGateway.GetEnrollmentsAsync(classRoomId);
            var openCertificates = (await _certificateGateway.GetBySubjectAsync(CertificateKind.Class, classRoomId))
                .Where(c => c.Status != CertificateStatus.Revoked)
                .ToList();
            var targets = SelectTargets(enrollments, openCertificates, request?.StudentWallets);

            if (targets.Count == 0)
            {
                throw new BusinessRuleException("No students are eligible for a certificate. Set their final grades first.");
            }

            var issued = await IssueAsync(CertificateKind.Class, classRoomId, classRoom.Name, Normalize(classRoom.TeacherWallet),
                targets.Select(e => new CertificateTarget(Normalize(e.StudentWallet), e.FinalGrade!.Value)).ToList(), openCertificates);

            return issued.Select(ToClassDto).ToList();
        }

        public async Task<ChapterCertificateProgressDto> GetChapterProgressAsync(Guid chapterId)
        {
            var wallet = _access.RequireWallet();

            return await BuildChapterProgressAsync(chapterId, wallet);
        }

        public async Task<MyCertificateDto> ClaimChapterCertificateAsync(Guid chapterId)
        {
            var wallet = _access.RequireRole(UserRole.Student);
            var progress = await BuildChapterProgressAsync(chapterId, wallet);

            if (!progress.CertificatesAvailable)
            {
                throw new BusinessRuleException("Certificates are available only for chapters in which every formula has its own exercises.");
            }

            if (progress.Certificate != null)
            {
                throw new BusinessRuleException("You already have a certificate for this chapter.");
            }

            if (!progress.Completed)
            {
                throw new BusinessRuleException($"Solve every exercise in this chapter first ({progress.SolvedExercises}/{progress.TotalExercises}).");
            }

            var openCertificates = (await _certificateGateway.GetBySubjectAsync(CertificateKind.Chapter, chapterId))
                .Where(c => c.StudentWallet == wallet && c.Status != CertificateStatus.Revoked)
                .ToList();
            var custodialUser = await FindCustodialUserAsync(wallet);

            if (custodialUser != null)
            {
                var claimed = await ClaimThroughSmartAccountAsync(chapterId, progress.Title, wallet, custodialUser.Id,
                    progress.TotalExercises, openCertificates.FirstOrDefault());

                return ToMyDto(claimed);
            }

            EnsureCanIssue();

            var issued = await IssueAsync(CertificateKind.Chapter, chapterId, progress.Title, NoTeacherAddress,
                new List<CertificateTarget> { new(wallet, progress.TotalExercises) }, openCertificates);

            return ToMyDto(issued.Single());
        }

        private async Task<List<Certificate>> IssueAsync(CertificateKind kind, Guid subjectId, string subjectTitle, string teacherWallet,
            List<CertificateTarget> targets, List<Certificate> openCertificates)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var newCertificates = new List<Certificate>();
            var changedCertificates = new List<Certificate>();
            var toIssue = new List<Certificate>();
            var alreadyOnChain = new List<Certificate>();

            foreach (var target in targets)
            {
                var studentWallet = target.StudentWallet;
                var grade = target.Grade;
                var certificate = openCertificates.FirstOrDefault(c => c.StudentWallet == studentWallet);
                var activeTokenId = await _registry.FindActiveAsync(studentWallet, kind, subjectId);

                if (activeTokenId != 0)
                {
                    var onChain = await _registry.GetAsync(activeTokenId);

                    if (certificate != null && onChain != null && SameHex(onChain.DataHash, certificate.DataHash))
                    {
                        certificate.Status = CertificateStatus.Issued;
                        certificate.TokenId = activeTokenId;
                        certificate.IssuedAt = onChain.IssuedAt;
                        changedCertificates.Add(certificate);
                        alreadyOnChain.Add(certificate);
                        continue;
                    }

                    await _registry.RevokeAsync(activeTokenId);
                }

                if (certificate == null)
                {
                    certificate = new Certificate
                    {
                        Id = Guid.NewGuid(),
                        Kind = kind,
                        SubjectId = subjectId,
                        StudentWallet = studentWallet,
                        Status = CertificateStatus.Pending,
                        CreatedAt = now
                    };
                    newCertificates.Add(certificate);
                }
                else
                {
                    changedCertificates.Add(certificate);
                }

                certificate.SubjectTitle = Truncate(subjectTitle, MaximumTitleLength);

                if (string.IsNullOrEmpty(certificate.Salt) || certificate.Grade != grade || certificate.TeacherWallet != teacherWallet)
                {
                    certificate.Grade = grade;
                    certificate.TeacherWallet = teacherWallet;
                    certificate.Salt = _registry.CreateSalt();
                    certificate.DataHash = _registry.ComputeDataHash(ToCommitment(certificate));
                }

                toIssue.Add(certificate);
            }

            if (newCertificates.Count > 0)
            {
                await _certificateGateway.AddRangeAsync(newCertificates);
            }

            if (changedCertificates.Count > 0)
            {
                await _certificateGateway.UpdateRangeAsync(changedCertificates);
            }

            foreach (var batch in toIssue.Chunk(MaximumCertificatesPerTransaction))
            {
                var items = batch
                    .Select(c => new CertificateIssueItem { StudentAddress = c.StudentWallet, DataHash = c.DataHash })
                    .ToList();
                var result = await _registry.IssueBatchAsync(kind, subjectId, items);

                foreach (var certificate in batch)
                {
                    var token = result.Tokens.First(t => Normalize(t.StudentAddress) == certificate.StudentWallet);

                    certificate.TokenId = token.TokenId;
                    certificate.TransactionHash = result.TransactionHash;
                    certificate.Status = CertificateStatus.Issued;
                    certificate.IssuedAt = now;
                }

                await _certificateGateway.UpdateRangeAsync(batch);
            }

            return alreadyOnChain.Concat(toIssue).ToList();
        }

        public async Task<SmartAccountDto> GetMySmartAccountAsync()
        {
            var wallet = _access.RequireWallet();
            var custodialUser = await FindCustodialUserAsync(wallet);

            if (custodialUser == null)
            {
                return new SmartAccountDto { Available = false };
            }

            var address = await _smartAccounts!.GetAccountAddressAsync(wallet);
            var explorer = _registry.Network.ExplorerUrl;

            return new SmartAccountDto
            {
                Available = true,
                Address = address,
                Deployed = await _smartAccounts.IsDeployedAsync(address),
                ExplorerUrl = string.IsNullOrEmpty(explorer) ? null : $"{explorer}/address/{address}"
            };
        }

        private async Task<Certificate> ClaimThroughSmartAccountAsync(Guid chapterId, string title, string wallet, Guid userId,
            int grade, Certificate? certificate)
        {
            var account = await _smartAccounts!.GetAccountAddressAsync(wallet);
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            if (_registry.CanRead)
            {
                var activeTokenId = await _registry.FindActiveAsync(account, CertificateKind.Chapter, chapterId);

                if (activeTokenId != 0)
                {
                    var onChain = await _registry.GetAsync(activeTokenId);

                    if (certificate != null && onChain != null && certificate.HolderAddress == account
                        && SameHex(onChain.DataHash, certificate.DataHash))
                    {
                        certificate.Status = CertificateStatus.Issued;
                        certificate.TokenId = activeTokenId;
                        certificate.IssuedAt = onChain.IssuedAt;
                        await _certificateGateway.UpdateRangeAsync(new List<Certificate> { certificate });

                        return certificate;
                    }

                    await _smartAccounts.RevokeClaimedCertificateAsync(activeTokenId);
                }
            }

            var isNew = certificate == null;
            certificate ??= new Certificate
            {
                Id = Guid.NewGuid(),
                Kind = CertificateKind.Chapter,
                SubjectId = chapterId,
                StudentWallet = wallet,
                Status = CertificateStatus.Pending,
                CreatedAt = now
            };

            certificate.HolderAddress = account;
            certificate.SubjectTitle = Truncate(title, MaximumTitleLength);
            certificate.Grade = grade;
            certificate.TeacherWallet = NoTeacherAddress;
            certificate.Salt = _registry.CreateSalt();
            certificate.DataHash = _registry.ComputeDataHash(ToCommitment(certificate));

            if (isNew)
            {
                await _certificateGateway.AddRangeAsync(new List<Certificate> { certificate });
            }
            else
            {
                await _certificateGateway.UpdateRangeAsync(new List<Certificate> { certificate });
            }

            var result = await _smartAccounts.ClaimCertificateAsync(new SmartAccountClaimRequest
            {
                UserId = userId,
                OwnerAddress = wallet,
                Kind = CertificateKind.Chapter,
                SubjectId = chapterId,
                DataHash = certificate.DataHash
            });

            certificate.TokenId = result.TokenId;
            certificate.TransactionHash = result.TransactionHash;
            certificate.Status = CertificateStatus.Issued;
            certificate.IssuedAt = now;
            await _certificateGateway.UpdateRangeAsync(new List<Certificate> { certificate });

            return certificate;
        }

        private async Task<User?> FindCustodialUserAsync(string wallet)
        {
            if (_smartAccounts is not { IsConfigured: true } || _userGateway == null || _custodialAddresses == null)
            {
                return null;
            }

            var user = await _userGateway.GetByWalletAddressAsync(wallet);

            if (user == null)
            {
                return null;
            }

            try
            {
                return string.Equals(_custodialAddresses.GetOwnerAddress(user.Id), wallet, StringComparison.OrdinalIgnoreCase) ? user : null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        public async Task<List<ClassCertificateDto>> GetClassCertificatesAsync(Guid classRoomId)
        {
            await _access.RequireOwnerAsync(classRoomId);

            var certificates = await _certificateGateway.GetBySubjectAsync(CertificateKind.Class, classRoomId);

            return certificates.Select(ToClassDto).ToList();
        }

        public async Task RevokeAsync(Guid certificateId)
        {
            var certificate = await _certificateGateway.GetByIdAsync(certificateId);

            if (certificate == null)
            {
                throw new NotFoundException("Certificate not found.");
            }

            await RequireManagerAsync(certificate);

            if (certificate.Status == CertificateStatus.Revoked)
            {
                throw new BusinessRuleException("This certificate is already revoked.");
            }

            var claimedBySmartAccount = certificate.HolderAddress != null;

            if (claimedBySmartAccount)
            {
                if (_smartAccounts is not { IsConfigured: true })
                {
                    throw new ServiceUnavailableException("Smart account claims are not configured, so this certificate cannot be revoked.");
                }
            }
            else
            {
                EnsureCanIssue();
            }

            if (certificate.TokenId == null)
            {
                var activeTokenId = await _registry.FindActiveAsync(certificate.HolderAddress ?? certificate.StudentWallet,
                    certificate.Kind, certificate.SubjectId);
                var onChain = activeTokenId == 0 ? null : await _registry.GetAsync(activeTokenId);

                if (onChain != null && SameHex(onChain.DataHash, certificate.DataHash))
                {
                    certificate.TokenId = activeTokenId;
                }
            }

            if (certificate.TokenId != null)
            {
                if (claimedBySmartAccount)
                {
                    await _smartAccounts!.RevokeClaimedCertificateAsync(certificate.TokenId.Value);
                }
                else
                {
                    await _registry.RevokeAsync(certificate.TokenId.Value);
                }
            }

            certificate.Status = CertificateStatus.Revoked;
            certificate.RevokedAt = _timeProvider.GetUtcNow().UtcDateTime;

            await _certificateGateway.UpdateRangeAsync(new[] { certificate });
        }

        public async Task<List<MyCertificateDto>> GetMyCertificatesAsync()
        {
            var wallet = _access.RequireWallet();
            var certificates = await _certificateGateway.GetByStudentAsync(wallet);

            return certificates
                .Where(c => c.Status != CertificateStatus.Pending)
                .Select(ToMyDto)
                .ToList();
        }

        public async Task<CertificateVerificationDto> VerifyAsync(long tokenId, string? verificationKey)
        {
            if (!_registry.CanRead)
            {
                throw new ServiceUnavailableException(NotConfiguredMessage);
            }

            var onChain = await _registry.GetAsync(tokenId);

            if (onChain == null)
            {
                throw new NotFoundException("Certificate not found.");
            }

            var certificate = await _certificateGateway.GetByTokenIdAsync(tokenId);
            var network = _registry.Network;
            var verification = new CertificateVerificationDto
            {
                TokenId = tokenId,
                Kind = onChain.Kind,
                StudentAddress = onChain.StudentAddress,
                IssuerAddress = onChain.IssuerAddress,
                IssuedAt = onChain.IssuedAt,
                Revoked = onChain.Revoked,
                IsValid = !onChain.Revoked,
                TransactionHash = certificate?.TransactionHash,
                NetworkName = network.NetworkName,
                ChainId = network.ChainId,
                ContractAddress = network.ContractAddress,
                ExplorerUrl = network.ExplorerUrl
            };

            if (string.IsNullOrWhiteSpace(verificationKey))
            {
                return verification;
            }

            if (certificate == null || !SameSecret(certificate.Salt, verificationKey.Trim()))
            {
                throw new BusinessRuleException("The verification key does not match this certificate.");
            }

            var matches = SameHex(_registry.ComputeDataHash(ToCommitment(certificate)), onChain.DataHash)
                && Normalize(onChain.StudentAddress) == (certificate.HolderAddress ?? certificate.StudentWallet);

            verification.DetailsRevealed = true;
            verification.DataMatchesBlockchain = matches;
            verification.IsValid = !onChain.Revoked && matches;
            verification.SubjectTitle = certificate.SubjectTitle;
            verification.Grade = certificate.Grade;
            verification.TeacherAddress = certificate.TeacherWallet;

            return verification;
        }

        public async Task<CertificateSetupDto> GetSetupAsync()
        {
            _access.RequireRole(UserRole.Admin);

            var network = _registry.Network;
            var setup = new CertificateSetupDto
            {
                NetworkName = network.NetworkName,
                ChainId = network.ChainId,
                RpcUrl = network.RpcUrl,
                ExplorerUrl = network.ExplorerUrl,
                ContractAddress = network.ContractAddress,
                CanRead = _registry.CanRead,
                CanIssue = _registry.CanIssue,
                IssuerAddress = network.IssuerAddress
            };

            if (_registry.CanIssue)
            {
                var status = await _registry.GetIssuerStatusAsync();

                setup.HasIssuerRole = status.HasIssuerRole;
                setup.IssuerBalance = status.Balance;
                setup.GrantIssuerRoleCallData = _registry.BuildGrantIssuerRoleCallData();
            }

            if (_smartAccounts != null)
            {
                var accounts = await _smartAccounts.GetSetupAsync();

                setup.SmartAccountsConfigured = accounts.Configured;
                setup.EntryPointAddress = accounts.EntryPointAddress;
                setup.AccountFactoryAddress = accounts.FactoryAddress;
                setup.PaymasterAddress = accounts.PaymasterAddress;
                setup.ClaimsAddress = accounts.ClaimsAddress;
                setup.PaymasterDeposit = accounts.PaymasterDeposit;
                setup.ClaimsCanIssue = accounts.ClaimsCanIssue;
                setup.GrantClaimsIssuerRoleCallData = accounts.GrantClaimsIssuerRoleCallData;
                setup.PaymasterDepositCallData = accounts.PaymasterDepositCallData;
            }

            return setup;
        }

        public async Task<decimal> GetBalanceAsync(string walletAddress)
        {
            if (string.IsNullOrWhiteSpace(walletAddress))
            {
                throw new BusinessRuleException("Wallet address is required.");
            }

            _access.RequireSelfOrAdmin(walletAddress);

            if (!_registry.CanRead)
            {
                return 0m;
            }

            return await _registry.GetBalanceAsync(Normalize(walletAddress));
        }

        private async Task<ChapterCertificateProgressDto> BuildChapterProgressAsync(Guid chapterId, string wallet)
        {
            var chapter = await _curriculumGateway.GetByIdAsync(chapterId);

            if (chapter == null)
            {
                throw new NotFoundException("Chapter not found.");
            }

            var formulas = await _curriculumGateway.GetFormulasByChapterIdAsync(chapterId);
            var solved = formulas.Count == 0
                ? new List<SolvedPracticeExercise>()
                : await _practiceGateway.GetSolvedAsync(wallet, formulas.Select(f => f.Id).ToList());

            var items = formulas.Select(formula =>
            {
                var exercises = ExerciseCatalog.ForFormula(formula.ExercisesJson);

                return new ChapterFormulaProgressDto
                {
                    FormulaId = formula.Id,
                    Title = formula.Title,
                    UsesGenericExercises = exercises.UsesGenericExercises,
                    TotalExercises = exercises.Exercises.Count,
                    SolvedExercises = solved
                        .Where(s => s.FormulaId == formula.Id && s.ExerciseIndex < exercises.Exercises.Count)
                        .Select(s => s.ExerciseIndex)
                        .Distinct()
                        .Count()
                };
            }).ToList();

            var available = items.Count > 0 && items.All(i => !i.UsesGenericExercises);
            var certificate = (await _certificateGateway.GetByStudentAsync(wallet))
                .FirstOrDefault(c => c.Kind == CertificateKind.Chapter && c.SubjectId == chapterId && c.Status == CertificateStatus.Issued);

            return new ChapterCertificateProgressDto
            {
                ChapterId = chapterId,
                Title = chapter.Title,
                CertificatesAvailable = available,
                TotalExercises = items.Sum(i => i.TotalExercises),
                SolvedExercises = items.Sum(i => i.SolvedExercises),
                Completed = available && items.All(i => i.SolvedExercises >= i.TotalExercises),
                Formulas = items,
                Certificate = certificate == null ? null : ToMyDto(certificate)
            };
        }

        private static MyCertificateDto ToMyDto(Certificate certificate)
        {
            return new MyCertificateDto
            {
                Id = certificate.Id,
                Kind = certificate.Kind,
                SubjectId = certificate.SubjectId,
                SubjectTitle = certificate.SubjectTitle,
                Grade = certificate.Grade,
                Status = certificate.Status,
                TokenId = certificate.TokenId,
                VerificationKey = certificate.Salt,
                TransactionHash = certificate.TransactionHash,
                IssuedAt = certificate.IssuedAt,
                HolderAddress = certificate.HolderAddress
            };
        }

        private static List<Enrollment> SelectTargets(List<Enrollment> enrollments, List<Certificate> openCertificates, List<string>? requestedWallets)
        {
            bool HasIssuedCertificate(string wallet) =>
                openCertificates.Any(c => c.StudentWallet == wallet && c.Status == CertificateStatus.Issued);

            if (requestedWallets == null || requestedWallets.Count == 0)
            {
                return enrollments
                    .Where(e => e.FinalGrade.HasValue && !HasIssuedCertificate(Normalize(e.StudentWallet)))
                    .ToList();
            }

            var targets = new List<Enrollment>();

            foreach (var wallet in requestedWallets.Where(w => !string.IsNullOrWhiteSpace(w)).Select(Normalize).Distinct())
            {
                var enrollment = enrollments.FirstOrDefault(e => Normalize(e.StudentWallet) == wallet);

                if (enrollment == null)
                {
                    throw new NotFoundException($"Student {wallet} is not enrolled in this classroom.");
                }

                if (!enrollment.FinalGrade.HasValue)
                {
                    throw new BusinessRuleException($"Student {wallet} does not have a final grade yet.");
                }

                if (HasIssuedCertificate(wallet))
                {
                    throw new BusinessRuleException($"Student {wallet} already has a certificate for this classroom. Revoke it first.");
                }

                targets.Add(enrollment);
            }

            return targets;
        }

        private async Task RequireManagerAsync(Certificate certificate)
        {
            if (certificate.Kind == CertificateKind.Class && !_access.IsAdmin)
            {
                await _access.RequireOwnerAsync(certificate.SubjectId);
                return;
            }

            _access.RequireRole(UserRole.Admin);
        }

        private void EnsureCanIssue()
        {
            if (!_registry.CanIssue)
            {
                throw new ServiceUnavailableException(NotConfiguredMessage);
            }
        }

        private static CertificateCommitment ToCommitment(Certificate certificate)
        {
            return new CertificateCommitment
            {
                StudentAddress = certificate.HolderAddress ?? certificate.StudentWallet,
                Kind = certificate.Kind,
                SubjectId = certificate.SubjectId,
                Grade = certificate.Grade,
                TeacherAddress = certificate.TeacherWallet,
                Salt = certificate.Salt
            };
        }

        private static ClassCertificateDto ToClassDto(Certificate certificate)
        {
            return new ClassCertificateDto
            {
                Id = certificate.Id,
                StudentWallet = certificate.StudentWallet,
                Grade = certificate.Grade,
                Status = certificate.Status,
                TokenId = certificate.TokenId,
                TransactionHash = certificate.TransactionHash,
                IssuedAt = certificate.IssuedAt
            };
        }

        private static string Normalize(string wallet)
        {
            return wallet.Trim().ToLowerInvariant();
        }

        private static string Truncate(string value, int maximumLength)
        {
            return value.Length <= maximumLength ? value : value[..maximumLength];
        }

        private static bool SameHex(string first, string second)
        {
            return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
        }

        private sealed record CertificateTarget(string StudentWallet, int Grade);

        private static bool SameSecret(string expected, string provided)
        {
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected.ToLowerInvariant()),
                Encoding.UTF8.GetBytes(provided.ToLowerInvariant()));
        }
    }
}
