using FluentAssertions;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Repositories;

namespace MathChain.Tests.Domain
{
    public class InMemoryMathProblemRepositoryTests
    {
        [Fact]
        public void GetAll_RetrurnsAllProblems()
        {
            var inMemoryRepo = new InMemoryMathProblemData();
            var result = inMemoryRepo.GetAll();
            result.Should().NotBeEmpty();
            result.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public void GetById_ExistingId_ReturnsProblem()
        {
            var inMemoryRepo = new InMemoryMathProblemData();
            var existingProblem = inMemoryRepo.GetAll().First();
            var result = inMemoryRepo.GetById(existingProblem.Id);
            result.Should().NotBeNull();
            result.Id.Should().Be(existingProblem.Id);
        }

        [Fact]
        public void GetById_NonExistingId_ReturnsNull()
        {
            var inMemoryRepo = new InMemoryMathProblemData();
            var guidRandom = Guid.NewGuid();
            var result = inMemoryRepo.GetById(guidRandom);
            result.Should().BeNull();
        }

        [Fact]
        public void GetByDifficulty_Easy_ReturnsOnlyEasyProblems()
        {
            var inMemoryRepo = new InMemoryMathProblemData();
            inMemoryRepo.Add(new IntegralProblem
            {
                Id = Guid.NewGuid(),
                Title = "Integral of x^3 from 0 to 1",
                Description = "Find the value of the integral ∫(0 to 1) x^3 dx",
                Difficulty = DifficultyLevel.Easy,
                CreatedAt = DateTime.UtcNow,
                Expression = "x^3",
                CorrectSolution = 0.25,
                Variable = "x",
                LowerBound = 0,
                UpperBound = 1
            });
            var result = inMemoryRepo.GetByDifficulty(DifficultyLevel.Easy);
            result.Should().NotBeEmpty();
            result.Should().OnlyContain(p => p.Difficulty == DifficultyLevel.Easy);
        }

        [Fact]
        public void Add_NewProblem_IncreasesCount()
        {
            var inMemoryRepository = new InMemoryMathProblemData();
            var initialCount = inMemoryRepository.GetAll().Count();
            inMemoryRepository.Add(new IntegralProblem
            {
                Id = Guid.NewGuid(),
                Title = "Integral of x^4 from 0 to 1",
                Description = "Find the value of the integral ∫(0 to 1) x^4 dx",
                Difficulty = DifficultyLevel.Easy,
                CreatedAt = DateTime.UtcNow,
                Expression = "x^4",
                CorrectSolution = 0.2,
                Variable = "x",
                LowerBound = 0,
                UpperBound = 1
            });
            var result = inMemoryRepository.GetAll().Count();
            result.Should().Be(initialCount + 1);
        }
    }
}
