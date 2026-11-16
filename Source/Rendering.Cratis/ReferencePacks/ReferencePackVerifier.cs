// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.Build.Framework;

namespace Cratis.Stage.Rendering.Cratis.ReferencePacks;

/// <summary>
/// Verifies the exact DLL set before MSBuild embeds a pinned reference pack.
/// </summary>
public class ReferencePackVerifier : Microsoft.Build.Utilities.Task
{
    static readonly string[] _manifestSeparator = ["  "];

    /// <summary>
    /// Gets or sets the directory containing the reference assemblies.
    /// </summary>
    [Required]
    public string PackDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the checked-in SHA-256 manifest path.
    /// </summary>
    [Required]
    public string ManifestPath { get; set; } = string.Empty;

    /// <summary>
    /// Compares every embedded DLL with the manifest, rejecting missing and extra assemblies.
    /// </summary>
    /// <param name="packDirectory">The extracted reference assembly directory.</param>
    /// <param name="manifestPath">The checked-in manifest to compare against.</param>
    /// <returns>Every mismatch, naming its file.</returns>
    public static IEnumerable<string> Verify(string packDirectory, string manifestPath)
    {
        var expected = ReadManifest(manifestPath);
        foreach (var entry in expected)
        {
            var error = VerifyFile(Path.Combine(packDirectory, entry.Key), entry.Value);
            if (error.Length > 0)
            {
                yield return error;
            }
        }

        // A missing directory is already reported for each expected file above.
        if (Directory.Exists(packDirectory))
        {
            foreach (var file in Directory.EnumerateFiles(packDirectory, "*.dll").Where(file => !expected.ContainsKey(Path.GetFileName(file))))
            {
                yield return $"Unexpected reference assembly '{file}'.";
            }
        }
    }

    /// <inheritdoc/>
    public override bool Execute()
    {
        foreach (var error in Verify(PackDirectory, ManifestPath))
        {
            Log.LogError($"STAGE-REFERENCE-PACK: {error}");
        }

        return !Log.HasLoggedErrors;
    }

    static Dictionary<string, string> ReadManifest(string manifestPath)
    {
        var entries = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in File.ReadAllLines(manifestPath))
        {
            var parts = line.Split(_manifestSeparator, StringSplitOptions.None);
            if (parts.Length != 2 || !IsValidEntry(parts[0], parts[1]))
            {
                throw new InvalidDataException($"Invalid reference pack manifest '{manifestPath}': '{line}'.");
            }

            entries.Add(parts[1], parts[0]);
        }

        if (entries.Count == 0)
        {
            throw new InvalidDataException($"Empty reference pack manifest '{manifestPath}'.");
        }

        return entries;
    }

    static bool IsValidEntry(string hash, string name) =>
        hash.Length == 64 && hash.All("0123456789abcdef".Contains) &&
        name.EndsWith(".dll", StringComparison.Ordinal) && name == Path.GetFileName(name) &&
        !name.Contains('\\') && !name.Contains('/');

    static string VerifyFile(string path, string expectedHash)
    {
        if (!File.Exists(path))
        {
            return $"Missing reference assembly '{path}'.";
        }

        using var stream = File.OpenRead(path);
        using var algorithm = SHA256.Create();
        var actualHash = string.Concat(algorithm.ComputeHash(stream).Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));

        return actualHash == expectedHash ? string.Empty : $"SHA-256 mismatch for reference assembly '{path}': expected {expectedHash}, found {actualHash}.";
    }
}
