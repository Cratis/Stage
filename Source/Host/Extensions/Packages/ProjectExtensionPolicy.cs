// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Stage.Host.Extensions.Packages;

/// <summary>
/// What the project says about extension packages, read from <c lang="text">.cratis/extensions.json</c> in the model's folder or the nearest folder above it.
/// </summary>
/// <param name="Allow">What the project allows.</param>
/// <param name="Publishers">The signing keys by publisher identity.</param>
/// <param name="Source">The file it was read from, or empty when there is none.</param>
/// <param name="Error">Why the file could not be read, or empty. An unreadable file allows nothing.</param>
public sealed record ProjectExtensionPolicy(PackageAllowOptions Allow, IReadOnlyDictionary<string, PublisherOptions> Publishers, string Source, string Error)
{
    /// <summary>
    /// The relative path of the file.
    /// </summary>
    public static readonly string RelativePath = Path.Combine(".cratis", "extensions.json");

    static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web) { ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

    /// <summary>
    /// Gets the policy of a project that says nothing.
    /// </summary>
    public static ProjectExtensionPolicy None { get; } = new(new(), new Dictionary<string, PublisherOptions>(), string.Empty, string.Empty);

    /// <summary>
    /// Reads the project's policy.
    /// </summary>
    /// <param name="modelPath">The model's folder or one of its files.</param>
    /// <returns>The <see cref="ProjectExtensionPolicy"/>.</returns>
    public static ProjectExtensionPolicy Read(string modelPath)
    {
        var folder = Directory.Exists(modelPath) ? Path.GetFullPath(modelPath) : Path.GetDirectoryName(Path.GetFullPath(modelPath));
        for (; folder is not null; folder = Path.GetDirectoryName(folder))
        {
            var path = Path.Combine(folder, RelativePath);
            if (File.Exists(path)) return ReadFile(path);
        }

        return None;
    }

    static ProjectExtensionPolicy ReadFile(string path)
    {
        try
        {
            var file = JsonSerializer.Deserialize<ProjectFile>(File.ReadAllText(path), _json) ?? new();
            return new(file.Allow ?? new(), file.Publishers ?? [], path, string.Empty);
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            return new(new(), new Dictionary<string, PublisherOptions>(), path, $"{RelativePath} could not be read: {exception.Message}");
        }
    }

    sealed class ProjectFile
    {
        public PackageAllowOptions? Allow { get; set; }

        public Dictionary<string, PublisherOptions>? Publishers { get; set; }
    }
}
