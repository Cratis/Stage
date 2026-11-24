// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Verifies and compile-checks provided bodies without executing authored code.</summary>
internal static class SemanticProvidedBodyAdmission
{
    internal static bool TryBody(
        SemanticApplicationContext context,
        SemanticProvidedDeclaration declaration,
        ImmutableArray<ArtifactRenderDiagnostic>.Builder diagnostics,
        out string? body)
    {
        body = null;
        var value = declaration.Value;
        var source = (SemanticCodeProvideSource)value.Source;
        var id = source.Implementation;
        var requirement = context.Request.ImplementationRequirements.FirstOrDefault(item => item.RequirementId == id);
        if (requirement is not { Role: SemanticImplementationRole.ProvideValue, ContextVersion: 1u, ResultVersion: 1u } ||
            requirement.Source?.SemanticId != declaration.Owner || requirement.RequiredCapability != "provider-defined")
        {
            diagnostics.Add(Error(value, context, $"Implementation requirement '{id}' is not a provided-value body."));
            return false;
        }
        if (SemanticImplementationBodySelector.EffectiveLanguage(requirement) is not ("csharp" or "c#"))
        {
            diagnostics.Add(Error(value, context, $"Implementation requirement '{id}' is not C#."));
            return false;
        }
        if (!SemanticImplementationAdmission.TryGetVerifiedBody(context.Request, id, declaration.Owner, diagnostics, out body)) return false;

        var parsed = PureTransitionAdmission.ParseBody(body!, new HashSet<string>(StringComparer.Ordinal), "provided value", enforcePurity: false);
        var reason = parsed.Rejection?.Reason ?? Analyze(context, value, body!);
        if (reason is null) return true;
        diagnostics.Add(Error(value, context, $"Implementation requirement '{id}' at {PureTransitionAdmission.SourceLocation(requirement, body!, 0)}: {reason}"));
        body = null;
        return false;
    }

    static string? Analyze(SemanticApplicationContext context, SemanticProvidedValue value, string body)
    {
        var builder = context.CodeBuilder().Namespace($"{context.RootNamespace}.GeneratedProvides").Using($"global::{context.CommonNamespace}");
        SemanticProvidedBodyRendering.Render(builder, value, context, body);
        var types = new SemanticTypeSystem(context);
        var models = context.ReadModels.Values.Select(model =>
            $"namespace {SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(model.Id).Path)} {{ using global::{context.CommonNamespace}; " +
            SemanticStateViewArtifactRenderer.ModelDeclaration(model, types, [], false) + "; }");
        var common = $"namespace {context.CommonNamespace} {{ {string.Join('\n', SemanticBodyDeclarations.CompositeTypes(context))} }}";
        try
        {
            var compilation = SemanticBodyCompilation.Create(
                "ProvidedBodyAnalysis",
                new[] { SemanticBodyCompilation.ImplicitUsings, builder.ToString(), common }
                    .Concat(models).Concat(SemanticBodyDeclarations.RealConcepts(context))
                    .Select(source => CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest))),
                [.. PureTransitionAdmission.AnalysisReferences(context.TargetFramework), SemanticGenerationBodyAdmission.FundamentalsReference, SemanticProvidedContextReferences.ForTargetFramework(context.TargetFramework)],
                "CS1701",
                "CS1702");
            var error = compilation.GetDiagnostics().FirstOrDefault(SemanticBodyCompilation.IsFailure);
            return error is null ? null : $"{error.Id}: {error.GetMessage(CultureInfo.InvariantCulture)}";
        }
        catch (InvalidOperationException exception)
        {
            return exception.Message;
        }
    }

    static ArtifactRenderDiagnostic Error(SemanticProvidedValue value, SemanticApplicationContext context, string reason) =>
        new("STAGE-ESM-075", ArtifactRenderDiagnosticSeverity.Error, $"Provided value '{value.Name}': {reason}", context.Application.Id);
}
