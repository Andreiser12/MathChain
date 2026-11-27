using System.Text.Json;
using MathChain.Application.Contexts;
using MathChain.Application.Curriculum;
using MathChain.Application.DTOs;
using MathChain.Application.Practice;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Exceptions;
using MathChain.Tests.Shared.Fakes;

namespace MathChain.Application.UnitTests
{
    public class ChapterPrerequisiteTests
    {
        private const string Student = "0x3333333333333333333333333333333333333333";
        private const string Teacher = "0x1111111111111111111111111111111111111111";
        private const string Admin = "0x6666666666666666666666666666666666666666";

        private static readonly string TwoExercises = JsonSerializer.Serialize(new object[]
        {
            new { difficulty = "Easy", wolframQuery = "expand (x + {a})^2", latexTemplate = "(x + {a})^2", variables = new { a = new[] { 2, 2 } } },
            new { difficulty = "Hard", wolframQuery = "factor x^2 - {a*a}", latexTemplate = "x^2 - {a*a}", variables = new { a = new[] { 3, 3 } } }
        });

        private readonly InMemoryCurriculumGateway _curriculum = new();
        private readonly InMemoryPracticeExerciseGateway _practice = new();
        private readonly InMemoryClassRoomGateway _classRooms = new();
        private readonly FakeCurrentUser _currentUser = new();
        private readonly ManualTimeProvider _time = new();
        private readonly CurriculumContext _context;
        private readonly PracticeContext _practiceContext;
        private readonly CurriculumChapter _algebra;
        private readonly CurriculumChapter _geometry;
        private readonly CurriculumChapter _powers;
        private readonly CurriculumChapter _radicals;
        private readonly CurriculumChapter _logarithms;
        private readonly CurriculumChapter _properties;
        private readonly CurriculumChapter _conditions;

        public ChapterPrerequisiteTests()
        {
            var access = new AccessGuard(_currentUser, _classRooms);
            var unlocks = new ChapterUnlockService(_curriculum, _practice);
            _context = new CurriculumContext(_curriculum, access, null, unlocks);
            _practiceContext = new PracticeContext(_curriculum, _practice, access, _time, new PracticeRandom(new Random(7)), unlocks);

            _algebra = AddChapter("Algebra", null, 1);
            _geometry = AddChapter("Geometry", null, 2);
            _powers = AddChapter("Powers", _algebra, 1, withFormula: true);
            _radicals = AddChapter("Radicals", _algebra, 2);
            _logarithms = AddChapter("Logarithms", _algebra, 3, withFormula: true);
            _properties = AddChapter("Properties of radicals", _radicals, 1, withFormula: true);
            _conditions = AddChapter("Conditions of existence", _radicals, 2, withFormula: true);
        }

        private CurriculumChapter AddChapter(string title, CurriculumChapter? parent, int order, bool withFormula = false)
        {
            var chapter = new CurriculumChapter
            {
                Id = Guid.NewGuid(),
                ParentId = parent?.Id,
                Title = title,
                ChapterSlug = title.ToLowerInvariant().Replace(' ', '_'),
                Order = order
            };
            _curriculum.Chapters.Add(chapter);

            if (withFormula)
            {
                _curriculum.Formulas.Add(new CurriculumFormula { Id = Guid.NewGuid(), ChapterId = chapter.Id, Title = title + " formula", ExercisesJson = TwoExercises });
            }

            return chapter;
        }

        private CurriculumFormula FormulaOf(CurriculumChapter chapter)
        {
            return _curriculum.Formulas.Single(f => f.ChapterId == chapter.Id);
        }

        private void Solve(CurriculumChapter chapter)
        {
            var formula = FormulaOf(chapter);
            _practice.MarkSolved(Student, formula.Id, 0);
            _practice.MarkSolved(Student, formula.Id, 1);
        }

        private async Task RequireAsync(CurriculumChapter chapter, params CurriculumChapter[] prerequisites)
        {
            var previous = (_currentUser.WalletAddress, _currentUser.Role);
            _currentUser.As(Admin, UserRole.Admin);
            await _context.SetPrerequisitesAsync(chapter.Id, new SetChapterPrerequisitesRequest { PrerequisiteIds = prerequisites.Select(p => p.Id).ToList() });

            if (previous.WalletAddress != null)
            {
                _currentUser.As(previous.WalletAddress, previous.Role!.Value);
            }
            else
            {
                _currentUser.Anonymous();
            }
        }

        private async Task BuildChainAsync()
        {
            await RequireAsync(_radicals, _powers);
            await RequireAsync(_logarithms, _radicals);
            await RequireAsync(_conditions, _properties);
        }

