using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using MathChain.Infrastructure.ResponseHandling;

namespace MathChain.Application.Contexts
{
    public interface IClassContentContext
    {
        Task<Envelope<List<AnnouncementResponseDto>>> GetAnnouncementsAsync(Guid classRoomId);
        Task<Envelope<ClassAnnouncement>> CreateAnnouncementAsync(Guid classRoomId, CreateAnnouncementRequest request);
        Task<Envelope<MessageComment>> AddCommentAsync(Guid messageId, AddCommentRequest request);

        Task<Envelope<List<ClassMedia>>> GetMediaAsync(Guid classRoomId);
        Task<Envelope<ClassMedia>> AddMediaAsync(Guid classRoomId, CreateClassMediaRequest request);
        Task<Envelope<object>> DeleteMediaAsync(Guid mediaId);

        Task<Envelope<List<ClassResource>>> GetResourcesAsync(Guid classRoomId);
        Task<Envelope<ClassResource>> AddResourceAsync(Guid classRoomId, CreateResourceRequest request);
        Task<Envelope<object>> DeleteResourceAsync(Guid resourceId);
    }
}
