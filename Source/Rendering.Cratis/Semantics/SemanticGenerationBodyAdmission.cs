// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Compile-checks a generation body against BCL and the shipped Fundamentals context, without executing it.
/// Generation deliberately permits entropy and clock reads; it does not use the pure-body operation walk.
/// </summary>
internal static class SemanticGenerationBodyAdmission
{
    const string FundamentalsResource = "GenerationReferences.Cratis.Fundamentals.dll";
    const string ChronicleResource = "GenerationReferences.Cratis.Chronicle.dll";
    static readonly Lazy<PortableExecutableReference> _fundamentals = new(() => LoadEmbedded(FundamentalsResource, "Fundamentals generation"));
    static readonly Lazy<PortableExecutableReference> _chronicle = new(() => LoadEmbedded(ChronicleResource, "Chronicle"));

    internal static PortableExecutableReference FundamentalsReference => _fundamentals.Value;

    internal static PortableExecutableReference ChronicleReference => _chronicle.Value;

    internal static PureTransitionAdmission.Verdict Analyze(string body, SemanticConcept concept, SemanticApplicationContext context, SemanticImplementationRequirement requirement)
    {
        var parsed = PureTransitionAdmission.ParseBody(body, new HashSet<string>(StringComparer.Ordinal), "generation", enforcePurity: false);
        if (parsed.Rejection is not null) return parsed.Rejection;

        var method = SemanticGenerationRendering.BodyMethod(concept);
        var signature = $"static string {method}(global::Cratis.Concepts.GenerationContext context)";

        // Match the scaffold's SDK implicit usings and RenderBody's explicit Cratis.Concepts import.
        var prefix = "using System; using System.Collections.Generic; using System.IO; using System.Linq; " +
            "using System.Net.Http; using System.Threading; using System.Threading.Tasks; using Cratis.Concepts;\n" +
            $"static class {Identifiers.ToPascalCase(concept.Name)} {{ {signature} {{\n";
        var code = prefix + body + "\n} }";
        var tree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest));
        return WithAnalysisReferences(metadataReferences =>
        {
            var compilation = CSharpCompilation.Create(
                "GenerationBodyAnalysis",
                [tree],
                metadataReferences,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
            var model = compilation.GetSemanticModel(tree);
            var references = PureTransitionAdmission.MethodTypeReferences(tree, model, method);
            var peers = context.Application.Concepts.Select(concept => Identifiers.ToPascalCase(concept.Name))
                .Concat(context.Application.Types.Select(type => Identifiers.ToPascalCase(type.Name)));
            var shadow = peers.FirstOrDefault(name => references.Any(reference => reference.Name == name));
            if (shadow is not null) return Reject("STAGE-ESM-022", $"Generated type '{shadow}' shadows a type referenced by the generation body.");
            var namespaceShadow = context.NamespacePaths.GroupBy(path => path.Split('.')[^1], StringComparer.Ordinal).FirstOrDefault(group =>
                references.Any(reference => reference.Name == group.Key && (reference.Namespace is null || group.Any(path => path != reference.Namespace))))?.Key;
            if (namespaceShadow is not null) return Reject("STAGE-ESM-022", $"Generated namespace '{namespaceShadow}' shadows a type referenced by the generation body.");

            // The generated application treats warnings as errors, so a body that only warns would not build there.
            var diagnostics = compilation.GetDiagnostics();
            var error = diagnostics.FirstOrDefault(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error) ??
                diagnostics.FirstOrDefault(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning && !diagnostic.IsSuppressed);
            if (error is not null)
            {
                var detail = $"{error.Id}: {error.GetMessage(CultureInfo.InvariantCulture)}";
                if (!error.Location.IsInSource || error.Location.SourceTree != tree)
                {
                    return Reject("STAGE-ESM-021", $"Internal generation analysis compilation error: {detail}");
                }

                var offset = Math.Clamp(error.Location.SourceSpan.Start - prefix.Length, 0, Math.Max(0, body.Length - 1));
                return Reject("STAGE-ESM-019", $"Generation body {PureTransitionAdmission.SourceLocation(requirement, body, offset)}: {detail}");
            }

            return new(null, null, [], []);
        });
    }

    static PureTransitionAdmission.Verdict WithAnalysisReferences(Func<MetadataReference[], PureTransitionAdmission.Verdict> analyze)
    {
        MetadataReference[] references;
        try
        {
            references = [.. PureTransitionAdmission.AnalysisReferences(), FundamentalsReference];
        }
        catch (InvalidOperationException exception)
        {
            return Reject("STAGE-ESM-021", exception.Message);
        }

        return analyze(references);
    }

    static PortableExecutableReference LoadEmbedded(string resource, string description)
    {
        using var stream = typeof(SemanticGenerationBodyAdmission).Assembly.GetManifestResourceStream(resource)
            ?? throw new Contracts.Rendering.InvalidArtifactRenderContract($"The embedded {description} reference is unavailable.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);

        return MetadataReference.CreateFromImage(bytes.ToArray(), filePath: resource);
    }

    static PureTransitionAdmission.Verdict Reject(string code, string reason) => new(code, reason, [], []);
}
