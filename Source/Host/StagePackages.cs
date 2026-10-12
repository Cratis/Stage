// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Scene.Engine.Packages;
using Cratis.Stage.Contracts.Scene;

namespace Cratis.Stage.Host;

/// <summary>
/// The custom runtime packages a live Stage was handed: the approved package set document and the folder holding
/// the browser (ESM) build of each package in it.
/// </summary>
/// <remarks>
/// <para>
/// The folder mirrors an npm layout - <c language="shell">approved-packages.json</c> at its root and each package in a folder named
/// by its npm name (<c language="shell">@acme/scene.charts/</c>) - so the same approved package set that makes <c language="shell">cratis render</c> install a
/// package makes a running Stage serve it. The Stage serves only the files inside the folders of the packages the set
/// names, never the rest of the folder, and only the entry modules the set's declaration points at are ever imported.
/// </para>
/// <para>
/// A set that is malformed, that reuses the name of a shipped package, or whose package cannot be found does not stop the
/// Stage: it is reported in <see cref="Diagnostics"/>, which the frontend shows instead of rendering a profile with a
/// package silently missing.
/// </para>
/// </remarks>
internal sealed class StagePackages
{
    /// <summary>
    /// The name of the approved package set document at the root of the packages folder.
    /// </summary>
    public const string SetFileName = "approved-packages.json";

    /// <summary>
    /// The route the package document is served on.
    /// </summary>
    public const string DocumentRoute = "/stage/packages";

    /// <summary>
    /// The route the files of the approved packages are served under.
    /// </summary>
    public const string FilesRoute = "/stage/packages/files";