        private async Task<Dictionary<string, CurriculumChapterResponseDto>> DetailAsync(CurriculumChapter chapter)
        {
            var detail = await _context.GetChapterDetailAsync(chapter.Id.ToString());

            return detail.SubChapters.Append(detail.Chapter).ToDictionary(c => c.Title);
        }

        [Fact]
        public async Task SetPrerequisitesAsync_ShouldSaveThemAndShowThemInTheDetail_WhenTheUserIsAnAdmin()
        {
            await BuildChainAsync();
            _currentUser.Anonymous();

            var detail = await DetailAsync(_algebra);

            Assert.Equal(new[] { _powers.Id }, detail["Radicals"].PrerequisiteIds);
            Assert.Equal(new[] { _radicals.Id }, detail["Logarithms"].PrerequisiteIds);
            Assert.Empty(detail["Powers"].PrerequisiteIds);
        }

        [Fact]
        public async Task SetPrerequisitesAsync_ShouldThrowForbiddenException_WhenTheUserIsNotAnAdmin()
        {
            _currentUser.As(Teacher, UserRole.Teacher);

            await Assert.ThrowsAsync<ForbiddenException>(() => _context.SetPrerequisitesAsync(_radicals.Id,
                new SetChapterPrerequisitesRequest { PrerequisiteIds = new List<Guid> { _powers.Id } }));

            Assert.Empty(_curriculum.Prerequisites);
        }

        [Fact]
        public async Task SetPrerequisitesAsync_ShouldRejectTheChange_WhenTheChapterIsAMainChapter()
        {
            _currentUser.As(Admin, UserRole.Admin);

            var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => _context.SetPrerequisitesAsync(_geometry.Id,
                new SetChapterPrerequisitesRequest { PrerequisiteIds = new List<Guid> { _algebra.Id } }));

            Assert.Contains("always unlocked", exception.Message);
        }

        [Fact]
        public async Task SetPrerequisitesAsync_ShouldRejectTheChange_WhenThePrerequisiteIsItselfOrHasAnotherParent()
        {
            _currentUser.As(Admin, UserRole.Admin);

            await Assert.ThrowsAsync<BusinessRuleException>(() => _context.SetPrerequisitesAsync(_radicals.Id,
                new SetChapterPrerequisitesRequest { PrerequisiteIds = new List<Guid> { _radicals.Id } }));
            var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => _context.SetPrerequisitesAsync(_logarithms.Id,
                new SetChapterPrerequisitesRequest { PrerequisiteIds = new List<Guid> { _properties.Id } }));

