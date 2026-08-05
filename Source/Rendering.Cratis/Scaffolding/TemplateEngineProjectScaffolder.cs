// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Abstractions.TemplatePackage;
using Microsoft.TemplateEngine.Edge;
using Microsoft.TemplateEngine.Edge.BuiltInManagedProvider;
using Microsoft.TemplateEngine.Edge.Installers.Folder;
using Microsoft.TemplateEngine.Edge.Installers.NuGet;
using Microsoft.TemplateEngine.Edge.Mount.Archive;
using Microsoft.TemplateEngine.Edge.Mount.FileSystem;
using Microsoft.TemplateEngine.Edge.Settings;
using Microsoft.TemplateEngine.Edge.Template;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects;

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

/// <summary>
/// Scaffolds a new target application from the <c>Cratis.Templates</c> NuGet package, using the .NET Template
/// Engine's programmatic API directly — never shelling out to <c>dotnet new</c>. The package is located in the
/// local NuGet cache, where the running application's own <c>Cratis.Templates</c> package reference already
/// restored it, so no separate network fetch happens at scaffold time.
/// </summary>
public class TemplateEngineProjectScaffolder : IProjectScaffolder
{
    const string PackageId = "Cratis.Templates";
    const string TemplateShortName = "cratis";

    /// <inheritdoc/>
    public async Task<bool> EnsureScaffolded(DirectoryInfo targetDirectory, TextWriter output)
    {
        if (HasExistingProject(targetDirectory))
        {
            return false;
        }

        await output.WriteLineAsync($"Scaffolding a new Cratis application in '{targetDirectory.FullName}'...");

        var packagePath = LocateLocalPackage(PackageId) ?? throw new TemplatePackageNotFound(PackageId);

        var host = new DefaultTemplateEngineHost("Cratis.Stage.Rendering.Cratis", "1.0.0", builtIns: BuiltInComponents());
        using var environment = new EngineEnvironmentSettings(host, virtualizeSettings: true);
        using var packageManager = new TemplatePackageManager(environment);

        await output.WriteLineAsync($"Installing template package from '{packagePath}'...");
        var provider = packageManager.GetBuiltInManagedProvider();
        var installResults = await provider.InstallAsync([new InstallRequest(packagePath)], CancellationToken.None);
        var installResult = installResults.Single();
        if (!installResult.Success)
        {
            throw new ScaffoldingFailed($"Failed to install '{PackageId}': {installResult.ErrorMessage}");
        }

        await packageManager.RebuildTemplateCacheAsync(CancellationToken.None);
        var templates = await packageManager.GetTemplatesAsync(CancellationToken.None);
        var template = templates.FirstOrDefault(candidate => candidate.ShortNameList.Contains(TemplateShortName)) ??
            throw new TemplateNotFound(TemplateShortName);

        await output.WriteLineAsync($"Applying template '{TemplateShortName}'...");
        var creator = new TemplateCreator(environment);
        var result = await creator.InstantiateAsync(
            template,
            targetDirectory.Name,
            targetDirectory.Name,
            targetDirectory.FullName,
            new Dictionary<string, string?>());

        if (result.Status != CreationResultStatus.Success)
        {
            throw new ScaffoldingFailed($"Failed to instantiate template '{TemplateShortName}': {result.ErrorMessage}");
        }

        await output.WriteLineAsync("Scaffolding complete.");
        return true;
    }

    static IReadOnlyList<(Type InterfaceType, IIdentifiedComponent Instance)> BuiltInComponents() =>
    [
        (typeof(ITemplatePackageProviderFactory), new GlobalSettingsTemplatePackageProviderFactory()),
        (typeof(IInstallerFactory), new NuGetInstallerFactory()),
        (typeof(IInstallerFactory), new FolderInstallerFactory()),
        (typeof(IGenerator), new RunnableProjectGenerator()),
        (typeof(IMountPointFactory), new ZipFileMountPointFactory()),
        (typeof(IMountPointFactory), new FileSystemMountPointFactory()),
    ];

    static bool HasExistingProject(DirectoryInfo targetDirectory) =>
        targetDirectory.Exists &&
        (targetDirectory.EnumerateFiles("*.csproj").Any() || targetDirectory.EnumerateFiles("*.sln").Any());

    static string? LocateLocalPackage(string packageId)
    {
        var packagesFolder = Environment.GetEnvironmentVariable("NUGET_PACKAGES") ??
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");

        var packageFolder = Path.Combine(packagesFolder, packageId.ToLowerInvariant());
        if (!Directory.Exists(packageFolder))
        {
            return null;
        }

        var latestVersionFolder = Directory.GetDirectories(packageFolder).OrderDescending(StringComparer.OrdinalIgnoreCase).FirstOrDefault();
        return latestVersionFolder is null ? null : Directory.GetFiles(latestVersionFolder, "*.nupkg").FirstOrDefault();
    }
}
