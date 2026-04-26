using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using System;
using System.Collections.Generic;

namespace MathChain.Domain.Interfaces
{
    public interface IMathProblemData
    {
        MathProblem GetById(Guid id);
        IEnumerable<MathProblem> GetAll();
        IEnumerable<MathProblem> GetByDifficulty(DifficultyLevel difficulty);
        void Add(MathProblem problem);

    }
}
