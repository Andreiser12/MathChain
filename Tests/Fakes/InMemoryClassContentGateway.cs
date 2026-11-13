using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;

namespace MathChain.Tests.Fakes
{
    public class InMemoryClassContentGateway : IClassContentGateway
    {
        public List<ClassAnnouncement> Announcements { get; } = new();
        public List<MessageComment> Comments { get; } = new();
        public List<ClassMedia> Media { get; } = new();
        public List<ClassResource> Resources { get; } = new();

        public Task<List<ClassMedia>> GetMediaByClassRoomIdAsync(Guid classRoomId)
        {
            return Task.FromResult(Media.Where(m => m.ClassRoomId == classRoomId).OrderByDescending(m => m.CreatedAt).ToList());
        }

        public Task<ClassMedia> AddMediaAsync(ClassMedia media)
        {
            Media.Add(media);
            return Task.FromResult(media);
        }

        public Task DeleteMediaAsync(Guid id)
        {
            Media.RemoveAll(m => m.Id == id);
            return Task.CompletedTask;
        }

        public Task<List<ClassResource>> GetResourcesByClassRoomIdAsync(Guid classRoomId)
        {
            return Task.FromResult(Resources.Where(r => r.ClassRoomId == classRoomId).OrderByDescending(r => r.CreatedAt).ToList());
        }

        public Task<ClassResource> AddResourceAsync(ClassResource resource)
        {
            Resources.Add(resource);
            return Task.FromResult(resource);
        }

        public Task DeleteResourceAsync(Guid id)
        {
            Resources.RemoveAll(r => r.Id == id);
            return Task.CompletedTask;
        }

        public Task<List<ClassAnnouncement>> GetAnnouncementByClassRoomIdAsync(Guid classRoomId)
        {
            return Task.FromResult(Announcements.Where(a => a.ClassRoomId == classRoomId).OrderByDescending(a => a.CreatedAt).ToList());
        }

        public Task<ClassAnnouncement> AddAnnouncementAsync(ClassAnnouncement announcement)
        {
            Announcements.Add(announcement);
            return Task.FromResult(announcement);
        }

        public Task<MessageComment> AddCommentAsync(MessageComment comment)
        {
            Comments.Add(comment);
            return Task.FromResult(comment);
        }

        public Task<List<MessageComment>> GetCommentsByMessageIdsAsync(List<Guid> messageIds)
        {
            return Task.FromResult(Comments.Where(c => messageIds.Contains(c.MessageId)).OrderBy(c => c.CreatedAt).ToList());
        }

        public Task<ClassAnnouncement?> GetAnnouncementByIdAsync(Guid id)
        {
            return Task.FromResult(EntityCopy.Detached(Announcements.FirstOrDefault(a => a.Id == id)));
        }

        public Task<ClassMedia?> GetMediaByIdAsync(Guid id)
        {
            return Task.FromResult(EntityCopy.Detached(Media.FirstOrDefault(m => m.Id == id)));
        }

        public Task<ClassResource?> GetResourceByIdAsync(Guid id)
        {
            return Task.FromResult(EntityCopy.Detached(Resources.FirstOrDefault(r => r.Id == id)));
        }
    }
}
