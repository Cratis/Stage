// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Publishing;

namespace Cratis.Stage.Host.Published;

/// <summary>
/// An exported application package a published host starts from.
/// </summary>
/// <param name="Root">The package folder.</param>
/// <param name="Manifest">The manifest.</param>
internal sealed record PublishedPackage(string Root, PackageManifest Manifest)
{
    /// <summary>
    /// Gets the folder holding the model the host loads at startup.
    /// </summary>
    internal string ModelPath => Path.Combine(Root, PackageManifest.ModelFolder);

    /// <summary>
    /// Reads and verifies a package: manifest present and understood, model intact, runtime version matching.
    /// </summary>
    /// <param name="root">The package folder, or a <c language="shell">.tar.gz</c> archive of one, which is extracted to a temporary folder first.</param>
    /// <returns>The verified package.</returns>
    /// <exception cref="PublishedModeRefused">The package is missing, damaged, or for another runtime.</exception>
    internal static PublishedPackage Read(string root)
    {
        if (IsArchive(root))
        {
            root = Extract(root);
        }

        var manifestPath = Path.Combine(root, PackageManifest.FileName);
        if (!File.Exists(manifestPath))
        {
            throw new PublishedModeRefused([$"No application package found at '{root}'. Expected {PackageManifest.FileName} and a {PackageManifest.ModelFolder} folder; set {PublishedOptions.Section}:PackagePath."]);
        }

        PackageManifest manifest;
        try
        {
            manifest = PackageManifestJson.Read(File.ReadAllText(manifestPath));
        }
        catch (System.Text.Json.JsonException exception)
        {
            throw new PublishedModeRefused([$"The package manifest is not valid: {exception.Message}"]);
        }

        var problems = new List<string>();
        if (manifest.SchemaVersion != PackageManifest.CurrentSchemaVersion)
        {
            problems.Add($"The package manifest has schema version {manifest.SchemaVersion}; this runtime reads version {PackageManifest.CurrentSchemaVersion}.");
        }

        if (!string.Equals(manifest.StageRuntimeVersion, StageRuntimeVersion.Current, StringComparison.Ordinal))
        {
            problems.Add($"The package requires Stage runtime {manifest.StageRuntimeVersion} but this is {StageRuntimeVersion.Current}.");
        }

        var model = Path.Combine(root, PackageManifest.ModelFolder);
        if (!Directory.Exists(model))
        {
            problems.Add($"The package has no {PackageManifest.ModelFolder} folder.");
        }
        else if (!string.Equals(PackageDigest.Of(model), manifest.ModelDigest, StringComparison.Ordinal))
        {
            problems.Add("The model in the package does not match the digest in its manifest.");
        }

        if (!manifest.Authentication.Required)
        {
            problems.Add("The package declares no authentication requirement; published applications must require sign-in.");
        }

        if (problems.Count > 0)
        {
            throw new PublishedModeRefused(problems);
        }

        return new PublishedPackage(root, manifest);
    }

    static bool IsArchive(string path) => File.Exists(path) &&
        (path.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase));

    static string Extract(string archive)
    {
        var folder = Directory.CreateTempSubdirectory("stage-package-").FullName;
        AppDomain.CurrentDomain.ProcessExit += (_, _) => TryDelete(folder);
        try
        {
            PackageArchive.Extract(archive, folder);
        }
        catch (InvalidDataException exception)
        {
            TryDelete(folder);
            throw new PublishedModeRefused([$"The package archive '{archive}' is not usable: {exception.Message}"]);
        }

        return folder;
    }

    static void TryDelete(string folder)
    {
        try
        {
            Directory.Delete(folder, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
