// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering.Model;

internal static partial class RenderModelBuilder
{
    [GeneratedRegex(@"(?<left>[a-z0-9])(?<right>[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex WordBoundaries { get; }

    internal static RenderModel Build(ArtifactRenderRequest request)
    {
        var selected = RenderScopeSelection.Select(request).OrderBy(value => value.Slice.Id.ToString(), StringComparer.Ordinal).ToArray();
        var normalizer = new RenderPayloadNormalizer(request.Model.Application);
        var commands = selected.SelectMany(location => location.Slice.Commands.Select(command => Command(command, location, normalizer))).OrderBy(value => value.Id.ToString(), StringComparer.Ordinal);
        var reactions = selected.SelectMany(location => location.Slice.Reactions.Select(reaction => Reaction(reaction, location, normalizer))).OrderBy(value => value.Id.ToString(), StringComparer.Ordinal);
        var scopes = new[] { request.Scope }.Concat(request.AdditionalScopes).Distinct().OrderBy(value => value.Artifact.ToString(), StringComparer.Ordinal).ThenBy(value => value.Kind);
        var source = new RenderModelSource(request.Model.Application.Id, request.Model.Revision, request.Model.LanguageVersion, request.Model.SemanticVersion, null);
        var scope = new RenderModelScope(new(scopes), new(selected.Select(value => new RenderSliceRef(value.Slice.Id, Location(value), new(value.Path)))));

        return new(0, source, scope, new([.. commands], [.. reactions]));
    }

    internal static RenderSequence<string> Words(string name) => new(WordBoundaries.Replace(name, "${left} ${right}").Split([' ', '_', '-'], StringSplitOptions.RemoveEmptyEntries));

    static RenderCommand Command(SemanticCommand command, LocatedSemanticSlice location, RenderPayloadNormalizer normalizer) => new(
        command.Id, command.Name, Words(command.Name), Location(location), new(command.Properties.Any(property => property.IsIdentifier), command.Properties.Any(property => property.IsGenerated), !command.Reads.IsEmpty), normalizer.Command(command, location.Slice));

    static RenderReaction Reaction(SemanticReaction reaction, LocatedSemanticSlice location, RenderPayloadNormalizer normalizer) => new(
        reaction.Id, reaction.Name, Words(reaction.Name), Location(location), new(false, false, false), normalizer.Reaction(reaction));

    static RenderLocation Location(LocatedSemanticSlice location) => new(location.Module.Id, new(location.Features.Select(value => value.Id)), location.Slice.Id);
}
