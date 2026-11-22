using MathChain.Domain.Entities;

namespace MathChain.Domain.Interfaces
{
    public interface ISolutionCatalog
    {
        ExerciseSolution? Find(string formulaCode, ExerciseTemplate template);
    }
}
