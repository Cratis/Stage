// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.when_recording_input_provenance;

public class and_components_are_attached : given.a_provenance_request
{
    static readonly string Digest = new('a', 64);
    ArtifactRenderPlan _original = null!;
    ArtifactRenderPlan _attached = null!;

    void Establish() => _original = Plan(With([Input("kit", "1", "one")], [Requirement("r1")]));

    void Because() => _attached = _original.WithInputComponents([
        new(ArtifactRenderComponentKind.TemplateEngine, "scriban", "7.5.3"),
        new(ArtifactRenderComponentKind.Kit, "acme", "1.0.0", Digest)]);

    [Fact] void should_sort_by_kind_then_name() => _attached.InputProvenance.Components.Select(_ => _.Kind).ShouldEqual([ArtifactRenderComponentKind.Kit, ArtifactRenderComponentKind.TemplateEngine]);
    [Fact] void should_retain_the_artifacts() => _attached.Artifacts.ShouldEqual(_original.Artifacts);
    [Fact] void should_retain_the_scopes() => _attached.Scope.ShouldEqual(_original.Scope);
    [Fact] void should_retain_the_output_digest() => _attached.Digest.ShouldEqual(_original.Digest);
    [Fact] void should_change_the_provenance_digest() => _attached.InputProvenance.Digest.ShouldNotEqual(_original.InputProvenance.Digest);
    [Fact] void should_retain_the_inputs() => _attached.InputProvenance.Inputs.ShouldEqual(_original.InputProvenance.Inputs);
    [Fact] void should_replace_components_on_a_second_attachment() => _attached.WithInputComponents([]).InputProvenance.Components.ShouldBeEmpty();
    [Fact] void should_refuse_a_duplicate_kind_and_name() => Catch.Exception(() => _original.WithInputComponents([
        new(ArtifactRenderComponentKind.Kit, "acme", "1"),
        new(ArtifactRenderComponentKind.Kit, "acme", "2")])).ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_a_malformed_digest() => Catch.Exception(() => _original.WithInputComponents([
        new(ArtifactRenderComponentKind.Kit, "acme", "1", Digest.ToUpperInvariant())])).ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_a_short_digest() => Catch.Exception(() => _original.WithInputComponents([
        new(ArtifactRenderComponentKind.Kit, "acme", "1", "abc")])).ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_an_unknown_kind() => Catch.Exception(() => _original.WithInputComponents([
        new(ArtifactRenderComponentKind.Unknown, "acme", "1")])).ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_a_default_array() => Catch.Exception(() => _original.WithInputComponents(default)).ShouldBeOfExactType<InvalidArtifactRenderContract>();
}