    static readonly HashSet<string> _servedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".js", ".mjs", ".css", ".map", ".json", ".svg", ".png", ".jpg", ".jpeg", ".gif", ".webp", ".avif", ".ico", ".woff", ".woff2", ".ttf", ".otf"
    };

    StagePackages(string root, ApprovedPackageSet set, IReadOnlyDictionary<string, string> modules, IReadOnlyList<string> styles, IReadOnlyList<string> diagnostics)
    {
        Root = root;
        Set = set;
        Modules = modules;
        Styles = styles;
        Diagnostics = diagnostics;
    }

    /// <summary>
    /// Gets the absolute path of the packages folder.
    /// </summary>
    public string Root { get; }

    /// <summary>
    /// Gets the approved package set.
    /// </summary>
    public ApprovedPackageSet Set { get; }

    /// <summary>
    /// Gets the page-relative URL of each package's entry module, by npm name.
    /// </summary>
    public IReadOnlyDictionary<string, string> Modules { get; }

    /// <summary>
    /// Gets the page-relative URL of every style sheet the approved packages bring.
    /// </summary>
    public IReadOnlyList<string> Styles { get; }

    /// <summary>
    /// Gets every reason the packages cannot be used. Empty when the set was read and every package was found.
    /// </summary>
    public IReadOnlyList<string> Diagnostics { get; }

    /// <summary>
    /// Reads the packages of a folder.
    /// </summary>
    /// <param name="directory">The folder, or <see langword="null"/> when none is configured.</param>
    /// <returns>The packages, or <see langword="null"/> when there is no folder or it holds no approved package set.</returns>
    public static StagePackages? Read(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return null;
        }

        var root = Path.GetFullPath(directory);
        var setPath = Path.Combine(root, SetFileName);
        if (!File.Exists(setPath))
        {
            return null;
        }

        var read = ApprovedPackageSetJson.Read(File.ReadAllText(setPath), StageShippedPackages.Names);
        if (read.Set is null || !read.Succeeded)
        {
            return new(root, ApprovedPackageSet.Empty, new Dictionary<string, string>(), [], [.. read.Diagnostics.Select(_ => $"{SetFileName}: {_}")]);
        }

        var diagnostics = new List<string>();
        var modules = new Dictionary<string, string>(StringComparer.Ordinal);
        var styles = new List<string>();
        foreach (var package in read.Set.Packages)
        {
            var npm = package.Module.Npm;
            var packageRoot = Path.GetFullPath(Path.Combine(root, npm));
            if (!IsInside(root, packageRoot) || !Directory.Exists(packageRoot))
            {
                diagnostics.Add($"Package '{package.Manifest.Name}' is imported from '{npm}', but the packages folder has no '{npm}' folder.");
                continue;
            }

            var entry = ResolveEntry(packageRoot);
            if (entry is null)
            {
                diagnostics.Add($"Package '{package.Manifest.Name}': '{npm}' has no browser module (an .js or .mjs entry named by its package.json exports, module or main, or index.js).");
                continue;
            }

            modules[npm] = $"{FilesRoute[1..]}/{npm}/{entry}";
            foreach (var style in package.Module.Styles)
            {
                var stylePath = Path.GetFullPath(Path.Combine(root, style));
                if (!IsInside(packageRoot, stylePath) || !File.Exists(stylePath) || !string.Equals(Path.GetExtension(stylePath), ".css", StringComparison.OrdinalIgnoreCase))
                {
                    diagnostics.Add($"Package '{package.Manifest.Name}': style sheet '{style}' is not a .css file inside the '{npm}' folder.");
                    continue;
                }

                styles.Add($"{FilesRoute[1..]}/{style}");
            }
        }

        return new(root, read.Set, modules, [.. styles.Distinct(StringComparer.Ordinal)], diagnostics);
    }

    /// <summary>
    /// Determines whether a request path under <see cref="FilesRoute"/> names a file of an approved package that Stage may serve.
    /// </summary>
    /// <param name="relativePath">The path after the files route, without the leading slash.</param>
    /// <returns><see langword="true"/> when it is inside an approved package's folder and has a servable extension.</returns>
    public bool CanServe(string relativePath)
    {
        if (relativePath.Contains("..", StringComparison.Ordinal) || relativePath.Contains('\\', StringComparison.Ordinal))
        {
            return false;
        }

        var extension = Path.GetExtension(relativePath);
        return _servedExtensions.Contains(extension) &&
            Set.Packages.Any(package => relativePath.StartsWith($"{package.Module.Npm}/", StringComparison.Ordinal));
    }

    /// <summary>
    /// Writes the document the frontend reads: the set, where each module is served from, the style sheets and the diagnostics.
    /// </summary>
    /// <returns>The document.</returns>
    public object ToDocument() => new
    {
        set = JsonDocument.Parse(ApprovedPackageSetJson.Write(Set)).RootElement.Clone(),
        modules = Modules,
        styles = Styles,
        diagnostics = Diagnostics
    };

    static bool IsInside(string root, string path) =>
        path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.Ordinal);

    static string? ResolveEntry(string packageRoot)
    {
        string? declared = null;
        var manifest = Path.Combine(packageRoot, "package.json");
        if (File.Exists(manifest))
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(manifest));
                declared = DeclaredEntry(document.RootElement);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        var candidate = Normalize(declared ?? "index.js");
        return candidate is not null && (string.Equals(Path.GetExtension(candidate), ".js", StringComparison.Ordinal) || string.Equals(Path.GetExtension(candidate), ".mjs", StringComparison.Ordinal)) && File.Exists(Path.Combine(packageRoot, candidate))
            ? candidate
            : null;
    }

    static string? DeclaredEntry(JsonElement manifest)
    {
        if (manifest.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (manifest.TryGetProperty("exports", out var exports))
        {
            var root = exports.ValueKind == JsonValueKind.Object && exports.TryGetProperty(".", out var dot) ? dot : exports;
            if (ConditionalTarget(root) is { } exported)
            {
                return exported;
            }
        }

        foreach (var field in new[] { "module", "main" })
        {
            if (manifest.TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }
        }

        return null;
    }

    static string? ConditionalTarget(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            return element.GetString();
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var condition in new[] { "import", "browser", "default" })
        {
            if (element.TryGetProperty(condition, out var target) && ConditionalTarget(target) is { } resolved)
            {
                return resolved;
            }
        }

        return null;
    }

    static string? Normalize(string path)
    {
        var normalized = path.Replace('\\', '/');
        while (normalized.StartsWith("./", StringComparison.Ordinal))
        {
            normalized = normalized[2..];
        }

        return normalized.Length == 0 || normalized.StartsWith('/') || normalized.Split('/').Any(_ => string.Equals(_, "..", StringComparison.Ordinal) || string.Equals(_, ".", StringComparison.Ordinal))
            ? null
            : normalized;
    }
}
