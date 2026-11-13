using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;
using MathChain.Domain.Exceptions;

namespace MathChain.Application.Contexts
{
    public class ClassContentContext : IClassContentContext
    {
        private readonly IClassContentGateway _contentGateway;
        private readonly AccessGuard _access;

        public ClassContentContext(IClassContentGateway contentGateway, AccessGuard access)
        {
            _contentGateway = contentGateway;
            _access = access;
        }

        public async Task<List<AnnouncementResponseDto>> GetAnnouncementsAsync(Guid classRoomId)
        {
            await _access.RequireMemberAsync(classRoomId);

            var announcements = await _contentGateway.GetAnnouncementByClassRoomIdAsync(classRoomId);

            var dtos = announcements.Select(a => new AnnouncementResponseDto
            {
                Id = a.Id,
                ClassRoomId = a.ClassRoomId,
                Title = a.Title,
                Content = a.Content,
                AuthorWallet = a.AuthorWallet,
                AuthorName = FormatWallet(a.AuthorWallet),
                CreatedAt = a.CreatedAt
            }).ToList();

            return dtos;
        }

        public async Task<ClassAnnouncement> CreateAnnouncementAsync(Guid classRoomId, CreateAnnouncementRequest request)
        {
            await _access.RequireOwnerAsync(classRoomId);
            var authorWallet = _access.RequireWallet();

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                throw new BusinessRuleException("Title is required.");
            }

            var announcement = new ClassAnnouncement
            {
                Id = Guid.NewGuid(),
                ClassRoomId = classRoomId,
                Title = request.Title.Trim(),
                Content = request.Content?.Trim() ?? string.Empty,
                AuthorWallet = authorWallet,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _contentGateway.AddAnnouncementAsync(announcement);

            return created;
        }

        public async Task<MessageComment> AddCommentAsync(Guid messageId, AddCommentRequest request)
        {
            var announcement = await _contentGateway.GetAnnouncementByIdAsync(messageId);

            if (announcement == null)
            {
                throw new NotFoundException("Message not found.");
            }

            await _access.RequireMemberAsync(announcement.ClassRoomId);
            var authorWallet = _access.RequireWallet();

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                throw new BusinessRuleException("Comment content is required.");
            }

            var comment = new MessageComment
            {
                Id = Guid.NewGuid(),
                MessageId = messageId,
                AuthorWallet = authorWallet,
                Content = request.Content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            var created = await _contentGateway.AddCommentAsync(comment);

            return created;
        }

        public async Task<List<ClassMedia>> GetMediaAsync(Guid classRoomId)
        {
            await _access.RequireMemberAsync(classRoomId);

            var mediaList = await _contentGateway.GetMediaByClassRoomIdAsync(classRoomId);
            return mediaList;
        }

        public async Task<ClassMedia> AddMediaAsync(Guid classRoomId, CreateClassMediaRequest request)
        {
            await _access.RequireMemberAsync(classRoomId);
            var uploaderWallet = _access.RequireWallet();

            if (string.IsNullOrWhiteSpace(request.FileName) || string.IsNullOrWhiteSpace(request.IpfsHash))
            {
                throw new BusinessRuleException("File name and IPFS hash are required.");
            }

            var media = new ClassMedia
            {
                Id = Guid.NewGuid(),
                ClassRoomId = classRoomId,
                FileName = request.FileName.Trim(),
                IpfsHash = request.IpfsHash.Trim(),
                UploaderWallet = uploaderWallet,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _contentGateway.AddMediaAsync(media);

            return created;
        }

        public async Task DeleteMediaAsync(Guid mediaId)
        {
            var media = await _contentGateway.GetMediaByIdAsync(mediaId);

            if (media == null)
            {
                throw new NotFoundException("Media not found.");
            }

            var classRoom = await _access.RequireMemberAsync(media.ClassRoomId);
            var isUploader = string.Equals(media.UploaderWallet, _access.RequireWallet(), StringComparison.OrdinalIgnoreCase);

            if (!_access.IsOwnerOrAdmin(classRoom) && !isUploader)
            {
                throw new ForbiddenException("Only the teacher or the person who uploaded this media can delete it.");
            }

            await _contentGateway.DeleteMediaAsync(mediaId);
        }

        public async Task<List<ClassResource>> GetResourcesAsync(Guid classRoomId)
        {
            await _access.RequireMemberAsync(classRoomId);

            var resources = await _contentGateway.GetResourcesByClassRoomIdAsync(classRoomId);

            return resources;
        }

        public async Task<ClassResource> AddResourceAsync(Guid classRoomId, CreateResourceRequest request)
        {
            await _access.RequireOwnerAsync(classRoomId);

            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.IpfsHash))
            {
                throw new BusinessRuleException("Title and IPFS hash are required.");
            }

            var resource = new ClassResource
            {
                Id = Guid.NewGuid(),
                ClassRoomId = classRoomId,
                WeekNumber = request.WeekNumber,
                Title = request.Title.Trim(),
                Description = request.Description?.Trim() ?? string.Empty,
                FileName = request.FileName?.Trim() ?? string.Empty,
                IpfsHash = request.IpfsHash.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            var created = await _contentGateway.AddResourceAsync(resource);

            return created;
        }

        public async Task DeleteResourceAsync(Guid resourceId)
        {
            var resource = await _contentGateway.GetResourceByIdAsync(resourceId);

            if (resource == null)
            {
                throw new NotFoundException("Resource not found.");
            }

            await _access.RequireOwnerAsync(resource.ClassRoomId);

            await _contentGateway.DeleteResourceAsync(resourceId);
        }

        private static string FormatWallet(string wallet)
        {
            if (string.IsNullOrWhiteSpace(wallet))
            {
                return string.Empty;
            }

            return wallet.Length > 10 ? $"{wallet[..6]}...{wallet[^4..]}" : wallet;
        }
    }
}
