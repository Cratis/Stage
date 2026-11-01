// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.given;

public class a_republished_application_plan : a_profile_source_plan
{
    protected const string GlobalUsingsPath = "GlobalUsings.cs";
    const string AmbientUsingsPath = "GlobalUsings.g.cs";

    protected CratisPublicationCheck _check = null!;
    protected List<string> _readPaths = [];
    protected string[] _errors = [];

    /// <summary>
    /// Gets the existing target content of a planned file, or null when the target lacks it.
    /// </summary>
    /// <param name="path">The application-root-relative path.</param>
    /// <param name="planned">The planned content.</param>
    /// <returns>The content the target holds.</returns>
    protected virtual string? Existing(string path, string planned) => planned;

    void Establish()
    {
        PlanFull();
        var existing = _full.Artifacts
            .Select(artifact => (artifact.RelativePath, Content: Existing(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan()))))
            .Where(entry => entry.Content is not null)
            .ToDictionary(entry => entry.RelativePath, entry => entry.Content);
        _check = CratisRendering.CheckPublication(_full, path =>
        {
            _readPaths.Add(path);
            return existing.GetValueOrDefault(path);
        });

        var files = _full.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));

        // The harness mirrors the scaffold's global namespaces as ambient usings, which would hide a missing GlobalUsings.cs.
        // Remove those from the ambient file, so the planned GlobalUsings.cs is the only source of them.
        var compilation = RenderedOutput.CreateCompilation(files);
        var ambient = compilation.SyntaxTrees.Single(tree => tree.FilePath == AmbientUsingsPath);
        var scaffold = FrameworkNameCatalog.Current.ScaffoldGlobalUsings(GeneratedProjects.Application).ToHashSet(StringComparer.Ordinal);
        var kept = string.Concat(ambient.GetText().ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(line => !scaffold.Contains(line.Replace("global using ", string.Empty, StringComparison.Ordinal).TrimEnd(';')))
            .Select(line => line + "\n"));
        compilation = compilation.ReplaceSyntaxTree(ambient, CSharpSyntaxTree.ParseText(kept, path: AmbientUsingsPath));
        _errors = [.. compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => $"{diagnostic.Location.SourceTree?.FilePath}: {diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture)}")];
    }
}