            Assert.Contains("same parent", exception.Message);
            Assert.Empty(_curriculum.Prerequisites);
        }

        [Fact]
        public async Task SetPrerequisitesAsync_ShouldRejectTheChangeWithTheChaptersOfTheCycle_WhenItCreatesACycle()
        {
            await BuildChainAsync();
            _currentUser.As(Admin, UserRole.Admin);

            var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => _context.SetPrerequisitesAsync(_powers.Id,
                new SetChapterPrerequisitesRequest { PrerequisiteIds = new List<Guid> { _logarithms.Id } }));

            Assert.Equal("These prerequisites would create a cycle: Logarithms → Powers → Radicals → Logarithms.", exception.Message);
            Assert.Equal(3, _curriculum.Prerequisites.Count);
            Assert.DoesNotContain(_curriculum.Prerequisites, p => p.ChapterId == _powers.Id);
        }

        [Fact]
        public async Task SetPrerequisitesAsync_ShouldRemoveThePrerequisites_WhenTheListIsEmpty()
        {
            await BuildChainAsync();

            await RequireAsync(_radicals);

            Assert.DoesNotContain(_curriculum.Prerequisites, p => p.ChapterId == _radicals.Id);
            Assert.Equal(2, _curriculum.Prerequisites.Count);
        }

        [Fact]
        public async Task GetChapterDetailAsync_ShouldKeepTheOrderOfTheChapters_WhenThereAreNoPrerequisites()
        {
            var detail = await _context.GetChapterDetailAsync(_algebra.Id.ToString());

            Assert.Equal(new[] { "Powers", "Radicals", "Logarithms" }, detail.SubChapters.Select(c => c.Title));
        }

        [Fact]
        public async Task GetChapterDetailAsync_ShouldOrderTheSubchaptersTopologically_WhenTheyHavePrerequisites()
        {
            await RequireAsync(_powers, _logarithms);

            var detail = await _context.GetChapterDetailAsync(_algebra.Id.ToString());

            Assert.Equal(new[] { "Radicals", "Logarithms", "Powers" }, detail.SubChapters.Select(c => c.Title));
        }

        [Fact]
        public async Task GetChapterDetailAsync_ShouldFallBackToTheChapterOrder_WhenACycleIsAlreadyInTheData()
        {
            _curriculum.Prerequisites.Add(new ChapterPrerequisite { ChapterId = _powers.Id, PrerequisiteId = _radicals.Id });
            _curriculum.Prerequisites.Add(new ChapterPrerequisite { ChapterId = _radicals.Id, PrerequisiteId = _powers.Id });
            _currentUser.As(Student, UserRole.Student);

            var detail = await DetailAsync(_algebra);

            Assert.Equal(new[] { "Powers", "Radicals", "Logarithms" }, (await _context.GetChapterDetailAsync(_algebra.Id.ToString())).SubChapters.Select(c => c.Title));
            Assert.True(detail["Powers"].Locked);
            Assert.True(detail["Radicals"].Locked);
        }

        [Fact]
        public async Task GetChapterDetailAsync_ShouldShowWhichChaptersAreLocked_WhenTheUserIsAStudent()
        {
            await BuildChainAsync();
            _currentUser.As(Student, UserRole.Student);

            var detail = await DetailAsync(_algebra);

            Assert.False(detail["Algebra"].Locked);
            Assert.False(detail["Powers"].Locked);
            Assert.True(detail["Radicals"].Locked);
            Assert.Equal(new[] { "Powers" }, detail["Radicals"].MissingPrerequisites);
            Assert.True(detail["Logarithms"].Locked);
            Assert.Equal(new[] { "Radicals" }, detail["Logarithms"].MissingPrerequisites);
        }

        [Fact]
        public async Task GetChapterDetailAsync_ShouldShowWhyTheParentIsLocked_WhenTheSubchapterBelongsToALockedChapter()
        {
            await BuildChainAsync();
            _currentUser.As(Student, UserRole.Student);

            var detail = await DetailAsync(_radicals);

            Assert.True(detail["Properties of radicals"].Locked);
            Assert.Equal(new[] { "Powers" }, detail["Properties of radicals"].MissingPrerequisites);
            Assert.True(detail["Conditions of existence"].Locked);
            Assert.Equal(new[] { "Powers" }, detail["Conditions of existence"].MissingPrerequisites);
        }

        [Fact]
        public async Task GetChapterDetailAsync_ShouldUnlockTheChaptersInOrder_WhenThePrerequisitesAreSolved()
        {
            await BuildChainAsync();
            _currentUser.As(Student, UserRole.Student);

            Solve(_powers);
            var afterPowers = await DetailAsync(_algebra);
            var insideRadicals = await DetailAsync(_radicals);

            Assert.True(afterPowers["Powers"].Completed);
            Assert.False(afterPowers["Radicals"].Locked);
            Assert.True(afterPowers["Logarithms"].Locked);
            Assert.False(insideRadicals["Properties of radicals"].Locked);
            Assert.True(insideRadicals["Conditions of existence"].Locked);
            Assert.Equal(new[] { "Properties of radicals" }, insideRadicals["Conditions of existence"].MissingPrerequisites);

            Solve(_properties);
            Assert.True((await DetailAsync(_algebra))["Logarithms"].Locked);

            Solve(_conditions);
            var afterRadicals = await DetailAsync(_algebra);

            Assert.True(afterRadicals["Radicals"].Completed);
            Assert.False(afterRadicals["Logarithms"].Locked);
            Assert.Empty(afterRadicals["Logarithms"].MissingPrerequisites);
        }

        [Fact]
        public async Task GetChapterDetailAsync_ShouldKeepTheChapterLocked_WhenThePrerequisiteIsOnlyPartlySolved()
        {
            await BuildChainAsync();
            _currentUser.As(Student, UserRole.Student);
            _practice.MarkSolved(Student, FormulaOf(_powers).Id, 0);

            var detail = await DetailAsync(_algebra);

            Assert.False(detail["Powers"].Completed);
            Assert.True(detail["Radicals"].Locked);
        }

        [Fact]
        public async Task GetChapterDetailAsync_ShouldUnlockNothing_WhenTheExercisesWereSolvedByAnotherStudent()
        {
            await BuildChainAsync();
            var formula = FormulaOf(_powers);
            _practice.MarkSolved(Teacher, formula.Id, 0);
            _practice.MarkSolved(Teacher, formula.Id, 1);
            _currentUser.As(Student, UserRole.Student);

            Assert.True((await DetailAsync(_algebra))["Radicals"].Locked);
        }

        [Fact]
        public async Task GetChapterDetailAsync_ShouldCountThePrerequisiteAsCompleted_WhenItIsEmpty()
        {
            var comingSoon = AddChapter("Coming soon", _geometry, 1);
            var vectors = AddChapter("Vectors", _geometry, 2, withFormula: true);
            await RequireAsync(vectors, comingSoon);
            _currentUser.As(Student, UserRole.Student);

            var detail = await DetailAsync(_geometry);

            Assert.True(detail["Coming soon"].Completed);
            Assert.False(detail["Vectors"].Locked);
        }

        [Fact]
        public async Task GetChapterDetailAsync_ShouldShowNoLocks_WhenTheUserIsATeacherOrAVisitor()
        {
            await BuildChainAsync();

            _currentUser.As(Teacher, UserRole.Teacher);
            var teacherView = await DetailAsync(_algebra);
            _currentUser.Anonymous();
            var visitorView = await DetailAsync(_algebra);

            Assert.All(teacherView.Values.Concat(visitorView.Values), c => Assert.False(c.Locked));
        }

        [Fact]
        public async Task StartExerciseAsync_ShouldThrowForbiddenException_WhenTheChapterIsLocked()
        {
            await BuildChainAsync();
            _currentUser.As(Student, UserRole.Student);

            var exception = await Assert.ThrowsAsync<ForbiddenException>(() => _practiceContext.StartExerciseAsync(FormulaOf(_conditions).Id, 0));
            await Assert.ThrowsAsync<ForbiddenException>(() => _practiceContext.GetFormulaAsync(FormulaOf(_logarithms).Id));

            Assert.Equal("This chapter is locked. Finish Powers first.", exception.Message);
            Assert.Empty(_practice.Exercises);
        }

        [Fact]
        public async Task StartExerciseAsync_ShouldStartTheExercise_WhenThePrerequisitesAreCompleted()
        {
            await BuildChainAsync();
            _currentUser.As(Student, UserRole.Student);
            Solve(_powers);
            Solve(_properties);

            var exercise = await _practiceContext.StartExerciseAsync(FormulaOf(_conditions).Id, 0);

            Assert.NotEqual(Guid.Empty, exercise.ExerciseId);
        }

        [Fact]
        public async Task StartExerciseAsync_ShouldNotBlockTheUser_WhenTheUserIsATeacher()
        {
            await BuildChainAsync();
            _currentUser.As(Teacher, UserRole.Teacher);

            var exercise = await _practiceContext.StartExerciseAsync(FormulaOf(_logarithms).Id, 0);

            Assert.NotEqual(Guid.Empty, exercise.ExerciseId);
        }

        [Fact]
        public async Task SubmitAnswerAsync_ShouldThrowForbiddenException_WhenTheChapterIsLockedAfterTheExerciseStarted()
        {
            _currentUser.As(Student, UserRole.Student);
            var exercise = await _practiceContext.StartExerciseAsync(FormulaOf(_logarithms).Id, 0);
            await RequireAsync(_logarithms, _radicals);

            await Assert.ThrowsAsync<ForbiddenException>(() => _practiceContext.SubmitAnswerAsync(exercise.ExerciseId,
                new SubmitPracticeAnswerRequest { Answer = "x^2 + 4x + 4" }));

            Assert.Null(_practice.Exercises.Single().SolvedAt);
        }

        [Fact]
        public async Task ClaimChapterCertificateAsync_ShouldThrowForbiddenException_WhenTheChapterIsLocked()
        {
            await BuildChainAsync();
            var registry = new FakeCertificateRegistry();
            var certificates = new CertificateContext(new InMemoryCertificateGateway(), registry, _classRooms, _curriculum, _practice,
                new AccessGuard(_currentUser, _classRooms), _time, unlocks: new ChapterUnlockService(_curriculum, _practice));
            _currentUser.As(Student, UserRole.Student);
            Solve(_conditions);

            await Assert.ThrowsAsync<ForbiddenException>(() => certificates.ClaimChapterCertificateAsync(_conditions.Id));

            Assert.Empty(registry.Tokens);
        }

        [Fact]
        public void ChaptersNeededFor_ShouldIncludeOnlyTheSubtreesThatDecideTheStatus_WhenAChapterHasAPrerequisite()
        {
            var prerequisites = new List<ChapterPrerequisite>
            {
                new() { ChapterId = _logarithms.Id, PrerequisiteId = _radicals.Id }
            };

            var needed = ChapterProgression.ChaptersNeededFor(_curriculum.Chapters, prerequisites, new[] { _logarithms.Id });

            Assert.Equal(new[] { _logarithms.Id, _radicals.Id, _properties.Id, _conditions.Id }.OrderBy(id => id), needed.OrderBy(id => id));
        }
    }
}
