using MathChain.Application.Practice;
using MathChain.Domain.Entities;
using MathChain.Domain.Exceptions;
using MathChain.Domain.Gateways;

namespace MathChain.Application.Curriculum
{
    public class ChapterUnlockService
    {
        private readonly ICurriculumGateway _curriculumGateway;
        private readonly IPracticeExerciseGateway _practiceGateway;

        public ChapterUnlockService(ICurriculumGateway curriculumGateway, IPracticeExerciseGateway practiceGateway)
        {
            _curriculumGateway = curriculumGateway;
            _practiceGateway = practiceGateway;
        }

        public async Task<IReadOnlyDictionary<Guid, ChapterStatus>> EvaluateAsync(string wallet, IReadOnlyCollection<Guid> chapterIds,
            IReadOnlyCollection<CurriculumChapter>? allChapters = null, IReadOnlyCollection<ChapterPrerequisite>? prerequisites = null)
        {
            var chapters = allChapters ?? await _curriculumGateway.GetAllAsync();
            var arcs = prerequisites ?? await _curriculumGateway.GetPrerequisitesAsync();
            var known = chapters.Select(c => c.Id).ToHashSet();
            var targets = chapterIds.Where(known.Contains).Distinct().ToList();
            var needed = ChapterProgression.ChaptersNeededFor(chapters, arcs, targets);
            var formulas = await _curriculumGateway.GetFormulasByChapterIdsAsync(needed.ToList());
            var solved = formulas.Count == 0
                ? new List<SolvedPracticeExercise>()
                : await _practiceGateway.GetSolvedAsync(wallet, formulas.Select(f => f.Id).ToList());
            var solvedByFormula = solved
                .GroupBy(s => s.FormulaId)
                .ToDictionary(g => g.Key, g => g.Select(s => s.ExerciseIndex).ToHashSet());
            var formulasByChapter = formulas.ToLookup(f => f.ChapterId);

            bool OwnExercisesCompleted(Guid chapterId)
            {
                return formulasByChapter[chapterId].All(formula =>
                {
                    var total = ExerciseCatalog.ForFormula(formula.ExercisesJson).Exercises.Count;
                    var done = solvedByFormula.GetValueOrDefault(formula.Id);

                    return Enumerable.Range(0, total).All(index => done != null && done.Contains(index));
                });
            }

            var progression = new ChapterProgression(chapters, arcs, OwnExercisesCompleted);

            return targets.ToDictionary(id => id, progression.StatusOf);
        }

        public async Task EnsureUnlockedAsync(string wallet, Guid chapterId)
        {
            var statuses = await EvaluateAsync(wallet, new[] { chapterId });

            if (statuses.TryGetValue(chapterId, out var status) && status.Locked)
            {
                throw new ForbiddenException(LockedMessage(status));
            }
        }

        public static string LockedMessage(ChapterStatus status)
        {
            var titles = status.MissingPrerequisites.Select(c => c.Title).ToList();

            return titles.Count == 0
                ? "This chapter is locked."
                : $"This chapter is locked. Finish {string.Join(", ", titles)} first.";
        }
    }
}
