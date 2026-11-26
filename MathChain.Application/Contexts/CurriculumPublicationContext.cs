using MathChain.Application.Curriculum;
using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Exceptions;
using MathChain.Domain.Gateways;
using MathChain.Domain.Interfaces;

namespace MathChain.Application.Contexts
{
    public class CurriculumPublicationContext : ICurriculumPublicationContext
    {
        private const string NotConfiguredMessage = "Curriculum anchoring is not configured on this server.";

        private readonly ICurriculumGateway _curriculumGateway;
        private readonly IIpfsStorageService _ipfs;
        private readonly ICurriculumRegistry _registry;
        private readonly AccessGuard _access;

        public CurriculumPublicationContext(ICurriculumGateway curriculumGateway, IIpfsStorageService ipfs, ICurriculumRegistry registry, AccessGuard access)
        {
            _curriculumGateway = curriculumGateway;
            _ipfs = ipfs;
            _registry = registry;
            _access = access;
        }

        public async Task<CurriculumAnchorDto?> GetLatestAsync()
        {
            if (!_registry.CanRead)
            {
                return null;
            }

            var latest = await _registry.GetLatestAsync();

            return latest == null ? null : ToDto(latest);
        }

        public async Task<CurriculumPublicationStatusDto> GetStatusAsync()
        {
            _access.RequireRole(UserRole.Admin);

            var (documents, chapterCount, formulaCount) = await BuildDocumentsAsync();
            var status = CreateStatus(_ipfs.ComputeHash(documents.Manifest), chapterCount, formulaCount);

            if (_registry.CanRead)
            {
                try
                {
                    var latest = await _registry.GetLatestAsync();
                    status.Published = latest == null ? null : ToDto(latest);
                    status.MatchesPublished = status.Published == null ? null : status.Published.Cid == status.CurrentCid;
                }
                catch (ServiceUnavailableException exception)
                {
                    status.ChainError = exception.Message;
                }
            }

            return status;
        }

        public async Task<CurriculumPublicationStatusDto> PublishAsync()
        {
            _access.RequireRole(UserRole.Admin);

            if (!_registry.CanPublish)
            {
                throw new ServiceUnavailableException(NotConfiguredMessage);
            }

            var (documents, chapterCount, formulaCount) = await BuildDocumentsAsync();

            foreach (var document in documents.ChapterFormulas.Values)
            {
                await _ipfs.SaveFileAsync(document);
            }

            var cid = await _ipfs.SaveFileAsync(documents.Manifest);
            var latest = await _registry.GetLatestAsync();
            var latestCid = latest == null ? null : _ipfs.FromBinaryCid(latest.Cid);

            if (latestCid == cid)
            {
                throw new BusinessRuleException($"This curriculum is already published as version {latest!.Version}.");
            }

            var anchor = await _registry.PublishAsync(_ipfs.ToBinaryCid(cid));
            var status = CreateStatus(cid, chapterCount, formulaCount);
            status.Published = ToDto(anchor);
            status.MatchesPublished = status.Published.Cid == cid;

            return status;
        }

        private async Task<(CurriculumDocuments Documents, int Chapters, int Formulas)> BuildDocumentsAsync()
        {
            var chapters = await _curriculumGateway.GetAllAsync();
            var prerequisites = await _curriculumGateway.GetPrerequisitesAsync();
            var formulas = await _curriculumGateway.GetFormulasByChapterIdsAsync(chapters.Select(c => c.Id).ToList());

            return (CurriculumManifest.Build(chapters, prerequisites, formulas, _ipfs.ComputeHash), chapters.Count, formulas.Count);
        }

        private CurriculumPublicationStatusDto CreateStatus(string currentCid, int chapterCount, int formulaCount)
        {
            var info = _registry.Info;

            return new CurriculumPublicationStatusDto
            {
                Configured = _registry.CanRead,
                CanPublish = _registry.CanPublish,
                NetworkName = info.NetworkName,
                ChainId = info.ChainId,
                ContractAddress = info.ContractAddress,
                ContractUrl = _registry.CanRead && !string.IsNullOrEmpty(info.ExplorerUrl) ? $"{info.ExplorerUrl}/address/{info.ContractAddress}" : null,
                PublisherAddress = info.PublisherAddress,
                CurrentCid = currentCid,
                ChapterCount = chapterCount,
                FormulaCount = formulaCount
            };
        }

        private CurriculumAnchorDto ToDto(CurriculumAnchor anchor)
        {
            var cid = _ipfs.FromBinaryCid(anchor.Cid) ?? string.Empty;
            var explorer = _registry.Info.ExplorerUrl;

            return new CurriculumAnchorDto
            {
                Version = anchor.Version,
                Cid = cid,
                PublishedAt = anchor.PublishedAt,
                Publisher = anchor.Publisher,
                TransactionHash = anchor.TransactionHash,
                TransactionUrl = anchor.TransactionHash != null && !string.IsNullOrEmpty(explorer) ? $"{explorer}/tx/{anchor.TransactionHash}" : null,
                ManifestPath = string.IsNullOrEmpty(cid) ? string.Empty : $"/api/ipfs/{cid}?fileName=curriculum-v{anchor.Version}.json"
            };
        }
    }
}
