// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Host.Published;

namespace Cratis.Stage.Host.Export;

/// <summary>
/// Produces a versioned, publishable application package from a model.
/// </summary>
/// <remarks>
/// <para>
/// The package is a folder: <c language="shell">manifest.json</c>, the model files under <c language="shell">model/</c> (verified here to compile and
/// pass the semantic admission the Stage applies when it loads a model), and a <c language="shell">Dockerfile</c> that bakes the package
/// into an image built <c language="shell">FROM cratis/stage:&lt;runtime version&gt;</c>. The same model and arguments always give the
/// same bytes.
/// </para>
/// </remarks>
public static partial class StageExport
{
    [GeneratedRegex(@"^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex SemanticVersion { get; }

    [GeneratedRegex(@"^[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?$", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex PublishedNameLabel { get; }

    /// <summary>
    /// Exports the package.
    /// </summary>
    /// <param name="request">What to export.</param>
    /// <returns>The folder written to.</returns>
    /// <exception cref="StageExportRefused">The request is invalid, the model fails admission or has no authentication policy.</exception>
    public static async Task<string> Run(StageExportRequest request)
    {
        var (_, _, folder) = await Produce(request);

        return folder;
    }

    internal static async Task<(PackageManifest Manifest, string ModelDigest, string Folder)> Produce(StageExportRequest request)
    {
        Validate(request);
        var issues = new List<StageUnsupportedIssue>();
        var notes = new List<SemanticModelLoadDiagnostic>();
        var (loaded, scene) = await SemanticHost.LoadModel(request.ModelPath, issues, notes);
        if (loaded is null || scene is null || issues.Count > 0)
        {
            throw new StageExportRefused([.. issues.Select(issue => $"{issue.Capability} {issue.Artifact}: {issue.Details}").DefaultIfEmpty("The model could not be loaded.")]);
        }

        var admission = new SemanticRuntimeAdmission(loaded.Plan, id => loaded.ImplementationRequirements.FirstOrDefault(requirement => requirement.RequirementId == id)?.Member);
        if (admission.Blocking.Count > 0)
        {
            throw new StageExportRefused([.. admission.Blocking.Select(entry => $"Admission refused {entry.Kind} {entry.Artifact}: {entry.Details}")]);
        }

        var authentication = AuthenticationRequirementsReader.From(loaded.Model);
        if (!authentication.Required)
        {
            throw new StageExportRefused(["The model has no authentication policy: no command or query requires a signed-in caller, and published applications must require sign-in."]);
        }

        var references = ModelReferenceMap.Build(ModelReferenceMap.Keys(loaded.Model, scene), request.ModelReferences);
        var output = Path.GetFullPath(request.OutputPath);
        var model = Path.Combine(output, PackageManifest.ModelFolder);
        Directory.CreateDirectory(model);
        CopyModel(request.ModelPath, model);

        var digest = PackageDigest.Of(model);
        var runtime = request.RuntimeVersion ?? StageRuntimeVersion.Current;
        var manifest = new PackageManifest(
            PackageManifest.CurrentSchemaVersion,
            loaded.Model.Application.Name,
            request.PublishedName,
            request.ApplicationVersion,
            runtime,
            request.PublicationId ?? PublicationIdFor(request, digest),
            digest,
            request.NotesEnabled,
            authentication,
            references);

        await File.WriteAllTextAsync(Path.Combine(output, PackageManifest.FileName), PackageManifestJson.Write(manifest));
        await File.WriteAllTextAsync(Path.Combine(output, "Dockerfile"), PackageDockerfile.Render(runtime));

        return (manifest, digest, output);
    }

    static void Validate(StageExportRequest request)
    {
        var problems = new List<string>();
        if (!SemanticVersion.IsMatch(request.ApplicationVersion))
        {
            problems.Add($"Application version '{request.ApplicationVersion}' is not a semantic version (for example 1.2.0).");
        }

        if (!PublishedNameLabel.IsMatch(request.PublishedName))
        {
            problems.Add($"Published name '{request.PublishedName}' must be a lowercase host label: letters, digits and hyphens, not starting or ending with a hyphen.");
        }

        if (request.RuntimeVersion is not null && !SemanticVersion.IsMatch(request.RuntimeVersion))
        {
            problems.Add($"Runtime version '{request.RuntimeVersion}' is not a semantic version.");
        }

        if (Directory.Exists(request.OutputPath) && Directory.EnumerateFileSystemEntries(request.OutputPath).Any())
        {
            problems.Add($"The output folder '{request.OutputPath}' is not empty.");
        }

        if (problems.Count > 0)
        {
            throw new StageExportRefused(problems);
        }
    }

    static void CopyModel(string source, string target)
    {
        if (File.Exists(source))
        {
            File.Copy(source, Path.Combine(target, Path.GetFileName(source)));
            return;
        }

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            if (relative.Split(Path.DirectorySeparatorChar).Any(segment => segment.StartsWith('.')))
            {
                continue;
            }

            var destination = Path.Combine(target, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination);
        }
    }

    static string PublicationIdFor(StageExportRequest request, string digest) =>
        "pub-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{request.PublishedName}\n{request.ApplicationVersion}\n{digest}"))).ToLowerInvariant()[..32];
}
