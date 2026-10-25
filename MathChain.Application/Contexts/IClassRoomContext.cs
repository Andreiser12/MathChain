using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using MathChain.Infrastructure.ResponseHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathChain.Application.Contexts
{
    public interface IClassRoomContext
    {
        Task<Envelope<ClassRoom>> CreateClassRoomAsync(CreateClassRoomRequest request);
        Task<Envelope<ClassRoom>> JoinClassRoomAsync(JoinClassRoomRequest request);
        Task<Envelope<ClassRoom>> GetClassRoomByIdAsync(Guid id);
        Task<Envelope<List<ClassRoom>>> GetByTeacherWalletAsync(string teacherWallet);
        Task<Envelope<List<ClassRoom>>> GetByStudentWalletAsync(string studentWallet);
        Task<Envelope<List<string>>> GetEnrolledStudentsAsync(Guid classRoomId);
        Task<Envelope<object>> DeleteClassRoomAsync(Guid id);
    }
}
