// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticUnknownMembers.when_finding.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticUnknownMembers.when_finding;

public class with_scoped_owners : a_populated_model
{
    [Fact]
    void should_rebind_only_at_selected_owners()
    {
        var known = new Dictionary<string, SemanticSurfaceDisposition>(_known, StringComparer.Ordinal);
        known.Remove("SemanticDecisionRead.Name");
        var scope = Scope([_readingCommand.Id], []);
        var found = SemanticUnknownMembers.Find(_model, known, scope);
        found.ShouldNotBeEmpty();
        var nearest = SemanticUnknownMembers.Find(_model, known).ToDictionary(member => member.Path);
        found.ShouldEachConformTo(member => member.Owner == (nearest[member.Path].Owner == _readingCommand.Id ? _readingCommand.Id : _model.Application.Id));
        found.Any(member => member.Owner == _readingCommand.Id).ShouldBeTrue();
        found.Any(member => member.Owner == _model.Application.Id).ShouldEqual(_readingCommands.Length > 1);
    }

    [Fact]
    void should_keep_application_fallback_with_explicitly_empty_owners()
    {
        var known = new Dictionary<string, SemanticSurfaceDisposition>(_known, StringComparer.Ordinal);
        known.Remove("SemanticDecisionRead.Name");
        var found = SemanticUnknownMembers.Find(_model, known, Scope([], []));
        found.ShouldNotBeEmpty();
        found.ShouldEachConformTo(member => member.Owner == _model.Application.Id);
    }

    [Fact]
    void should_skip_an_excluded_slice_subtree()
    {
        var known = new Dictionary<string, SemanticSurfaceDisposition>(_known, StringComparer.Ordinal);
        known.Remove("SemanticDecisionRead.Name");
        var slice = SemanticVersionFeatures.Slices(_model.Application).Single(slice => slice.Commands.Any(command => command.Id == _readingCommand.Id));
        var found = SemanticUnknownMembers.Find(_model, known, Scope([_readingCommand.Id], [slice.Id]));
        found.Any(member => member.Owner == _readingCommand.Id).ShouldBeFalse();
        found.Length.ShouldEqual(_readingCommands.Where(command => !slice.Commands.Contains(command)).Sum(command => command.Reads.Length));
    }

    [Fact]
    void should_preserve_repeated_occurrences_in_structural_order()
    {
        var known = new Dictionary<string, SemanticSurfaceDisposition>(_known, StringComparer.Ordinal);
        known.Remove("SemanticCommand.Reads");
        var model = SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(_model));
        var slice = SemanticVersionFeatures.Slices(model.Application).Single(slice => slice.Commands.Any(command => command.Id == _readingCommand.Id));
        typeof(SemanticSlice).GetProperty(nameof(SemanticSlice.Commands)).SetValue(slice, ImmutableArray.Create(_readingCommand, _readingCommand));
        var scope = Scope([_readingCommand.Id], []);
        var found = SemanticUnknownMembers.Find(model, known, scope);
        found.Count(member => member.Owner == _readingCommand.Id).ShouldEqual(2);
        found.SequenceEqual(SemanticUnknownMembers.Find(model, known, scope)).ShouldBeTrue();
        found.SequenceEqual(found.OrderBy(member => member.Path, StringComparer.Ordinal).ThenBy(member => member.Member, StringComparer.Ordinal)).ShouldBeTrue();
    }

    static SemanticUnknownMemberScope Scope(SemanticId[] owners, SemanticId[] excluded) => new(owners.ToImmutableHashSet(), excluded.ToImmutableHashSet(), ImmutableHashSet<string>.Empty);
}
