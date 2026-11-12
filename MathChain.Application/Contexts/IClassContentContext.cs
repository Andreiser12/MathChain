using MathChain.Application.DTOs;
using MathChain.Domain.Entities;

namespace MathChain.Application.Contexts
{
    public interface IClassContentContext
    {
        Task<List<AnnouncementResponseDto>> GetAnnouncementsAsync(Guid classRoomId);
        Task<ClassAnnouncement> CreateAnnouncementAsync(Guid classRoomId, CreateAnnouncementRequest request);
        Task<MessageComment> AddCommentAsync(Guid messageId, AddCommentRequest request);

        Task<List<ClassMedia>> GetMediaAsync(Guid classRoomId);
        Task<ClassMedia> AddMediaAsync(Guid classRoomId, CreateClassMediaRequest request);
        Task DeleteMediaAsync(Guid mediaId);

        Task<List<ClassResource>> GetResourcesAsync(Guid classRoomId);
        Task<ClassResource> AddResourceAsync(Guid classRoomId, CreateResourceRequest request);
        Task DeleteResourceAsync(Guid resourceId);
    }
}
