using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Interfaces;

namespace MathChain.Domain.Repositories
{
    public class InMemoryMathProblemData : IMathProblemData
    {
        private readonly List<MathProblem> _problems;

        public InMemoryMathProblemData()
        {
            _problems = new List<MathProblem> 
            {
                new IntegralProblem
                {
                    Id = Guid.NewGuid(),
                    Title = "Integral of x^2 from 0 to 1",
                    Description = "Find the value of the integral ∫(0 to 1) x^2 dx",
                    Difficulty = DifficultyLevel.Easy,
                    CreatedAt = DateTime.UtcNow,
                    Expression = "x^2",
                    CorrectSolution = 0.333,
                    Variable = "x",
                    LowerBound = 0,
                    UpperBound = 1
                },
            };
        }

        public void Add(MathProblem problem)
        {
            _problems.Add(problem);
        }

        public IEnumerable<MathProblem> GetAll()
        {
            return _problems;
        }

        public IEnumerable<MathProblem> GetByDifficulty(DifficultyLevel difficulty)
        {
            return _problems.Where(p => p.Difficulty == difficulty);
        }

        public MathProblem GetById(Guid id)
        {
            return _problems.FirstOrDefault(p => p.Id == id);
        }
    }
}
