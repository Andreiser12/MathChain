using MathChain.Domain.Entities;

namespace MathChain.Domain.Gateways
{
    public interface IClassContentGateway
    {
        Task<List<ClassMedia>> GetMediaByClassRoomIdAsync(Guid classRoomId);
        Task<ClassMedia> AddMediaAsync(ClassMedia media);
        Task DeleteMediaAsync(Guid id);

        Task<List<ClassResource>> GetResourcesByClassRoomIdAsync(Guid classRoomId);
        Task<ClassResource> AddResourceAsync(ClassResource resource);
        Task DeleteResourceAsync(Guid id);

        Task<List<ClassAnnouncement>> GetAnnouncementByClassRoomIdAsync(Guid classRoomId);
        Task<ClassAnnouncement> AddAnnouncementAsync(ClassAnnouncement announcement);
        Task<MessageComment> AddCommentAsync(MessageComment comment);
    }
}
