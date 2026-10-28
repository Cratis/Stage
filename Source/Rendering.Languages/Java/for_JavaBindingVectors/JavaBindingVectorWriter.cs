// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.given;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaBindingVectors;

internal static class JavaBindingVectorWriter
{
    internal static void Write(string root, IEnumerable<JavaBindingCase> positive, JavaBindingCase negative)
    {
        if (Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any()) throw new InvalidOperationException("Vector root must be empty: " + root);
        WriteSet(Path.Combine(root, "positive"), positive, negative: false);
        WriteSet(Path.Combine(root, "negative"), [negative], negative: true);
    }

    static void WriteSet(string root, IEnumerable<JavaBindingCase> cases, bool negative)
    {
        var counts = cases.OrderBy(item => item.Name, StringComparer.Ordinal).Select(item => WriteCase(root, item, negative)).ToArray();
        if (counts.Length == 0) throw new InvalidOperationException("No binding cases");
        WriteText(Path.Combine(root, "index.tsv"), $"# java-binding-vectors v1 cases={counts.Length} files={counts.Sum(item => item.Files)} sites={counts.Sum(item => item.Sites)}\n" +
            string.Concat(counts.Select(item => $"{item.Name}\t{item.Files}\t{item.Sites}\n")));
    }

    static (string Name, int Files, int Sites) WriteCase(string root, JavaBindingCase item, bool negative)
    {
        var folder = Child(root, item.Name);
        var (sources, sites) = ResolvePlan(item);
        foreach (var source in item.HostSources.Concat(JavaFactStubs.Generate(negative ? a_java_pack.FactsJson : item.FactsJson))) sources.Add(source.Path, source.Text);
        if (sources.Count == 0 || sites.Count == 0) throw new InvalidOperationException("Empty binding case: " + item.Name);
        foreach (var source in sources) WriteText(Child(Path.Combine(folder, "src"), source.Key), source.Value);
        WriteSites(folder, sites, negative);
        return (item.Name, sources.Count, sites.Count);
    }

    static (SortedDictionary<string, string> Sources, List<(string Path, JavaReferenceSite Site)> Sites) ResolvePlan(JavaBindingCase item)
    {
        var sources = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var sites = new List<(string Path, JavaReferenceSite Site)>();
        var resolved = new JavaLanguagePack(JavaTypeFacts.Parse(item.FactsJson)).ResolveWithSites(item.Files);
        for (var i = 0; i < item.Files.Length; i++)
        {
            var path = item.Files[i].Path;
            if (item.ExcludedPaths.Contains(path)) continue;
            if (resolved[i].Resolution.Refusal is not null) throw new InvalidOperationException($"Refused {item.Name}/{path}: {resolved[i].Resolution.Refusal}");
            if (!path.EndsWith(".java", StringComparison.Ordinal)) continue;
            sources.Add(path, Text(resolved[i].Resolution));
            sites.AddRange(resolved[i].Sites.Select(site => (path, site)));
        }

        return (sources, sites);
    }

    static void WriteSites(string folder, List<(string Path, JavaReferenceSite Site)> sites, bool negative)
    {
        var ordered = sites.OrderBy(site => site.Path, StringComparer.Ordinal).ThenBy(site => site.Site.Start).ToArray();
        WriteText(Path.Combine(folder, "sites.tsv"), string.Concat(ordered.Select(site => $"{site.Path}\t{site.Site.Start}\t{site.Site.End}\t{site.Site.Intended}\n")));
        if (negative)
        {
            var planted = ordered.Single(site => site.Path == "A.java" && site.Site.Intended == "java.lang.String");
            WriteText(Path.Combine(folder, "expected.tsv"), $"{planted.Path}\t{planted.Site.Start}\t{planted.Site.End}\t{planted.Site.Intended}\tlib.Base.String\n");
        }
    }

    static string Text(DeclarativeFileResolution resolution) => string.Concat(resolution.Pieces.Select(piece => piece switch
    {
        DeclarativeGeneratedPiece generated => generated.Text,
        DeclarativeAuthoredPiece authored => authored.Text,
        _ => throw new InvalidOperationException("Unknown resolution piece")
    }));

    static string Child(string root, string path)
    {
        var invalidSegments = path.Split('/').Any(InvalidSegment);
        var invalidPath = Path.IsPathRooted(path) || invalidSegments || path.IndexOfAny(['\\', '\t', '\n']) >= 0;
        if (invalidPath) throw new InvalidOperationException("Unsafe vector path: " + path);
        return Path.Combine(root, path);
    }

    static bool InvalidSegment(string part) => part.Length == 0 || part.Equals(".", StringComparison.Ordinal) || part.Equals("..", StringComparison.Ordinal);

    static void WriteText(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
