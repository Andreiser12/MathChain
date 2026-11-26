using System.Text.Encodings.Web;
using System.Text.Json;
using MathChain.Domain.Entities;

namespace MathChain.Application.Curriculum
{
    public sealed record CurriculumDocuments(byte[] Manifest, IReadOnlyDictionary<Guid, byte[]> ChapterFormulas);

    public static class CurriculumManifest
    {
        public const string ManifestSchema = "mathchain.curriculum/v1";
        public const string FormulasSchema = "mathchain.chapter-formulas/v1";

        private static readonly JsonWriterOptions WriterOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Indented = false
        };

        public static CurriculumDocuments Build(IReadOnlyCollection<CurriculumChapter> chapters, IReadOnlyCollection<ChapterPrerequisite> prerequisites,
            IReadOnlyCollection<CurriculumFormula> formulas, Func<byte[], string> identify)
        {
            var formulasByChapter = formulas.ToLookup(f => f.ChapterId);
            var chapterFormulas = new Dictionary<Guid, byte[]>();
            var formulaCids = new Dictionary<Guid, string>();

            foreach (var chapter in chapters)
            {
                var own = formulasByChapter[chapter.Id].ToList();

                if (own.Count == 0)
                {
                    continue;
                }

                var document = SerializeFormulas(chapter.Id, own);
                chapterFormulas[chapter.Id] = document;
                formulaCids[chapter.Id] = identify(document);
            }

            var prerequisitesOf = prerequisites.ToLookup(p => p.ChapterId, p => p.PrerequisiteId);

            return new CurriculumDocuments(Write(writer =>
            {
                writer.WriteStartObject();
                writer.WriteStartArray("chapters");

                foreach (var chapter in chapters.OrderBy(c => Id(c.Id), StringComparer.Ordinal))
                {
                    writer.WriteStartObject();

                    if (formulaCids.TryGetValue(chapter.Id, out var cid))
                    {
                        writer.WriteString("formulas", cid);
                    }
                    else
                    {
                        writer.WriteNull("formulas");
                    }

                    writer.WriteString("id", Id(chapter.Id));
                    writer.WriteNumber("order", chapter.Order);

                    if (chapter.ParentId.HasValue)
                    {
                        writer.WriteString("parent", Id(chapter.ParentId.Value));
                    }
                    else
                    {
                        writer.WriteNull("parent");
                    }

                    writer.WriteStartArray("prerequisites");

                    foreach (var prerequisite in prerequisitesOf[chapter.Id].Select(Id).Distinct().OrderBy(id => id, StringComparer.Ordinal))
                    {
                        writer.WriteStringValue(prerequisite);
                    }

                    writer.WriteEndArray();
                    writer.WriteString("slug", chapter.ChapterSlug);
                    writer.WriteString("title", chapter.Title);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteString("schema", ManifestSchema);
                writer.WriteEndObject();
            }), chapterFormulas);
        }

        private static byte[] SerializeFormulas(Guid chapterId, IEnumerable<CurriculumFormula> formulas)
        {
            return Write(writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("chapter", Id(chapterId));
                writer.WriteStartArray("formulas");

                foreach (var formula in formulas.OrderBy(f => f.Order).ThenBy(f => f.FormulaCode, StringComparer.Ordinal).ThenBy(f => Id(f.Id), StringComparer.Ordinal))
                {
                    writer.WriteStartObject();
                    writer.WriteString("code", formula.FormulaCode);
                    writer.WriteString("description", formula.Description ?? string.Empty);
                    writer.WriteString("exercises", formula.ExercisesJson ?? string.Empty);
                    writer.WriteString("id", Id(formula.Id));
                    writer.WriteString("latex", formula.Latex);
                    writer.WriteString("model3D", formula.Model3D ?? string.Empty);
                    writer.WriteNumber("order", formula.Order);
                    writer.WriteString("requirement", formula.Requirement ?? string.Empty);
                    writer.WriteString("title", formula.Title);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteString("schema", FormulasSchema);
                writer.WriteEndObject();
            });
        }

        private static byte[] Write(Action<Utf8JsonWriter> write)
        {
            using var stream = new MemoryStream();

            using (var writer = new Utf8JsonWriter(stream, WriterOptions))
            {
                write(writer);
            }

            return stream.ToArray();
        }

        private static string Id(Guid id)
        {
            return id.ToString("D");
        }
    }
}
