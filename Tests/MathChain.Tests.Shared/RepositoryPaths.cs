namespace MathChain.Tests.Shared
{
    public static class RepositoryPaths
    {
        public static string Root
        {
            get
            {
                var directory = new DirectoryInfo(AppContext.BaseDirectory);

                while (directory != null && !File.Exists(Path.Combine(directory.FullName, "MathChain.sln")))
                {
                    directory = directory.Parent;
                }

                return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
            }
        }

        public static string CurriculumDirectory => Path.Combine(Root, "MathChain.Blazor", "wwwroot", "data", "curriculum");
    }
}
