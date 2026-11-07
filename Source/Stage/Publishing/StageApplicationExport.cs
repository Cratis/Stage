// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Publishing;

/// <summary>
/// Produces a versioned, publishable application package from in-memory Screenplay sources.
/// </summary>
/// <remarks>
/// <para>
/// The model is verified here to compile and to pass the semantic admission the Stage applies when it loads a model, and it
/// must require sign-in. The package holds <c language="shell">manifest.json</c>, the model files under <c language="shell">model/</c> and a
/// <c language="shell">Dockerfile</c> that bakes the package into an image built <c language="shell">FROM cratis/stage:&lt;runtime version&gt;</c>.
/// The same request always gives the same bytes.
/// </para>
/// </remarks>
public static partial class StageApplicationExport
{
    [GeneratedRegex(@"^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex SemanticVersion { get; }

    [GeneratedRegex(@"^[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?$", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex PublishedNameLabel { get; }

    /// <summary>
    /// Exports the package.
    /// </summary>
    /// <param name="request">What to export.</param>
    /// <returns>The package.</returns>
    /// <exception cref="StageExportRefused">The request is invalid, the model fails admission or has no authentication policy.</exception>
    public static StageApplicationPackage Export(StageApplicationExportRequest request) =>
        Task.Run(() => ExportAsync(request)).GetAwaiter().GetResult();

    /// <summary>
    /// Exports the package.
    /// </summary>
    /// <param name="request">What to export.</param>
    /// <returns>The package.</returns>
    /// <exception cref="StageExportRefused">The request is invalid, the model fails admission or has no authentication policy.</exception>
    public static async Task<StageApplicationPackage> ExportAsync(StageApplicationExportRequest request)
    {
        var files = Validate(request);
        var scratch = Directory.CreateTempSubdirectory("stage-export-").FullName;
        try
        {
            var source = Path.Combine(scratch, PackageManifest.ModelFolder);
            foreach (var file in files)
            {
                var destination = Path.Combine(source, file.Key);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                await File.WriteAllBytesAsync(destination, file.Value);
            }

            return await Produce(request, files, source);
        }
        finally
        {
            Directory.Delete(scratch, recursive: true);
        }
    }

    static async Task<StageApplicationPackage> Produce(StageApplicationExportRequest request, SortedDictionary<string, byte[]> files, string source)
    {
        var issues = new List<StageLoadIssue>();
        var (loaded, scene) = await StageModelLoader.Load(source, issues, null, request.ApplicationName);
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
        var digest = PackageDigest.Of(files);
        var runtime = request.RuntimeVersion ?? StageRuntimeVersion.Current;
        var manifest = new PackageManifest(
            PackageManifest.CurrentSchemaVersion,
            request.ApplicationName,
            request.PublishedName,
            request.ApplicationVersion,
            runtime,
            request.PublicationId ?? PublicationIdFor(request, digest),
            digest,
            request.NotesEnabled,
            authentication,
            references);

        var manifestJson = PackageManifestJson.Write(manifest);
        var all = new SortedDictionary<string, byte[]>(StringComparer.Ordinal)
        {
            [PackageManifest.FileName] = Utf8(manifestJson),
            ["Dockerfile"] = Utf8(PackageDockerfile.Render(runtime))
        };
        foreach (var file in files)
        {
            all[$"{PackageManifest.ModelFolder}/{file.Key}"] = file.Value;
        }

        return new StageApplicationPackage(manifest, manifestJson, new ReadOnlyDictionary<string, byte[]>(all));
    }

    static SortedDictionary<string, byte[]> Validate(StageApplicationExportRequest request)
    {
        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(request.ApplicationName))
        {
            problems.Add("The application name is required.");
        }

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

        var files = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var file in request.ModelFiles)
        {
            var path = file.Key.Replace('\\', '/');
            if (path.Length == 0 || path.StartsWith('/') || path.Contains(':', StringComparison.Ordinal) || path.Split('/').Any(segment => segment.Length == 0 || segment.Equals(".", StringComparison.Ordinal) || segment.Equals("..", StringComparison.Ordinal)))
            {
                problems.Add($"Model file path '{file.Key}' must be a relative path inside the model.");
            }
            else if (!files.TryAdd(path, Utf8(file.Value)))
            {
                problems.Add($"Model file path '{path}' is given more than once.");
            }
        }

        if (files.Count == 0 && problems.Count == 0)
        {
            problems.Add("The model has no files.");
        }

        if (problems.Count > 0)
        {
            throw new StageExportRefused(problems);
        }

        return files;
    }

    static byte[] Utf8(string text) => new UTF8Encoding(false).GetBytes(text);

    static string PublicationIdFor(StageApplicationExportRequest request, string digest) =>
        "pub-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{request.PublishedName}\n{request.ApplicationVersion}\n{digest}"))).ToLowerInvariant()[..32];
}
