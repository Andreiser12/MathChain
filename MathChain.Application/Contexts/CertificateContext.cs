using System.Security.Cryptography;
using System.Text;
using MathChain.Application.DTOs;
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

        private readonly ICertificateGateway _certificateGateway;
        private readonly ICertificateRegistry _registry;
        private readonly IClassRoomGateway _classRoomGateway;
        private readonly AccessGuard _access;
        private readonly TimeProvider _timeProvider;

        public CertificateContext(ICertificateGateway certificateGateway, ICertificateRegistry registry,
            IClassRoomGateway classRoomGateway, AccessGuard access, TimeProvider timeProvider)
        {
            _certificateGateway = certificateGateway;
            _registry = registry;
            _classRoomGateway = classRoomGateway;
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

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var teacherWallet = Normalize(classRoom.TeacherWallet);
            var newCertificates = new List<Certificate>();
            var changedCertificates = new List<Certificate>();
            var toIssue = new List<Certificate>();
            var alreadyOnChain = new List<Certificate>();

            foreach (var enrollment in targets)
            {
                var studentWallet = Normalize(enrollment.StudentWallet);
                var grade = enrollment.FinalGrade!.Value;
                var certificate = openCertificates.FirstOrDefault(c => c.StudentWallet == studentWallet);
                var activeTokenId = await _registry.FindActiveAsync(studentWallet, CertificateKind.Class, classRoomId);

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
                        Kind = CertificateKind.Class,
                        SubjectId = classRoomId,
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

                certificate.SubjectTitle = Truncate(classRoom.Name, MaximumTitleLength);

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
                var result = await _registry.IssueBatchAsync(CertificateKind.Class, classRoomId, items);

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

            return alreadyOnChain.Concat(toIssue).Select(ToClassDto).ToList();
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

            EnsureCanIssue();

            if (certificate.TokenId == null)
            {
                var activeTokenId = await _registry.FindActiveAsync(certificate.StudentWallet, certificate.Kind, certificate.SubjectId);
                var onChain = activeTokenId == 0 ? null : await _registry.GetAsync(activeTokenId);

                if (onChain != null && SameHex(onChain.DataHash, certificate.DataHash))
                {
                    certificate.TokenId = activeTokenId;
                }
            }

            if (certificate.TokenId != null)
            {
                await _registry.RevokeAsync(certificate.TokenId.Value);
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
                .Select(c => new MyCertificateDto
                {
                    Id = c.Id,
                    Kind = c.Kind,
                    SubjectId = c.SubjectId,
                    SubjectTitle = c.SubjectTitle,
                    Grade = c.Grade,
                    Status = c.Status,
                    TokenId = c.TokenId,
                    VerificationKey = c.Salt,
                    TransactionHash = c.TransactionHash,
                    IssuedAt = c.IssuedAt
                })
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
                && Normalize(onChain.StudentAddress) == certificate.StudentWallet;

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
                StudentAddress = certificate.StudentWallet,
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

        private static bool SameSecret(string expected, string provided)
        {
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected.ToLowerInvariant()),
                Encoding.UTF8.GetBytes(provided.ToLowerInvariant()));
        }
    }
}
