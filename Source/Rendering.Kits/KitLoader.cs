// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;

namespace Cratis.Stage.Rendering.Kits;

/// <summary>Loads untrusted kit sources into immutable, fully validated kits.</summary>
public static class KitLoader
{
    /// <summary>The only implicit file in every kit.</summary>
    public const string ManifestPath = "stage-kit.json";

    static readonly UTF8Encoding _strictUtf8 = new(false, true);

    /// <summary>Loads a kit, returning all discoverable rejections in code/path order.</summary>
    /// <param name="source">Kit source.</param>
    /// <param name="capabilities">Exact engine capabilities.</param>
    /// <param name="limits">Optional materialization limits.</param>
    /// <returns>A complete kit or diagnostics.</returns>
    public static KitLoadResult Load(IKitSource source, KitEngineCapabilities capabilities, KitLoadLimits? limits = null)
    {
        var diagnostics = new List<KitDiagnostic>();
        var files = KitSourceReader.Read(source, limits ?? new(), diagnostics);
        if (!files.TryGetValue(ManifestPath, out var manifestBytes))
        {
            diagnostics.Add(new(KitCodes.MissingManifest, "Kit root must contain stage-kit.json.", ManifestPath));
            return Failure(diagnostics);
        }

        var manifest = KitManifestReader.Read(manifestBytes, diagnostics);
        if (manifest is null)
        {
            return Failure(diagnostics);
        }

        KitManifestValidator.Validate(manifest, capabilities, diagnostics);
        ValidateFiles(manifest, files, diagnostics);
        if (diagnostics.Count != 0)
        {
            return Failure(diagnostics);
        }

        var kitFiles = new KitFiles(files);
        var archive = KitArchive.Write(kitFiles);
        return new(new(manifest, kitFiles, archive, KitDigest.Compute(archive)), []);
    }

    static KitLoadResult Failure(List<KitDiagnostic> diagnostics) => new(null, [.. diagnostics.OrderBy(diagnostic => diagnostic.Code, StringComparer.Ordinal).ThenBy(diagnostic => diagnostic.Path, StringComparer.Ordinal).ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)]);

    static void ValidateFiles(KitManifest manifest, Dictionary<string, ImmutableArray<byte>> files, List<KitDiagnostic> diagnostics)
    {
        var declared = KitManifestValidator.DeclaredFiles(manifest).ToHashSet(StringComparer.Ordinal);
        declared.Add(ManifestPath);
        foreach (var path in declared.Where(path => !files.ContainsKey(path) && KitPath.Validate(path) is null))
        {
            diagnostics.Add(new(KitCodes.MissingFile, "Declared file is missing.", path));
        }

        foreach (var path in files.Keys.Where(path => !declared.Contains(path)))
        {
            diagnostics.Add(new(KitCodes.UndeclaredFile, "File is not declared in the manifest.", path));
        }

        var textPaths = manifest.Outputs.Select(output => output.Template).Concat(manifest.Partials.Select(partial => partial.Template)).Concat(manifest.Tables.Select(table => table.Path));
        foreach (var path in textPaths.Distinct(StringComparer.Ordinal).Where(files.ContainsKey))
        {
            ValidateUtf8(path, files[path], diagnostics);
        }
    }

    static void ValidateUtf8(string path, ImmutableArray<byte> bytes, List<KitDiagnostic> diagnostics)
    {
        try
        {
            if (bytes.AsSpan().StartsWith(new byte[] { 0xef, 0xbb, 0xbf }))
            {
                throw new DecoderFallbackException("UTF-8 BOM is not canonical.");
            }

            _strictUtf8.GetCharCount(bytes.AsSpan());
        }
        catch (DecoderFallbackException)
        {
            diagnostics.Add(new(KitCodes.InvalidUtf8, "Templates and tables must be strict UTF-8 without a BOM.", path));
        }
    }
}
