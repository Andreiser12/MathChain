using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;
using MathChain.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathChain.Gateway.Repositories
{
    public class ClassContentRepository : BaseRepository, IClassContentGateway
    {
        public ClassContentRepository(MathChainDbContext context) : base(context)
        {

        }

        public async Task<ClassAnnouncement> AddAnnouncementAsync(ClassAnnouncement announcement)
        {
            _context.ClassAnnouncements.Add(announcement);
            await _context.SaveChangesAsync();
            return announcement;
        }

        public async Task<MessageComment> AddCommentAsync(MessageComment comment)
        {
            _context.MessagesComments.Add(comment);
            await _context.SaveChangesAsync();
            return comment;
        }

        public async Task<ClassMedia> AddMediaAsync(ClassMedia media)
        {
            _context.ClassMedia.Add(media);
            await _context.SaveChangesAsync();
            return media;
        }

        public async Task<ClassResource> AddResourceAsync(ClassResource resource)
        {
            _context.ClassResources.Add(resource);
            await _context.SaveChangesAsync();
            return resource;
        }

        public async Task DeleteMediaAsync(Guid id)
        {
            var media = await _context.ClassMedia.FindAsync(id);

            if(media != null)
            {
                _context.ClassMedia.Remove(media);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteResourceAsync(Guid id)
        {
            var resource = await _context.ClassResources.FindAsync(id);

            if(resource != null)
            {
                _context.ClassResources.Remove(resource);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<ClassAnnouncement>> GetAnnouncementByClassRoomIdAsync(Guid classRoomId)
        {
            return await _context.ClassAnnouncements
                .AsNoTracking()
                .Where(a => a.ClassRoomId == classRoomId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ClassMedia>> GetMediaByClassRoomIdAsync(Guid classRoomId)
        {
            return await _context.ClassMedia
                .AsNoTracking()
                .Where(m => m.ClassRoomId == classRoomId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ClassResource>> GetResourcesByClassRoomIdAsync(Guid classRoomId)
        {
            return await _context.ClassResources
                .AsNoTracking()
                .Where(r => r.ClassRoomId == classRoomId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
    }
}
