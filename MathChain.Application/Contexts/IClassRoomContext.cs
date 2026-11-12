using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathChain.Application.Contexts
{
    public interface IClassRoomContext
    {
        Task<ClassRoom> CreateClassRoomAsync(CreateClassRoomRequest request);
        Task<ClassRoom> JoinClassRoomAsync(JoinClassRoomRequest request);
        Task<ClassRoom> GetClassRoomByIdAsync(Guid id);
        Task<List<ClassRoom>> GetByTeacherWalletAsync(string teacherWallet);
        Task<List<ClassRoom>> GetByStudentWalletAsync(string studentWallet);
        Task<List<string>> GetEnrolledStudentsAsync(Guid classRoomId);
        Task DeleteClassRoomAsync(Guid id);
    }
}
