// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex;

public class when_encountering_an_unhandled_discriminator : given.a_dependency_index
{
    [Theory]
    [InlineData(SemanticReactionTriggerKind.Unknown)]
    [InlineData((SemanticReactionTriggerKind)999)]
    public void should_refuse_unhandled_reaction_triggers(SemanticReactionTriggerKind kind)
    {
        _registerProject = _registerProject with { Reactions = [new(Identity("reaction"), "Reaction", [new(kind) { Source = _registerProject.Events.Single().Id }])] };
        ShouldRefuse();
    }

    [Theory]
    [InlineData(SemanticQueryCardinality.Unknown)]
    [InlineData((SemanticQueryCardinality)999)]
    public void should_refuse_unhandled_query_cardinality(SemanticQueryCardinality kind)
    {
        _projectLookup = _projectLookup with { Queries = [_projectLookup.Queries.Single() with { Cardinality = kind }] };
        ShouldRefuse();
    }

    [Theory]
    [InlineData(SemanticSliceKind.Unknown)]
    [InlineData((SemanticSliceKind)999)]
    public void should_refuse_unhandled_slice_kinds(SemanticSliceKind kind)
    {
        _registerProject = _registerProject with { Kind = kind };
        ShouldRefuse();
    }

    [Theory]
    [InlineData(SemanticCaptureMapKind.Unknown)]
    [InlineData((SemanticCaptureMapKind)999)]
    public void should_refuse_unhandled_discriminators_in_scalar_only_children(SemanticCaptureMapKind kind)
    {
        _registerProject = _registerProject with { Captures = [new(Identity("capture"), "Capture", "id", [new(kind, ["id"])], [])] };
        ShouldRefuse();
    }

    [Theory]
    [InlineData(SemanticConstraintKind.Unknown)]
    [InlineData((SemanticConstraintKind)999)]
    public void should_refuse_unhandled_constraint_kinds(SemanticConstraintKind kind)
    {
        _projectLookup = _projectLookup with { Constraints = [new("Unique", kind, SemanticConstraintScope.EventSequence, [new(_registerProject.Events.Single().Id, [])], [], false, null)] };
        ShouldRefuse();
    }

    [Theory]
    [InlineData(SemanticConstraintScope.Unknown)]
    [InlineData((SemanticConstraintScope)999)]
    public void should_refuse_unhandled_constraint_scopes(SemanticConstraintScope kind)
    {
        _projectLookup = _projectLookup with { Constraints = [new("Unique", SemanticConstraintKind.UniqueEventOccurrence, kind, [new(_registerProject.Events.Single().Id, [])], [], false, null)] };
        ShouldRefuse();
    }

    [Theory]
    [InlineData(SemanticTypeReferenceKind.Unknown)]
    [InlineData((SemanticTypeReferenceKind)999)]
    public void should_refuse_unhandled_reference_kinds(SemanticTypeReferenceKind kind)
    {
        var contract = _registerProject.Events.Single();
        _registerProject = _registerProject with { Events = [contract with { Properties = [.. contract.Properties.Select(property => property with { Type = new(kind, SemanticPrimitiveType.Unknown, _projectName.Id, false, false) })] }] };
        ShouldRefuse();
    }

    [Fact]
    public void should_refuse_unhandled_projection_targets()
    {
        var lookup = _projectLookup with { Projections = [_projectLookup.Projections.Single() with { Target = (SemanticProjectionTargetKind)999 }] };
        ShouldRefuse(CreateIndex(projectLookup: lookup));
    }

    [Fact]
    public void should_refuse_unhandled_context_roles()
    {
        var request = _request with { TypedContextDescriptors = [new("body", (SemanticImplementationRole)999, 1, _registerProject.Commands.Single().Id, [])] };
        ShouldRefuse(CreateIndex(request: request));
    }

    [Fact] void should_refuse_defined_but_unclassified_enums() => SemanticDependencyVariants.Known(SemanticKind.Property).ShouldBeFalse();

    [Fact]
    public void should_refuse_all_defined_unknown_sentinels()
    {
        var enums = typeof(SemanticApplication).Assembly.GetExportedTypes().Where(type => type.IsEnum && type.Namespace == typeof(SemanticApplication).Namespace);
        var unknowns = enums.Where(type => Enum.GetNames(type).Contains("Unknown", StringComparer.Ordinal)).Select(type => (Enum)Enum.Parse(type, "Unknown"));
        Assert.All(unknowns, unknown => SemanticDependencyVariants.Known(unknown).ShouldBeFalse());
    }

    void ShouldRefuse(SemanticDependencyIndex? index = null)
    {
        index ??= CreateIndex();
        index.IsSemanticGraphComplete.ShouldBeFalse();
        index.IncompleteReasons.Any(reason => reason.Contains("unknown variant", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
