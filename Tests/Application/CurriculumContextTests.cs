using MathChain.Application.Contexts;
using MathChain.Application.DTOs;
using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Exceptions;
using MathChain.Tests.Fakes;

namespace MathChain.Tests.Application
{
    public class CurriculumContextTests
    {
        private const string Admin = "0xadmin";

        private readonly InMemoryCurriculumGateway _gateway = new();
        private readonly FakeIpfsStorageService _ipfs = new();
        private readonly FakeCurrentUser _currentUser = new();
        private readonly CurriculumContext _context;

        public CurriculumContextTests()
        {
            _context = new CurriculumContext(_gateway, new AccessGuard(_currentUser, new InMemoryClassRoomGateway()), _ipfs);
            _currentUser.As(Admin, UserRole.Admin);
        }

        private CurriculumChapter AddChapter(string title, string slug, CurriculumChapter? parent = null, int order = 0, int formulasCount = 0, string category = "")
        {
            var chapter = new CurriculumChapter
            {
                Id = Guid.NewGuid(),
                ParentId = parent?.Id,
                Title = title,
                ChapterSlug = slug,
                Category = category,
                Order = order,
                FormulasCount = formulasCount,
                Version = 1,
                UpdatedAt = DateTime.UtcNow
            };
            _gateway.Chapters.Add(chapter);
            return chapter;
        }

        private CurriculumFormula AddFormula(CurriculumChapter chapter, string code, int order)
        {
            var formula = new CurriculumFormula { Id = Guid.NewGuid(), ChapterId = chapter.Id, FormulaCode = code, Title = code, Latex = "x", Order = order, CreatedAt = DateTime.UtcNow };
            _gateway.Formulas.Add(formula);
            return formula;
        }

        [Fact]
        public async Task GetRootChaptersAsync_Anonymous_ReturnsOrderedRootsWithDescendantTotals()
        {
            _currentUser.Anonymous();
            var analysis = AddChapter("Analiza", "analiza", order: 2, formulasCount: 1);
            var integrals = AddChapter("Integrale", "integrale", analysis, formulasCount: 3);
            AddChapter("Integrale definite", "integrale_definite", integrals, formulasCount: 4);
            AddChapter("Algebra", "algebra", order: 1);

            var roots = await _context.GetRootChaptersAsync();

            Assert.Equal(new[] { "Algebra", "Analiza" }, roots.Select(r => r.Title));
            var analysisDto = roots[1];
            Assert.Equal(1, analysisDto.SubChaptersCount);
            Assert.Equal(2, analysisDto.TotalSubChaptersCount);
            Assert.Equal(1, analysisDto.FormulasCount);
            Assert.Equal(8, analysisDto.TotalFormulasCount);
        }

        [Fact]
        public async Task GetChapterDetailAsync_BySlugIgnoringCase_ReturnsBreadcrumbsAndOrderedSubChapters()
        {
            _currentUser.Anonymous();
            var analysis = AddChapter("Analiza", "analiza");
            var integrals = AddChapter("Integrale", "integrale", analysis);
            AddChapter("Definite", "definite", integrals, order: 2);
            AddChapter("Nedefinite", "nedefinite", integrals, order: 1);

            var detail = await _context.GetChapterDetailAsync("  INTEGRALE ");

            Assert.Equal(integrals.Id, detail.Chapter.Id);
            Assert.Equal(new[] { "analiza", "integrale" }, detail.Breadcrumbs.Select(b => b.Slug));
            Assert.Equal(new[] { "Nedefinite", "Definite" }, detail.SubChapters.Select(s => s.Title));
        }

        [Fact]
        public async Task GetChapterDetailAsync_ById_ReturnsChapter()
        {
            var chapter = AddChapter("Analiza", "analiza");

            var detail = await _context.GetChapterDetailAsync(chapter.Id.ToString());

            Assert.Equal("analiza", detail.Chapter.ChapterSlug);
            Assert.Single(detail.Breadcrumbs);
        }

