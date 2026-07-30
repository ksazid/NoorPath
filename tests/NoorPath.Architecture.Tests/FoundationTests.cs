using System.Xml.Linq;
using Xunit;

namespace NoorPath.Architecture.Tests;

public sealed class FoundationTests
{
    [Fact]
    public void Source_projects_follow_module_dependency_rules()
    {
        var root = FindRepositoryRoot();

        var projects = Directory
            .EnumerateFiles(
                Path.Combine(root, "src"),
                "*.csproj",
                SearchOption.AllDirectories)
            .Select(path => ProjectInfo.Load(root, path))
            .ToArray();

        var violations = new List<string>();

        foreach (var project in projects)
        {
            foreach (var reference in project.ProjectReferences)
            {
                var target = projects.SingleOrDefault(
                    candidate => candidate.FullPath == reference);

                if (reference.StartsWith(
                    Path.Combine(root, "apps") + Path.DirectorySeparatorChar,
                    StringComparison.Ordinal))
                {
                    violations.Add(
                        $"{project.RelativePath} -> {Path.GetRelativePath(root, reference)}: " +
                        "source projects must not reference an application host.");
                }

                if (project.IsBuildingBlocks
                    && reference.StartsWith(
                        Path.Combine(root, "src", "Modules")
                            + Path.DirectorySeparatorChar,
                        StringComparison.Ordinal))
                {
                    violations.Add(
                        $"{project.RelativePath} -> {Path.GetRelativePath(root, reference)}: " +
                        "BuildingBlocks must remain module-neutral.");
                }

                if (project.Module is not null
                    && target?.Module is not null
                    && project.Module != target.Module)
                {
                    violations.Add(
                        $"{project.RelativePath} -> {target.RelativePath}: " +
                        "modules must not reference another module's implementation.");
                }

                if (project.IsDomain && target?.IsInfrastructure == true)
                {
                    violations.Add(
                        $"{project.RelativePath} -> {target.RelativePath}: " +
                        "domain projects must not depend on infrastructure.");
                }
            }

            if (project.IsDomain)
            {
                foreach (var package in project.PackageReferences.Where(IsPersistencePackage))
                {
                    violations.Add(
                        $"{project.RelativePath} -> package:{package}: " +
                        "domain projects must not depend on persistence frameworks.");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "Forbidden dependency edges:"
            + Environment.NewLine
            + string.Join(
                Environment.NewLine,
                violations.Select(x => $"- {x}")));
    }

    private static bool IsPersistencePackage(string package) =>
        package.StartsWith(
            "Microsoft.EntityFrameworkCore",
            StringComparison.Ordinal)
        || package.StartsWith(
            "Npgsql",
            StringComparison.Ordinal);

    private static string FindRepositoryRoot()
    {
        for (
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            directory is not null;
            directory = directory.Parent)
        {
            if (File.Exists(
                Path.Combine(directory.FullName, "NoorPath.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException(
            "Could not find the repository root containing NoorPath.slnx.");
    }

    private sealed record ProjectInfo(
        string FullPath,
        string RelativePath,
        string? Module,
        bool IsBuildingBlocks,
        bool IsInfrastructure,
        IReadOnlyList<string> ProjectReferences,
        IReadOnlyList<string> PackageReferences)
    {
        public bool IsDomain => Module is not null && !IsInfrastructure;

        private static string? GetModuleName(
            string projectName,
            bool isModule)
        {
            if (!isModule)
                return null;

            const string prefix = "NoorPath.";
            const string infrastructureSuffix = ".Infrastructure";

            var moduleName = projectName;

            if (moduleName.StartsWith(
                prefix,
                StringComparison.Ordinal))
            {
                moduleName = moduleName[prefix.Length..];
            }

            if (moduleName.EndsWith(
                infrastructureSuffix,
                StringComparison.Ordinal))
            {
                moduleName =
                    moduleName[..^infrastructureSuffix.Length];
            }

            return moduleName;
        }

        public static ProjectInfo Load(
            string root,
            string path)
        {
            var fullPath = Path.GetFullPath(path);
            var relativePath = Path.GetRelativePath(root, fullPath);
            var relativeParts =
                relativePath.Split(Path.DirectorySeparatorChar);

            var isModule =
                relativeParts.Length > 2
                && relativeParts[0] == "src"
                && relativeParts[1] == "Modules";

            var projectName =
                Path.GetFileNameWithoutExtension(fullPath);

            var document = XDocument.Load(fullPath);

            return new(
                fullPath,
                relativePath,
                GetModuleName(projectName, isModule),
                relativePath.StartsWith(
                    Path.Combine(
                        "src",
                        "NoorPath.BuildingBlocks")
                    + Path.DirectorySeparatorChar,
                    StringComparison.Ordinal),
                projectName.EndsWith(
                    ".Infrastructure",
                    StringComparison.Ordinal),
                document.Descendants("ProjectReference")
                    .Select(element =>
                        element.Attribute("Include")?.Value)
                    .Where(value => value is not null)
                    .Select(value =>
                        Path.GetFullPath(
                            Path.Combine(
                                Path.GetDirectoryName(fullPath)!,
                                value!)))
                    .ToArray(),
                document.Descendants("PackageReference")
                    .Select(element =>
                        element.Attribute("Include")?.Value)
                    .Where(value => value is not null)
                    .Select(value => value!)
                    .ToArray());
        }
    }
}
