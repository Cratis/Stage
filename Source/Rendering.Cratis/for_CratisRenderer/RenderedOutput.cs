// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

/// <summary>
/// Compiles rendered files in memory against the real Cratis assemblies.
/// </summary>
/// <remarks>
/// Asserting on generated strings only proves the renderer emitted what the spec expected it to emit. It cannot
/// see a missing import, a constructor called with the wrong number of arguments, or an attribute placed where it
/// is not valid — all of which the compiler sees immediately. This is the assertion that closes that gap.
/// </remarks>
internal static class RenderedOutput
{
    static readonly ImmutableArray<MetadataReference> _references =
    [
        .. ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(assembly => MetadataReference.CreateFromFile(assembly))
    ];
    static readonly CSharpParseOptions _parseOptions = new(preprocessorSymbols: ["DEBUG"]);

    /// <summary>
    /// The implicit usings the scaffolded project enables (<c language="csharp">ImplicitUsings</c> in the Cratis template), together
    /// with the global usings the <c language="csharp">Cratis</c> package itself contributes through its <c language="csharp">Cratis.props</c>, mirrored
    /// here so the compilation sees the same ambient namespaces the rendered application really builds with.
    /// </summary>
    /// <remarks>
    /// The package's set is load-bearing in both directions: without it the rendered validators look broken because
    /// <c language="csharp">FluentValidation</c> is missing, and with it a short type name that is unambiguous on its own becomes
    /// ambiguous — <c language="csharp">IIdentityProvider</c> is declared by both <c language="csharp">Cratis.Arc.Identity</c> and
    /// <c language="csharp">Cratis.Chronicle.Identities</c>. A compilation that omits them sees neither.
    /// The resolver decides short names against exactly the catalog's set, so the compilation reads it from there. A
    /// namespace whose assembly the test host does not load (Orleans, for one) cannot hold a type the rendered code names,
    /// so it is left out.
    /// </remarks>
    static readonly string ImplicitUsings = string.Concat(FrameworkNameCatalog.Current.GlobalUsings(GeneratedProjects.Application)
        .Where(Resolves)
        .Select(@namespace => $"global using {@namespace};\n"));

    static readonly CSharpCompilation _template = CSharpCompilation.Create(
        "RenderedApplication",
        [CSharpSyntaxTree.ParseText(ImplicitUsings, _parseOptions, path: "GlobalUsings.g.cs")],
        _references,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable)
            .WithSpecificDiagnosticOptions(
            [
                // The SDK suppresses these by default: a package built against an older framework assembly version
                // (MongoDB.Driver against System.Linq.Expressions 6.0) binds to the one the host provides.
                new KeyValuePair<string, ReportDiagnostic>("CS1701", ReportDiagnostic.Suppress),
                new KeyValuePair<string, ReportDiagnostic>("CS1702", ReportDiagnostic.Suppress)
            ]));

    /// <summary>
    /// Compiles the rendered files and returns every compilation error, each prefixed with the file it came from.
    /// </summary>
    /// <param name="files">The rendered files to compile.</param>
    /// <returns>The compilation errors, empty when the output compiles.</returns>
    public static IReadOnlyList<string> Errors(IEnumerable<RenderedFile> files) => Diagnostics(CreateCompilation(files), DiagnosticSeverity.Error);

    /// <summary>
    /// Compiles rendered files and returns every compiler warning.
    /// </summary>
    /// <param name="files">The rendered files to compile.</param>
    /// <returns>The compilation warnings, empty when the output is warning-free.</returns>
    public static IReadOnlyList<string> Warnings(IEnumerable<RenderedFile> files) => Diagnostics(CreateCompilation(files), DiagnosticSeverity.Warning);

    /// <summary>
    /// Compiles and loads rendered files so specs can exercise the real Arc evaluators against generated members.
    /// </summary>
    /// <param name="files">The rendered files to compile and load.</param>
    /// <param name="clientOnly">Whether to disambiguate client SDK types that overlap the kernel assemblies in the test host.</param>
    /// <returns>The loaded rendered application assembly.</returns>
    /// <exception cref="RenderedOutputDoesNotCompile">The rendered output does not compile.</exception>
    public static Assembly Load(IEnumerable<RenderedFile> files, bool clientOnly = false)
    {
        var compilation = CreateCompilation(files);
        if (clientOnly)
        {
            // The test host also loads the kernel, which declares client SDK types such as IEventSequence in the same
            // namespaces; keeping the kernel out of the global alias leaves only the client SDK visible to the output.
            compilation = compilation.WithReferences(_references.Select(reference => Path.GetFileName(reference.Display) == "Cratis.Chronicle.Core.dll"
                ? reference.WithAliases(["ChronicleKernel"])
                : reference));
        }

        using var assembly = new MemoryStream();
        var result = compilation.Emit(assembly);
        if (!result.Success)
        {
            throw new RenderedOutputDoesNotCompile(Diagnostics(compilation, DiagnosticSeverity.Error));
        }

        assembly.Position = 0;
        return AssemblyLoadContext.Default.LoadFromStream(assembly);
    }

    static bool Resolves(string @namespace)
    {
        var probe = CSharpCompilation.Create("Probe", [CSharpSyntaxTree.ParseText($"using {@namespace};", _parseOptions)], _references, new(OutputKind.DynamicallyLinkedLibrary));
        return !probe.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    internal static CSharpCompilation CreateCompilation(IEnumerable<RenderedFile> files)
    {
        // DEBUG has to be defined or a rendered specification compiles to nothing: the whole file sits inside
        // '#if DEBUG', and a parse without the symbol drops it silently — the assertion would then pass on an
        // empty compilation unit and prove nothing about the spec it was written for.
        var trees = files.Select(file => CSharpSyntaxTree.ParseText(file.Content, _parseOptions, path: file.RelativePath));

        return _template.WithAssemblyName($"RenderedApplication_{Guid.NewGuid():N}").AddSyntaxTrees(trees);
    }

    static IReadOnlyList<string> Diagnostics(CSharpCompilation compilation, DiagnosticSeverity severity) =>
    [
        .. compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == severity)
            .Select(diagnostic => $"{diagnostic.Location.SourceTree?.FilePath}: {diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture)}")
    ];

    sealed class RenderedOutputDoesNotCompile(IReadOnlyList<string> errors) : Exception(
        $"Rendered output does not compile:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
}
