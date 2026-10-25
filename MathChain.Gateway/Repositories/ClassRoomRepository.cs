using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;
using MathChain.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace MathChain.Gateway.Repositories
{
    public class ClassRoomRepository : BaseRepository, IClassRoomGateway
    {
        public ClassRoomRepository(MathChainDbContext context) : base(context)
        {

        }

        public async Task<ClassRoom> CreateAsync(ClassRoom classRoom)
        {
            _context.ClassRooms.Add(classRoom);

            await _context.SaveChangesAsync();

            return classRoom;
        }

        public async Task DeleteAsync(Guid id)
        {
            var classRoom = await _context.ClassRooms.FindAsync(id);

            if(classRoom != null)
            {
                var enrollments = await _context.Enrollments
                    .Where(e => e.ClassRoomId == id)
                    .ToListAsync();

                _context.Enrollments.RemoveRange(enrollments);

                _context.ClassRooms.Remove(classRoom);

                await _context.SaveChangesAsync();
            }
        }

        public async Task EnrollStudentAsync(Enrollment enrollment)
        {
            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();
        }

        public async Task<ClassRoom?> GetByIdAsync(Guid id)
        {
            return await _context.ClassRooms
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<ClassRoom?> GetByJoinCodeAsync(string joinCode)
        {
            if(string.IsNullOrWhiteSpace(joinCode))
            {
                return null;
            }

            return await _context.ClassRooms
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.JoinCode == joinCode);
        }

        public async Task<List<ClassRoom>> GetByTeacherWalletAsync(string teacherWallet)
        {
            var wallet = teacherWallet.Trim().ToLower();

            return await _context.ClassRooms
                .AsNoTracking()
                .Where(c => c.TeacherWallet.ToLower() == wallet)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ClassRoom>> GetEnrolledByStudentWalletAsync(string studentWallet)
        {
            var wallet = studentWallet.Trim().ToLower();

            return await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.StudentWallet.ToLower() == wallet)
                .Join(_context.ClassRooms.AsNoTracking(),
                 enrollment => enrollment.ClassRoomId,
                 classRoom => classRoom.Id,
                 (enrollment, classRoom) => classRoom)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<string>> GetEnrolledStudentsWalletsAsync(Guid classRoomId)
        {
            return await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.ClassRoomId == classRoomId)
                .Select(e => e.StudentWallet)
                .ToListAsync();
        }

        public async Task<bool> IsJoinCodeTakenAsync(string joinCode)
        {
            return await _context.ClassRooms.AnyAsync(c => c.JoinCode == joinCode);
        }

        public async Task<bool> IsStudentEnrolledAsync(Guid classRoomId, string studentWallet)
        {
            var wallet = studentWallet.Trim().ToLower();

            return await _context.Enrollments
                .AnyAsync(e => e.ClassRoomId == classRoomId && e.StudentWallet.ToLower() == wallet);
        }
    }
}
