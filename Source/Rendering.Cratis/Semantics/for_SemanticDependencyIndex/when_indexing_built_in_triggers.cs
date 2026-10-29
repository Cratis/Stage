// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex;

public class when_indexing_built_in_triggers : given.a_dependency_index
{
    [Theory]
    [InlineData(SemanticReactionTriggerKind.Startup)]
    [InlineData(SemanticReactionTriggerKind.Shutdown)]
    [InlineData(SemanticReactionTriggerKind.Interval)]
    [InlineData(SemanticReactionTriggerKind.Schedule)]
    public void should_cover_a_reaction_without_a_source_identity(SemanticReactionTriggerKind kind)
    {
        var reaction = new SemanticReaction(Identity("built-in-reaction"), "BuiltIn", [new(kind)
        {
            Every = kind == SemanticReactionTriggerKind.Interval ? 60 : null,
            At = kind == SemanticReactionTriggerKind.Schedule ? 3600 : null,
            Produces = [new(_registerProject.Events.Single().Id, null, null, [])]
        }]);
        _registerProject = _registerProject with { Reactions = [reaction] };
        BuildIndex();

        Assert.True(_index.IsSemanticGraphComplete, string.Join(Environment.NewLine, _index.IncompleteReasons));
        _index.Dependents(_registerProject.Events.Single().Id).ShouldContain(reaction.Id);
        _index.Edges.Any(edge => edge.Dependent == reaction.Id && edge.Member == "SemanticReactionTrigger.Source").ShouldBeFalse();
    }

    [Theory]
    [InlineData(SemanticReactionTriggerKind.Startup)]
    [InlineData(SemanticReactionTriggerKind.Shutdown)]
    public void should_cover_a_specification_without_a_trigger_identity(SemanticReactionTriggerKind kind)
    {
        var specification = _registerProject.Specifications.First() with { When = null, WhenTrigger = new(kind, []) };
        _registerProject = _registerProject with { Specifications = [specification] };
        BuildIndex();

        Assert.True(_index.IsSemanticGraphComplete, string.Join(Environment.NewLine, _index.IncompleteReasons));
        _index.Edges.Any(edge => edge.Dependent == specification.Id && edge.Member == "SemanticSpecificationTrigger.Trigger").ShouldBeFalse();
    }

    [Theory]
    [InlineData(SemanticReactionTriggerKind.Event)]
    [InlineData(SemanticReactionTriggerKind.ApplicationTrigger)]
    public void should_still_refuse_a_missing_declared_reaction_source(SemanticReactionTriggerKind kind)
    {
        _registerProject = _registerProject with { Reactions = [new(Identity("missing-source-reaction"), "MissingSource", [new(kind)])] };
        BuildIndex();

        _index.IsSemanticGraphComplete.ShouldBeFalse();
        _index.IncompleteReasons.Any(reason => reason.StartsWith("SemanticReactionTrigger.Source:", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public void should_still_refuse_a_missing_application_trigger_in_a_specification()
    {
        var specification = _registerProject.Specifications.First() with { When = null, WhenTrigger = new(SemanticReactionTriggerKind.ApplicationTrigger, []) };
        _registerProject = _registerProject with { Specifications = [specification] };
        BuildIndex();

        _index.IsSemanticGraphComplete.ShouldBeFalse();
        _index.IncompleteReasons.Any(reason => reason.StartsWith("SemanticSpecificationTrigger.Trigger:", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
