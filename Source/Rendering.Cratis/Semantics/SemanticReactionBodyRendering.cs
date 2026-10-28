// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static class SemanticReactionBodyRendering
{
    static readonly HashSet<SyntaxKind> _declarations =
    [
        SyntaxKind.VariableDeclarator, SyntaxKind.SingleVariableDesignation, SyntaxKind.ForEachStatement,
        SyntaxKind.CatchDeclaration, SyntaxKind.Parameter, SyntaxKind.LocalFunctionStatement, SyntaxKind.TypeParameter,
        SyntaxKind.FromClause, SyntaxKind.LetClause, SyntaxKind.JoinClause, SyntaxKind.JoinIntoClause, SyntaxKind.QueryContinuation
    ];

    internal static IEnumerable<string> References(SemanticSlice slice) =>
        (slice.Reactions.IsDefault ? [] : slice.Reactions).SelectMany(reaction => reaction.Triggers)
            .Select(trigger => trigger.RequirementId).OfType<string>();

    internal static SemanticInlineCodeResult Inline(LocatedSemanticSlice located, SemanticReaction reaction, SemanticApplicationContext context)
    {
        var requirementId = reaction.Triggers.Single().RequirementId!;
        var owner = located.Slice.Id;
        var placement = new InlineCodePlacement("reaction", owner.ToString(), requirementId);
        var diagnostics = ImmutableArray.CreateBuilder<ArtifactRenderDiagnostic>();
        if (!SemanticImplementationAdmission.TryGetVerifiedInline(context.Request, new(requirementId, owner, placement), diagnostics, out var code))
        {
            return new(null, placement, diagnostics.ToImmutable());
        }

        var requirement = context.Request.ImplementationRequirements.Single(item => item.RequirementId == requirementId);
        if (!EnvelopeMatches(requirement, owner) || code!.Language != "csharp")
        {
            diagnostics.Add(new("STAGE-INLINE-INTEGRITY", ArtifactRenderDiagnosticSeverity.Error, "The reaction envelope has an invalid role, owner, language, version or capability.", reaction.Id));
            return new(null, placement, diagnostics.ToImmutable());
        }

        var source = context.Events[reaction.Triggers.Single().Source];
        var returnShape = new SemanticReactionReturnShape(source, context);
        if (!returnShape.IsSeparate(code!.Text))
        {
            diagnostics.Add(new(
                SemanticVersionFeatures.MixedReactionEffects,
                ArtifactRenderDiagnosticSeverity.Error,
                returnShape.HasValueItems
                    ? $"Reaction '{reaction.Name}' returns an item that is a value, not a modeled event or command, and would fail at run time. Return modeled events or commands, or an empty result."
                    : $"Reaction '{reaction.Name}' has a mixed or undeterminable event/command return shape. Arc appends events before executing commands, so a failed command leaves events appended; use two reactions, or produce the event and let a second reaction invoke the command.",
                reaction.Id));
        }

        return Present(code!, placement, source, diagnostics, reaction.Id);
    }

    internal static IEnumerable<ArtifactRenderDiagnostic> ReturnDiagnostics(LocatedSemanticSlice located, SemanticApplicationContext context) =>
        (located.Slice.Reactions.IsDefault ? [] : located.Slice.Reactions)
            .Where(reaction => reaction.Triggers is [var trigger] && trigger.RequirementId is not null && SemanticVersionFeatures.HasStandaloneEventBody(reaction, trigger))
            .SelectMany(reaction => Inline(located, reaction, context).Diagnostics)
            .Where(diagnostic => diagnostic.Code == SemanticVersionFeatures.MixedReactionEffects);

    static bool EnvelopeMatches(SemanticImplementationRequirement requirement, SemanticId owner) =>
        requirement is { Role: SemanticImplementationRole.ReactionEffect, ContextVersion: 1, ResultVersion: 1 } &&
        string.Equals(requirement.RequiredCapability, "provider-defined", StringComparison.Ordinal) &&
        requirement.Source.SemanticId == owner;

    static SemanticInlineCodeResult Present(
        InlineCode code,
        InlineCodePlacement placement,
        SemanticEventContract source,
        ImmutableArray<ArtifactRenderDiagnostic>.Builder diagnostics,
        SemanticId reaction)
    {
        var presentation = CSharpInlineLanguagePack.Present(code, verbatim: true);
        diagnostics.AddRange(presentation.Diagnostics.Select(diagnostic =>
            new ArtifactRenderDiagnostic(diagnostic.Code, ArtifactRenderDiagnosticSeverity.Error, diagnostic.Message, reaction)));
        if (BindingCollision(code.Text, source) is { } collision)
        {
            diagnostics.Add(new("STAGE-ESM-012", ArtifactRenderDiagnosticSeverity.Error, $"Reaction body declares '{collision}', which collides with a generated handler binding.", reaction));
        }

        return new(diagnostics.Count > 0 || presentation.Degraded ? null : code, placement, diagnostics.ToImmutable());
    }

    // This detects lexical declaration collisions, not arbitrary C# semantic errors or unsafe runtime calls.
    static string? BindingCollision(string body, SemanticEventContract source)
    {
        var bindings = source.Properties.Select(property => Identifiers.ToPascalCase(property.Name)).Concat(["event", "context"]).ToArray();
        var duplicate = bindings.GroupBy(name => name, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null) return duplicate.Key;
        var syntax = SyntaxFactory.ParseStatement("{\n" + body + "\n}");
        return syntax.DescendantNodes().Select(DeclaredName).OfType<string>().FirstOrDefault(bindings.Contains);
    }

    static string? DeclaredName(SyntaxNode node) => _declarations.Contains(node.Kind())
        ? node.ChildTokens().FirstOrDefault(token => token.IsKind(SyntaxKind.IdentifierToken)).ValueText
        : null;
}
