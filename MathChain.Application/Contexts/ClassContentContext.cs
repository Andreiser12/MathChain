using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;
using MathChain.Infrastructure.ResponseHandling;

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

        public async Task<Envelope<List<AnnouncementResponseDto>>> GetAnnouncementsAsync(Guid classRoomId)
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

            return EnvelopeExtensions.Success(dtos);
        }

        public async Task<Envelope<ClassAnnouncement>> CreateAnnouncementAsync(Guid classRoomId, CreateAnnouncementRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.AuthorWallet))
            {
                return EnvelopeExtensions.Failure<ClassAnnouncement>("Title and author wallet are required.", 400);
            }

            var classRoom = await _classRoomGateway.GetByIdAsync(classRoomId);

            if (classRoom == null)
            {
                return EnvelopeExtensions.Failure<ClassAnnouncement>("Classroom not found.", 404);
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

            return EnvelopeExtensions.Success(created, "Announcement posted successfully.", 201);
        }

        public async Task<Envelope<MessageComment>> AddCommentAsync(Guid messageId, AddCommentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Content) || string.IsNullOrWhiteSpace(request.AuthorWallet))
            {
                return EnvelopeExtensions.Failure<MessageComment>("Comment content and author wallet are required.", 400);
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

            return EnvelopeExtensions.Success(created, "Comment added successfully.", 201);
        }

        public async Task<Envelope<List<ClassMedia>>> GetMediaAsync(Guid classRoomId)
        {
            var mediaList = await _contentGateway.GetMediaByClassRoomIdAsync(classRoomId);
            return EnvelopeExtensions.Success(mediaList);
        }

        public async Task<Envelope<ClassMedia>> AddMediaAsync(Guid classRoomId, CreateClassMediaRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FileName) || string.IsNullOrWhiteSpace(request.IpfsHash))
            {
                return EnvelopeExtensions.Failure<ClassMedia>("File name and IPFS hash are required.", 400);
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

            return EnvelopeExtensions.Success(created, "Class media uploaded successfully.", 201);
        }

        public async Task<Envelope<object>> DeleteMediaAsync(Guid mediaId)
        {
            await _contentGateway.DeleteMediaAsync(mediaId);

            return EnvelopeExtensions.Success("Media deleted successfully.", 200);
        }

        public async Task<Envelope<List<ClassResource>>> GetResourcesAsync(Guid classRoomId)
        {
            var resources = await _contentGateway.GetResourcesByClassRoomIdAsync(classRoomId);

            return EnvelopeExtensions.Success(resources);
        }

        public async Task<Envelope<ClassResource>> AddResourceAsync(Guid classRoomId, CreateResourceRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.IpfsHash))
            {
                return EnvelopeExtensions.Failure<ClassResource>("Title and IPFS hash are required.", 400);
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

            return EnvelopeExtensions.Success(created, "Resource material added successfully.", 201);
        }

        public async Task<Envelope<object>> DeleteResourceAsync(Guid resourceId)
        {
            await _contentGateway.DeleteResourceAsync(resourceId);

            return EnvelopeExtensions.Success("Resource deleted successfully.", 200);
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
