// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.given;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaBindingVectors;

internal static class JavaBindingVectorWriter
{
    enum VectorSet
    {
        Positive,
        Negative
    }

    internal static void Write(string root, IEnumerable<JavaBindingCase> positive, JavaBindingCase negative)
    {
        if (Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any()) throw new InvalidOperationException("Vector root must be empty: " + root);
        WriteSet(new DirectoryInfo(Path.Combine(root, "positive")), positive, VectorSet.Positive);
        WriteSet(new DirectoryInfo(Path.Combine(root, "negative")), [negative], VectorSet.Negative);
    }

    static void WriteSet(DirectoryInfo root, IEnumerable<JavaBindingCase> cases, VectorSet set)
    {
        var counts = cases.OrderBy(item => item.Name, StringComparer.Ordinal).Select(item => WriteCase(root, item, set)).ToArray();
        if (counts.Length == 0) throw new InvalidOperationException("No binding cases");
        WriteText(new FileInfo(Path.Combine(root.FullName, "index.tsv")), $"# java-binding-vectors v1 cases={counts.Length} files={counts.Sum(item => item.Files)} sites={counts.Sum(item => item.Sites)}\n" +
            string.Concat(counts.Select(item => $"{item.Name}\t{item.Files}\t{item.Sites}\n")));
    }

    static (string Name, int Files, int Sites) WriteCase(DirectoryInfo root, JavaBindingCase item, VectorSet set)
    {
        var folder = new DirectoryInfo(Child(root, item.Name));
        var (sources, sites) = ResolvePlan(item);
        foreach (var source in item.HostSources.Concat(JavaFactStubs.Generate(set == VectorSet.Negative ? a_java_pack.FactsJson : item.FactsJson))) sources.Add(source.Path, source.Text);
        if (sources.Count == 0 || sites.Count == 0) throw new InvalidOperationException("Empty binding case: " + item.Name);
        foreach (var source in sources) WriteText(new FileInfo(Child(new DirectoryInfo(Path.Combine(folder.FullName, "src")), source.Key)), source.Value);
        WriteSites(folder, sites, set);
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

    static void WriteSites(DirectoryInfo folder, List<(string Path, JavaReferenceSite Site)> sites, VectorSet set)
    {
        var ordered = sites.OrderBy(site => site.Path, StringComparer.Ordinal).ThenBy(site => site.Site.Start).ToArray();
        WriteText(new FileInfo(Path.Combine(folder.FullName, "sites.tsv")), string.Concat(ordered.Select(site => $"{site.Path}\t{site.Site.Start}\t{site.Site.End}\t{site.Site.Intended}\n")));
        if (set == VectorSet.Negative)
        {
            var planted = ordered.Single(site => site.Path == "A.java" && site.Site.Intended == "java.lang.String");
            WriteText(new FileInfo(Path.Combine(folder.FullName, "expected.tsv")), $"{planted.Path}\t{planted.Site.Start}\t{planted.Site.End}\t{planted.Site.Intended}\tlib.Base.String\n");
        }
    }

    static string Text(DeclarativeFileResolution resolution) => string.Concat(resolution.Pieces.Select(piece => piece switch
    {
        DeclarativeGeneratedPiece generated => generated.Text,
        DeclarativeAuthoredPiece authored => authored.Text,
        _ => throw new InvalidOperationException("Unknown resolution piece")
    }));

    static string Child(DirectoryInfo root, string path)
    {
        var invalidSegments = path.Split('/').Any(InvalidSegment);
        var invalidPath = Path.IsPathRooted(path) || invalidSegments || path.IndexOfAny(['\\', '\t', '\n']) >= 0;
        if (invalidPath) throw new InvalidOperationException("Unsafe vector path: " + path);
        return Path.Combine(root.FullName, path);
    }

    static bool InvalidSegment(string part) => part.Length == 0 || part.Equals(".", StringComparison.Ordinal) || part.Equals("..", StringComparison.Ordinal);

    static void WriteText(FileInfo path, string text)
    {
        path.Directory!.Create();
        File.WriteAllText(path.FullName, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
