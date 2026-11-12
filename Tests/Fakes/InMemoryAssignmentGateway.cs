using MathChain.Domain.Entities;
using MathChain.Domain.Gateways;

namespace MathChain.Tests.Fakes
{
    public class InMemoryAssignmentGateway : IAssignmentGateway
    {
        public List<AssignmentBase> Assignments { get; } = new();

        public Task<List<Assignment>> GetByClassRoomIdAsync(Guid classRoomId)
        {
            return Task.FromResult(Assignments.OfType<Assignment>()
                .Where(a => a.ClassRoomId == classRoomId)
                .OrderByDescending(a => a.CreatedAt)
                .ToList());
        }

        public Task<Assignment?> GetByIdAsync(Guid id)
        {
            return Task.FromResult(Assignments.OfType<Assignment>().FirstOrDefault(a => a.Id == id));
        }

        public Task<Assignment> CreateAsync(Assignment assignment)
        {
            Assignments.Add(assignment);
            return Task.FromResult(assignment);
        }

        public Task DeleteAsync(Guid id)
        {
            Assignments.RemoveAll(a => a.Id == id);
            return Task.CompletedTask;
        }

        public Task<List<Guid>> GetAllIdsByClassRoomIdAsync(Guid classRoomId)
        {
            return Task.FromResult(Assignments.Where(a => a.ClassRoomId == classRoomId).Select(a => a.Id).ToList());
        }
    }
}