        [Fact]
        public async Task GetChapterDetailAsync_UnknownChapter_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _context.GetChapterDetailAsync("nu-exista"));
        }

        [Fact]
        public async Task GetChapterDetailAsync_BlankIdentifier_ThrowsBusinessRuleException()
        {
            await Assert.ThrowsAsync<BusinessRuleException>(() => _context.GetChapterDetailAsync("  "));
        }

        [Fact]
        public async Task GetFormulasByChapterAsync_Anonymous_ReturnsFormulasInOrder()
        {
            _currentUser.Anonymous();
            var chapter = AddChapter("Integrale", "integrale");
            AddFormula(chapter, "second", 2);
            AddFormula(chapter, "first", 1);

            var formulas = await _context.GetFormulasByChapterAsync("integrale");

            Assert.Equal(new[] { "first", "second" }, formulas.Select(f => f.FormulaCode));
        }

        [Fact]
        public async Task GetFormulasByChapterAsync_UnknownChapter_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _context.GetFormulasByChapterAsync(Guid.NewGuid().ToString()));
        }

        [Fact]
        public async Task CreateChapterAsync_Admin_BuildsSlugFromTitle()
        {
            var created = await _context.CreateChapterAsync(new CreateChapterRequest { Title = "  Integrale Definite  ", Category = "Analiza" });

            Assert.Equal("Integrale Definite", created.Title);
            Assert.Equal("integrale_definite", created.ChapterSlug);
            Assert.Equal(1, created.Version);
            Assert.Single(_gateway.Chapters);
        }

        [Fact]
        public async Task CreateChapterAsync_TitleWithRomanianDiacritics_KeepsLettersInSlug()
        {
            var created = await _context.CreateChapterAsync(new CreateChapterRequest { Title = "Funcții și șiruri în ână" });

            Assert.Equal("functii_si_siruri_in_ana", created.ChapterSlug);
        }

        [Fact]
        public async Task CreateChapterAsync_TitleWithoutLettersOrDigits_ThrowsBusinessRuleException()
        {
            await Assert.ThrowsAsync<BusinessRuleException>(() => _context.CreateChapterAsync(new CreateChapterRequest { Title = "∫∑" }));
            Assert.Empty(_gateway.Chapters);
        }

        [Fact]
        public async Task CreateChapterAsync_DuplicateSlug_AppendsRandomSuffix()
        {
            AddChapter("Integrale", "integrale");

            var created = await _context.CreateChapterAsync(new CreateChapterRequest { Title = "Integrale" });

            Assert.Matches("^integrale_[0-9a-f]{6}$", created.ChapterSlug);
        }

        [Fact]
        public async Task CreateChapterAsync_WithoutCategory_InheritsParentCategoryOrTitle()
        {
            var withCategory = AddChapter("Integrale", "integrale", category: "Analiza");
            var withoutCategory = AddChapter("Geometrie", "geometrie");

            var first = await _context.CreateChapterAsync(new CreateChapterRequest { Title = "Definite", ParentId = withCategory.Id });
            var second = await _context.CreateChapterAsync(new CreateChapterRequest { Title = "Triunghiuri", ParentId = withoutCategory.Id });

            Assert.Equal("Analiza", first.Category);
            Assert.Equal("Geometrie", second.Category);
            Assert.Equal(withCategory.Id, first.ParentId);
        }

        [Fact]
        public async Task CreateChapterAsync_UnknownParent_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _context.CreateChapterAsync(new CreateChapterRequest { Title = "Orfan", ParentId = Guid.NewGuid(), Category = "X" }));
            Assert.Empty(_gateway.Chapters);
        }

        [Fact]
        public async Task CreateChapterAsync_Teacher_ThrowsForbiddenException()
        {
            _currentUser.As("0xteacher", UserRole.Teacher);

            await Assert.ThrowsAsync<ForbiddenException>(() => _context.CreateChapterAsync(new CreateChapterRequest { Title = "Hack" }));
            Assert.Empty(_gateway.Chapters);
        }

        [Fact]
        public async Task CreateChapterAsync_Anonymous_ThrowsUnauthorizedException()
        {
            _currentUser.Anonymous();

            await Assert.ThrowsAsync<UnauthorizedException>(() => _context.CreateChapterAsync(new CreateChapterRequest { Title = "Hack" }));
        }

        [Fact]
        public async Task UpdateChapterAsync_WithoutSlug_KeepsSlugAndUpdatesFields()
        {
            var chapter = AddChapter("Integrale", "integrale", formulasCount: 5, category: "Analiza");

            var updated = await _context.UpdateChapterAsync(chapter.Id, new UpdateChapterRequest { Title = " Integrale noi ", Order = 3, FormulasCount = 0 });

            Assert.Equal("Integrale noi", updated.Title);
            Assert.Equal("integrale", updated.ChapterSlug);
            Assert.Equal("Analiza", updated.Category);
            Assert.Equal(3, updated.Order);
            Assert.Equal(5, updated.FormulasCount);
        }

        [Fact]
        public async Task UpdateChapterAsync_SlugTakenByAnotherChapter_AppendsRandomSuffix()
        {
            AddChapter("Algebra", "algebra");
            var chapter = AddChapter("Integrale", "integrale");

            var updated = await _context.UpdateChapterAsync(chapter.Id, new UpdateChapterRequest { Title = "Integrale", ChapterSlug = "Algebra" });

            Assert.Matches("^algebra_[0-9a-f]{6}$", updated.ChapterSlug);
        }

        [Fact]
        public async Task UpdateChapterAsync_SlugWithoutLettersOrDigits_ThrowsBusinessRuleException()
        {
            var chapter = AddChapter("Integrale", "integrale");

            await Assert.ThrowsAsync<BusinessRuleException>(() => _context.UpdateChapterAsync(chapter.Id, new UpdateChapterRequest { Title = "Integrale", ChapterSlug = "∫" }));
            Assert.Equal("integrale", _gateway.Chapters[0].ChapterSlug);
        }

        [Fact]
        public async Task UpdateChapterAsync_UnknownChapter_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _context.UpdateChapterAsync(Guid.NewGuid(), new UpdateChapterRequest { Title = "X" }));
        }

        [Fact]
        public async Task DeleteChapterAsync_Admin_RemovesChapter()
        {
            var chapter = AddChapter("Integrale", "integrale");

            Assert.True(await _context.DeleteChapterAsync(chapter.Id));
            Assert.Empty(_gateway.Chapters);
        }

        [Fact]
        public async Task DeleteChapterAsync_Teacher_ThrowsForbiddenAndKeepsChapter()
        {
            var chapter = AddChapter("Integrale", "integrale");
            _currentUser.As("0xteacher", UserRole.Teacher);

            await Assert.ThrowsAsync<ForbiddenException>(() => _context.DeleteChapterAsync(chapter.Id));
            Assert.Single(_gateway.Chapters);
        }

        [Fact]
        public async Task DeleteChapterAsync_UnknownChapter_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _context.DeleteChapterAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task UpdateIpfsHashAsync_Admin_UpdatesHash()
        {
            AddChapter("Integrale", "integrale");

            var updated = await _context.UpdateIpfsHashAsync("integrale", "QmNew");

            Assert.Equal("QmNew", updated.IpfsHash);
        }

        [Fact]
        public async Task UpdateIpfsHashAsync_UnknownChapter_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _context.UpdateIpfsHashAsync("nu-exista", "QmNew"));
        }

        [Fact]
        public async Task CreateFormulaAsync_Admin_GeneratesCodeAndOrderAndUpdatesChapter()
        {
            var chapter = AddChapter("Integrale", "integrale");
            AddFormula(chapter, "integrale-f1", 1);

            var created = await _context.CreateFormulaAsync(chapter.Id, new CreateFormulaRequest { Title = " Integrala lui x ", Latex = @" \int x\,dx " });

            Assert.Equal("integrale-f2", created.FormulaCode);
            Assert.Equal(2, created.Order);
            Assert.Equal("Integrala lui x", created.Title);
            Assert.Equal(@"\int x\,dx", created.Latex);
            var storedChapter = _gateway.Chapters[0];
            Assert.Equal(2, storedChapter.FormulasCount);
            Assert.Equal("QmFake1", storedChapter.IpfsHash);
            Assert.Contains(@"\\int x\\,dx", Assert.Single(_ipfs.SavedFiles));
        }

        [Fact]
        public async Task CreateFormulaAsync_ExplicitCode_IsTrimmed()
        {
            var chapter = AddChapter("Integrale", "integrale");

            var created = await _context.CreateFormulaAsync(chapter.Id, new CreateFormulaRequest { Title = "Formula", Latex = "x", FormulaCode = "  INT_01  ", Order = 7 });

            Assert.Equal("INT_01", created.FormulaCode);
            Assert.Equal(7, created.Order);
        }

        [Fact]
        public async Task CreateFormulaAsync_IpfsUnavailable_StillSavesFormulaAndCount()
        {
            var chapter = AddChapter("Integrale", "integrale");
            chapter.IpfsHash = "QmOld";
            _ipfs.ShouldFail = true;

            await _context.CreateFormulaAsync(chapter.Id, new CreateFormulaRequest { Title = "Formula", Latex = "x" });

            Assert.Single(_gateway.Formulas);
            Assert.Equal(1, _gateway.Chapters[0].FormulasCount);
            Assert.Equal("QmOld", _gateway.Chapters[0].IpfsHash);
        }

        [Fact]
        public async Task CreateFormulaAsync_WithoutIpfsService_SavesFormula()
        {
            var context = new CurriculumContext(_gateway, new AccessGuard(_currentUser, new InMemoryClassRoomGateway()));
            var chapter = AddChapter("Integrale", "integrale");

            await context.CreateFormulaAsync(chapter.Id, new CreateFormulaRequest { Title = "Formula", Latex = "x" });

            Assert.Single(_gateway.Formulas);
            Assert.Equal(1, _gateway.Chapters[0].FormulasCount);
        }

        [Fact]
        public async Task CreateFormulaAsync_MissingLatex_ThrowsBusinessRuleException()
        {
            var chapter = AddChapter("Integrale", "integrale");

            await Assert.ThrowsAsync<BusinessRuleException>(() => _context.CreateFormulaAsync(chapter.Id, new CreateFormulaRequest { Title = "Formula", Latex = " " }));
            Assert.Empty(_gateway.Formulas);
        }

        [Fact]
        public async Task CreateFormulaAsync_UnknownChapter_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _context.CreateFormulaAsync(Guid.NewGuid(), new CreateFormulaRequest { Title = "Formula", Latex = "x" }));
        }

        [Fact]
        public async Task CreateFormulaAsync_Teacher_ThrowsForbiddenException()
        {
            var chapter = AddChapter("Integrale", "integrale");
            _currentUser.As("0xteacher", UserRole.Teacher);

            await Assert.ThrowsAsync<ForbiddenException>(() => _context.CreateFormulaAsync(chapter.Id, new CreateFormulaRequest { Title = "Formula", Latex = "x" }));
            Assert.Empty(_gateway.Formulas);
        }

        [Fact]
        public async Task UpdateFormulaAsync_Admin_UpdatesFormulaAndResyncsChapter()
        {
            var chapter = AddChapter("Integrale", "integrale");
            var formula = AddFormula(chapter, "int-1", 1);

            var updated = await _context.UpdateFormulaAsync(formula.Id, new UpdateFormulaRequest { Title = " Noua ", Latex = " y ", Order = 4 });

            Assert.Equal("Noua", updated.Title);
            Assert.Equal("y", updated.Latex);
            Assert.Equal("int-1", updated.FormulaCode);
            Assert.Equal(4, _gateway.Formulas[0].Order);
            Assert.Equal("QmFake1", _gateway.Chapters[0].IpfsHash);
        }

        [Fact]
        public async Task UpdateFormulaAsync_UnknownFormula_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _context.UpdateFormulaAsync(Guid.NewGuid(), new UpdateFormulaRequest { Title = "X", Latex = "x" }));
        }

        [Fact]
        public async Task DeleteFormulaAsync_Admin_RemovesFormulaAndUpdatesCount()
        {
            var chapter = AddChapter("Integrale", "integrale", formulasCount: 2);
            var formula = AddFormula(chapter, "int-1", 1);
            AddFormula(chapter, "int-2", 2);

            Assert.True(await _context.DeleteFormulaAsync(formula.Id));

            Assert.Single(_gateway.Formulas);
            Assert.Equal(1, _gateway.Chapters[0].FormulasCount);
        }

        [Fact]
        public async Task DeleteFormulaAsync_UnknownFormula_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _context.DeleteFormulaAsync(Guid.NewGuid()));
        }
    }
}
