using MathChain.Domain.Entities;

namespace MathChain.Application.Curriculum
{
    public sealed record ChapterStatus(Guid ChapterId, bool Completed, bool Locked, IReadOnlyList<CurriculumChapter> MissingPrerequisites);

    public sealed class ChapterProgression
    {
        private readonly IReadOnlyDictionary<Guid, CurriculumChapter> _chapters;
        private readonly ILookup<Guid?, CurriculumChapter> _children;
        private readonly ILookup<Guid, Guid> _prerequisites;
        private readonly Func<Guid, bool> _ownExercisesCompleted;
        private readonly Dictionary<Guid, bool> _completed = new();
        private readonly Dictionary<Guid, ChapterStatus> _statuses = new();

        public ChapterProgression(IReadOnlyCollection<CurriculumChapter> chapters, IReadOnlyCollection<ChapterPrerequisite> prerequisites,
            Func<Guid, bool> ownExercisesCompleted)
        {
            _chapters = chapters.ToDictionary(c => c.Id);
            _children = chapters.ToLookup(c => c.ParentId);
            _prerequisites = prerequisites.ToLookup(p => p.ChapterId, p => p.PrerequisiteId);
            _ownExercisesCompleted = ownExercisesCompleted;
        }

        public static IReadOnlySet<Guid> ChaptersNeededFor(IReadOnlyCollection<CurriculumChapter> chapters,
            IReadOnlyCollection<ChapterPrerequisite> prerequisites, IEnumerable<Guid> targetIds)
        {
            var byId = chapters.ToDictionary(c => c.Id);
            var children = chapters.ToLookup(c => c.ParentId);
            var prerequisitesOf = prerequisites.ToLookup(p => p.ChapterId, p => p.PrerequisiteId);
            var needed = new HashSet<Guid>();

            foreach (var targetId in targetIds)
            {
                AddSubtree(targetId);

                for (var current = byId.GetValueOrDefault(targetId); current != null; current = Parent(current))
                {
                    foreach (var prerequisiteId in prerequisitesOf[current.Id])
                    {
                        AddSubtree(prerequisiteId);
                    }
                }
            }

            return needed;

            CurriculumChapter? Parent(CurriculumChapter chapter)
            {
                return chapter.ParentId.HasValue ? byId.GetValueOrDefault(chapter.ParentId.Value) : null;
            }

            void AddSubtree(Guid rootId)
            {
                var pending = new Stack<Guid>();
                pending.Push(rootId);

                while (pending.Count > 0)
                {
                    var id = pending.Pop();

                    if (byId.ContainsKey(id) && needed.Add(id))
                    {
                        foreach (var child in children[id])
                        {
                            pending.Push(child.Id);
                        }
                    }
                }
            }
        }

        public bool IsCompleted(Guid chapterId)
        {
            if (_completed.TryGetValue(chapterId, out var known))
            {
                return known;
            }

            var completed = _ownExercisesCompleted(chapterId) && _children[chapterId].All(child => IsCompleted(child.Id));
            _completed[chapterId] = completed;

            return completed;
        }

        public ChapterStatus StatusOf(Guid chapterId)
        {
            if (_statuses.TryGetValue(chapterId, out var known))
            {
                return known;
            }

            if (!_chapters.TryGetValue(chapterId, out var chapter))
            {
                throw new ArgumentException($"Chapter {chapterId} is not part of the curriculum.", nameof(chapterId));
            }

            var completed = IsCompleted(chapterId);
            ChapterStatus status;

            if (chapter.ParentId == null)
            {
                status = new ChapterStatus(chapterId, completed, false, Array.Empty<CurriculumChapter>());
            }
            else
            {
                var missing = _prerequisites[chapterId]
                    .Where(id => _chapters.ContainsKey(id) && !IsCompleted(id))
                    .Select(id => _chapters[id])
                    .OrderBy(c => c.Order)
                    .ThenBy(c => c.Title)
                    .ToList();
                var parent = _chapters.TryGetValue(chapter.ParentId.Value, out var parentChapter) ? StatusOf(parentChapter.Id) : null;

                status = parent is { Locked: true }
                    ? new ChapterStatus(chapterId, completed, true, parent.MissingPrerequisites)
                    : missing.Count > 0
                        ? new ChapterStatus(chapterId, completed, true, missing)
                        : new ChapterStatus(chapterId, completed, false, Array.Empty<CurriculumChapter>());
            }

            _statuses[chapterId] = status;

            return status;
        }
    }
}
