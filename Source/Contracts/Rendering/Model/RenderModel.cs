// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering.Model;

internal sealed record RenderModel(int Minor, RenderModelSource Source, RenderModelScope Scope, RenderModelItems Items)
{
    internal const string Schema = "stage-render-model/v1";

    [System.Text.Json.Serialization.JsonPropertyOrder(-3)]
    [System.Text.Json.Serialization.JsonPropertyName("schema")]
    public string SchemaVersion => Schema;

    [System.Text.Json.Serialization.JsonPropertyOrder(-2)]
    public int Canonicalization => 1;

    internal byte[] ToCanonicalJson() => RenderModelJson.Serialize(this);
}

internal sealed record RenderModelSource(SemanticId ApplicationId, SemanticRevision Revision, LanguageVersion LanguageVersion, SemanticVersion SemanticVersion, string? ScreenplayVersion);
internal sealed record RenderModelScope(RenderSequence<ArtifactRenderScope> Scopes, RenderSequence<RenderSliceRef> Slices);
internal sealed record RenderSliceRef(SemanticId Id, RenderLocation Location, RenderSequence<string> NamePath);
internal sealed record RenderLocation(SemanticId Module, RenderSequence<SemanticId> FeaturePath, SemanticId Slice);
internal sealed record RenderFlags(bool HasIdentifier, bool HasGeneratedProperties, bool HasProtectedReads);

internal sealed record RenderModelItems(ImmutableArray<RenderCommand> Commands, ImmutableArray<RenderReaction> Reactions)
{
    public bool Equals(RenderModelItems? other) => other is not null && Commands.SequenceEqual(other.Commands) && Reactions.SequenceEqual(other.Reactions);
    public override int GetHashCode() => HashCode.Combine(new RenderSequence<RenderCommand>(Commands), new RenderSequence<RenderReaction>(Reactions));
}

internal sealed record RenderProvenance
{
    public string Status => "Absent";
    public string Reason => "loader does not retain compiler SourceMap";
    internal static RenderProvenance Absent { get; } = new();
}

internal abstract record RenderMember
{
    public RenderProvenance Provenance => RenderProvenance.Absent;
}

internal sealed record RenderCommand(SemanticId Id, string Name, RenderSequence<string> NameWords, RenderLocation Location, RenderFlags Flags, RenderCommandPayload Payload) : RenderMember
{
    public string Kind => "command";
}

internal sealed record RenderReaction(SemanticId Id, string Name, RenderSequence<string> NameWords, RenderLocation Location, RenderFlags Flags, RenderReactionPayload Payload) : RenderMember
{
    public string Kind => "reaction";
}
