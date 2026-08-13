// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.Versioning;
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
    const string FrameworkParameter = "Framework";
    const string PackageManagerParameter = "packageManager";
    const string PackageManager = "yarn";

    /// <inheritdoc/>
    public async Task<bool> EnsureScaffolded(DirectoryInfo targetDirectory, string projectName, TextWriter output)
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

        var parameters = BuildParameters(template);
        await output.WriteLineAsync($"Applying template '{TemplateShortName}' as '{projectName}' targeting '{parameters[FrameworkParameter]}'...");

        var creator = new TemplateCreator(environment);
        var result = await creator.InstantiateAsync(
            template,
            projectName,
            projectName,
            targetDirectory.FullName,
            parameters);

        if (result.Status != CreationResultStatus.Success)
        {
            throw new ScaffoldingFailed($"Failed to instantiate template '{TemplateShortName}': {result.ErrorMessage}");
        }

        if (SampleSlice.Remove(targetDirectory))
        {
            await output.WriteLineAsync("Removed the template's sample slice.");
        }

        await output.WriteLineAsync("Scaffolding complete.");
        return true;
    }

    /// <summary>
    /// Builds the template parameters. Every parameter the template declares is supplied explicitly — the
    /// programmatic Template Engine API does not apply a symbol's declared <c>defaultValue</c> for a parameter the
    /// caller leaves out, and an unbound symbol emits its raw placeholder token (<c>TARGET_FRAMEWORK</c>) into the
    /// generated files.
    /// </summary>
    /// <param name="template">The <see cref="ITemplateInfo"/> being instantiated.</param>
    /// <returns>The parameters to instantiate the template with.</returns>
    static Dictionary<string, string?> BuildParameters(ITemplateInfo template) => new(StringComparer.Ordinal)
    {
        [FrameworkParameter] = ResolveTargetFramework(template),
        [PackageManagerParameter] = PackageManager,
    };

    /// <summary>
    /// Resolves the target framework to render for — the one the renderer itself runs on, so the rendered
    /// application builds with the same SDK. Falls back to the template's own default when that moniker is not one
    /// of the choices the template offers.
    /// </summary>
    /// <param name="template">The <see cref="ITemplateInfo"/> being instantiated.</param>
    /// <returns>The target framework moniker.</returns>
    static string ResolveTargetFramework(ITemplateInfo template)
    {
        var running = RunningTargetFramework();
        var parameter = template.ParameterDefinitions.FirstOrDefault(candidate => candidate.Name == FrameworkParameter);

        if (parameter?.Choices is null || parameter.Choices.Count == 0)
        {
            return running;
        }

        return parameter.Choices.ContainsKey(running) ? running : parameter.DefaultValue ?? running;
    }

    static string RunningTargetFramework()
    {
        var frameworkName = typeof(TemplateEngineProjectScaffolder).Assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
        if (frameworkName is null)
        {
            return $"net{Environment.Version.Major}.{Environment.Version.Minor}";
        }

        var version = new FrameworkName(frameworkName).Version;
        return $"net{version.Major}.{version.Minor}";
    }

    /// <summary>
    /// The components the template engine host is built with — each library's own complete <c>AllComponents</c>
    /// set rather than a hand-picked subset. The Runnable Projects set carries the macro components that evaluate
    /// generated symbols and value forms; without them the generator still renames files from <c>sourceName</c>,
    /// but every content substitution resolves to nothing and the raw placeholder tokens
    /// (<c>TARGET_FRAMEWORK</c>, <c>PROJECT_GUID</c>, <c>CratisApp</c>) are written verbatim.
    /// </summary>
    /// <returns>The built-in components.</returns>
    static IReadOnlyList<(Type InterfaceType, IIdentifiedComponent Instance)> BuiltInComponents() =>
    [
        .. Microsoft.TemplateEngine.Edge.Components.AllComponents,
        .. Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Components.AllComponents,
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
