// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Stage.Contracts.Rendering;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Rendering.Cratis;

public static partial class CratisRendering
{
    static readonly string[] _eventTypeAttributeNames = ["EventType", "EventTypeAttribute", "Cratis.Chronicle.Events.EventType", "Cratis.Chronicle.Events.EventTypeAttribute"];

    static CratisPublicationCheck.ChangesEventTypeIds? CheckEventTypeIds(ArtifactRenderPlan plan, Func<string, string?> readExistingFile)
    {
        var changes = plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal))
            .SelectMany(artifact => EventTypeIdChanges(artifact, readExistingFile))
            .OrderBy(change => change.Change.Path, StringComparer.Ordinal)
            .ThenBy(change => change.Change.TypeName, StringComparer.Ordinal).ToImmutableArray();

        return changes.IsEmpty ? null : new(
            [.. changes.Select(change => change.Change)],
            [.. changes.Select(change => new ArtifactRenderDiagnostic("STAGE-EVENT-002", ArtifactRenderDiagnosticSeverity.Error, EventTypeIdMessage(change), default))]);
    }

    static IEnumerable<EventTypeIdDifference> EventTypeIdChanges(PlannedArtifact artifact, Func<string, string?> readExistingFile)
    {
        var planned = EventTypeIds(Encoding.UTF8.GetString(artifact.Bytes.AsSpan()));
        if (planned.Length == 0 || readExistingFile(artifact.RelativePath) is not { } existing || !existing.Contains("EventType", StringComparison.Ordinal)) return [];
        var syntax = CSharpSyntaxTree.ParseText(existing).GetCompilationUnitRoot();
        if (syntax.ContainsDiagnostics)
        {
            return planned.Select(type => new EventTypeIdDifference(new(artifact.RelativePath, type.TypeName, "<unreadable>", type.Id), true));
        }

        var stored = EventTypeIds(syntax);
        return planned.SelectMany(type => MatchingEventTypes(type, stored).Where(previous => previous.Id is null || previous.Id != type.Id)
            .Select(previous => new EventTypeIdDifference(new(artifact.RelativePath, type.TypeName, previous.Id ?? previous.Expression, type.Id), previous.Id is null)));
    }

    static EventTypeIdentity[] MatchingEventTypes(EventTypeIdentity planned, EventTypeIdentity[] stored)
    {
        var candidates = stored.Where(previous => previous.Name == planned.Name).ToArray();
        var qualified = candidates.Where(previous => previous.TypeName == planned.TypeName).ToArray();

        return qualified.Length > 0 ? qualified : candidates;
    }

    static EventTypeIdentity[] EventTypeIds(string text) => text.Contains("EventType", StringComparison.Ordinal)
        ? EventTypeIds(CSharpSyntaxTree.ParseText(text).GetCompilationUnitRoot()) : [];

    static EventTypeIdentity[] EventTypeIds(CompilationUnitSyntax syntax) =>
        [.. syntax.DescendantNodes().OfType<TypeDeclarationSyntax>().SelectMany(type => type.AttributeLists.SelectMany(list => list.Attributes)
            .Where(IsEventTypeAttribute).Select(attribute => EventTypeId(type, attribute)))];

    static bool IsEventTypeAttribute(AttributeSyntax attribute) => _eventTypeAttributeNames.Contains(
        attribute.Name.ToString().Replace("global::", string.Empty, StringComparison.Ordinal), StringComparer.Ordinal);

    static EventTypeIdentity EventTypeId(TypeDeclarationSyntax type, AttributeSyntax attribute)
    {
        var typeName = string.Join('.', type.Ancestors().Reverse().Select(node => node switch
        {
            BaseNamespaceDeclarationSyntax space => string.Join('.', space.Name.DescendantTokens().Where(token => token.RawKind == (int)SyntaxKind.IdentifierToken).Select(token => token.ValueText)),
            TypeDeclarationSyntax parent => parent.Identifier.ValueText,
            _ => null
        }).OfType<string>().Append(type.Identifier.ValueText));
        var argument = attribute.ArgumentList?.Arguments.FirstOrDefault(argument =>
            argument.NameColon?.Name.Identifier.ValueText == "id" || (argument.NameColon is null && argument.NameEquals is null));
        if (argument is null) return new(typeName, type.Identifier.ValueText, type.Identifier.ValueText, string.Empty);
        var id = argument.Expression is LiteralExpressionSyntax literal && literal.RawKind == (int)SyntaxKind.StringLiteralExpression
            ? literal.Token.ValueText : null;

        return new(typeName, type.Identifier.ValueText, id is { Length: 0 } ? type.Identifier.ValueText : id, argument.Expression.ToString());
    }

    static string EventTypeIdMessage(EventTypeIdDifference difference)
    {
        var change = difference.Change;
        if (difference.Unreadable)
        {
            return $"Event type '{change.TypeName}' in '{change.Path}' has an existing [EventType] id that cannot be read; this plan would store it as '{change.PlannedId}'. Resolve the existing id before publishing, then pin it in the model or write an event type migration.";
        }

        return $"Event type '{change.TypeName}' in '{change.Path}' is stored as Chronicle event type '{change.ExistingId}', but this plan changes its [EventType] id to '{change.PlannedId}'; events already stored under '{change.ExistingId}' would no longer be read. Pin the existing id in the model (`id \"{change.ExistingId}\"`), add a new event instead of renaming, or write an event type migration. If the release baseline records the wrong name, correct it per Screenplay decision 0070.";
    }

    sealed record EventTypeIdentity(string TypeName, string Name, string? Id, string Expression);
    sealed record EventTypeIdDifference(CratisEventTypeIdChange Change, bool Unreadable);
}
