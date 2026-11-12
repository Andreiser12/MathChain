using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;
using MathChain.Domain.Exceptions;

namespace MathChain.Application.Contexts
{
    public class ClassContentContext : IClassContentContext
    {
        private readonly IClassContentGateway _contentGateway;
        private readonly IClassRoomGateway _classRoomGateway;
        public ClassContentContext(IClassContentGateway contentGateway, IClassRoomGateway classRoomGateway)
        {
            _contentGateway = contentGateway;
            _classRoomGateway = classRoomGateway;
        }

        public async Task<List<AnnouncementResponseDto>> GetAnnouncementsAsync(Guid classRoomId)
        {
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
            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.AuthorWallet))
            {
                throw new BusinessRuleException("Title and author wallet are required.");
            }

            var classRoom = await _classRoomGateway.GetByIdAsync(classRoomId);

            if (classRoom == null)
            {
                throw new NotFoundException("Classroom not found.");
            }

            var announcement = new ClassAnnouncement
            {
                Id = Guid.NewGuid(),
                ClassRoomId = classRoomId,
                Title = request.Title.Trim(),
                Content = request.Content?.Trim() ?? string.Empty,
                AuthorWallet = request.AuthorWallet.Trim().ToLower(),
                CreatedAt = DateTime.UtcNow
            };

            var created = await _contentGateway.AddAnnouncementAsync(announcement);

            return created;
        }

        public async Task<MessageComment> AddCommentAsync(Guid messageId, AddCommentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Content) || string.IsNullOrWhiteSpace(request.AuthorWallet))
            {
                throw new BusinessRuleException("Comment content and author wallet are required.");
            }

            var comment = new MessageComment
            {
                Id = Guid.NewGuid(),
                MessageId = messageId,
                AuthorWallet = request.AuthorWallet.Trim().ToLower(),
                Content = request.Content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            var created = await _contentGateway.AddCommentAsync(comment);

            return created;
        }

        public async Task<List<ClassMedia>> GetMediaAsync(Guid classRoomId)
        {
            var mediaList = await _contentGateway.GetMediaByClassRoomIdAsync(classRoomId);
            return mediaList;
        }

        public async Task<ClassMedia> AddMediaAsync(Guid classRoomId, CreateClassMediaRequest request)
        {
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
                UploaderWallet = request.UploaderWallet?.Trim().ToLower() ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _contentGateway.AddMediaAsync(media);

            return created;
        }

        public async Task DeleteMediaAsync(Guid mediaId)
        {
            await _contentGateway.DeleteMediaAsync(mediaId);
        }

        public async Task<List<ClassResource>> GetResourcesAsync(Guid classRoomId)
        {
            var resources = await _contentGateway.GetResourcesByClassRoomIdAsync(classRoomId);

            return resources;
        }

        public async Task<ClassResource> AddResourceAsync(Guid classRoomId, CreateResourceRequest request)
        {
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
