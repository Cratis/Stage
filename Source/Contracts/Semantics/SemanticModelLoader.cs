// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Semantics;

/// <summary>
/// The compiled executable model and its capability-admitted plan.
/// </summary>
/// <param name="Model">The executable semantic model.</param>
/// <param name="Plan">The execution plan.</param>
public sealed record LoadedSemanticModel(ExecutableSemanticModel Model, SemanticExecutionPlan Plan)
{
    /// <summary>Requirements emitted by the compilation.</summary>
    public ImmutableArray<SemanticImplementationRequirement> ImplementationRequirements { get; init; } = [];

    /// <summary>Resolved inline and file bodies keyed by requirement identity.</summary>
    public ImmutableDictionary<string, string> ImplementationContents { get; init; } = [];
}

/// <summary>
/// Loads portable Screenplay semantic execution from one file or a folder of source documents.
/// </summary>
public static class SemanticModelLoader
{
    /// <summary>
    /// Compiles a Screenplay source set and optional authoritative identity catalog.
    /// </summary>
    /// <param name="path">A .play file or a folder searched recursively for .play sources.</param>
    /// <param name="catalogPath">An optional canonical Screenplay identity catalog JSON file whose document keys match the UTF-8 hex encoding of relative .play paths.</param>
    /// <remarks>
    /// Without a workspace envelope, the input file or folder name becomes the application name. The caller must
    /// preserve that name, the source paths, and any identity catalog to retain the same semantic identities.
    /// </remarks>
    /// <returns>The executable model and plan.</returns>
    /// <exception cref="InvalidSemanticModel">Input is absent, malformed, or not executable.</exception>
    public static Task<LoadedSemanticModel> LoadFromPathAsync(string path, string? catalogPath = null) => LoadFromPathAsync(path, catalogPath, null);

    /// <summary>
    /// Compiles a source set using an explicit application name when supplied.
    /// </summary>
    /// <param name="path">A .play file or directory.</param>
    /// <param name="catalogPath">An optional authoritative identity catalog.</param>
    /// <param name="applicationName">The application name; defaults to the folder or file name.</param>
    /// <returns>The executable model and plan.</returns>
    /// <exception cref="InvalidSemanticModel">The source cannot compile into an executable plan.</exception>
    public static async Task<LoadedSemanticModel> LoadFromPathAsync(string path, string? catalogPath, string? applicationName)
    {
        var isFolder = Directory.Exists(path);
        if (!isFolder && (!File.Exists(path) || !string.Equals(Path.GetExtension(path), ".play", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidSemanticModel(["Supply an existing .play file or a directory containing .play files."]);
        }

        var files = isFolder ? Directory.GetFiles(path, "*.play", SearchOption.AllDirectories) : [path];
        if (files.Length == 0)
        {
            throw new InvalidSemanticModel(["No .play files were found."]);
        }

        var root = isFolder ? path : Path.GetDirectoryName(Path.GetFullPath(path))!;
        var name = applicationName ?? (isFolder ? new DirectoryInfo(path).Name : Path.GetFileNameWithoutExtension(path));
        var catalog = catalogPath is null
            ? SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(name))
            : SemanticIdentityCatalogSerializer.Deserialize(await File.ReadAllBytesAsync(catalogPath));
        var documents = new List<SemanticSourceDocument>();
        foreach (var file in files.Order(StringComparer.Ordinal))
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');

            // The catalog uses opaque, non-path keys. Encode UTF-8 bytes so even a directory separator cannot
            // be mistaken for a path component while retaining deterministic identity across host platforms.
            var key = Convert.ToHexString(Encoding.UTF8.GetBytes(relative));
            documents.Add(SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, relative, await File.ReadAllTextAsync(file)));
        }

        var sourceDocuments = documents.ToImmutableArray();
        var attachments = AttachmentFiles.Load(root, sourceDocuments);
        var compiled = new SemanticModelCompiler().Compile(name, SemanticDocumentSet.Create(sourceDocuments, catalog, attachments.Contents));
        if (!compiled.Success)
        {
            throw new InvalidSemanticModel([.. compiled.Diagnostics.Select(diagnostic => $"{diagnostic.Location.Path}({diagnostic.Location.Line},{diagnostic.Location.Column}): {diagnostic.Message}")]);
        }

        var model = compiled.Value!.Model;
        var plan = SemanticExecutionPlan.Compile(model);
        if (!plan.Success)
        {
            throw new InvalidSemanticModel([.. plan.Issues.Select(issue => $"{issue.Artifact}: {issue.Details}")]);
        }

        var bodies = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        foreach (var requirement in compiled.ImplementationRequirements)
        {
            if (requirement.File is { } file)
            {
                if (AttachmentFiles.TryNormalize(file, out var key, out _) && attachments.Contents.TryGetValue(key, out var content))
                {
                    bodies[requirement.RequirementId] = content;
                }

                continue;
            }

            var document = sourceDocuments.SingleOrDefault(candidate => candidate.Id == requirement.Source.Span.Document);
            if (document is null || new ScreenplayCompiler().Parse(document.Text, document.DisplayPath).Value is not { } syntax)
            {
                continue;
            }

            var collector = new InlineBodies();
            collector.VisitApplication(syntax);
            var body = collector.Bodies.Where(code => code.Location.Line == requirement.Source.Span.StartLine &&
                code.Location.Column == requirement.Source.Span.StartColumn).ToArray();
            if (body.Length == 1)
            {
                bodies[requirement.RequirementId] = body[0].Code;
            }
        }

        return new(model, plan.Plan!)
        {
            ImplementationRequirements = compiled.ImplementationRequirements,
            ImplementationContents = bodies.ToImmutable()
        };
    }

    sealed class InlineBodies : ScreenplaySyntaxWalker
    {
        public List<CodeBlockSyntax> Bodies { get; } = [];

        public override void VisitCodeBlock(CodeBlockSyntax syntax)
        {
            Bodies.Add(syntax);
            base.VisitCodeBlock(syntax);
        }
    }
}
