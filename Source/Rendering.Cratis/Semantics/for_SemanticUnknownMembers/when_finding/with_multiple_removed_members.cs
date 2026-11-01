// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticUnknownMembers.when_finding.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticUnknownMembers.when_finding;

public class with_multiple_removed_members : a_populated_model
{
    void Because()
    {
        _known.Remove("SemanticCommand.Reads");
        _known.Remove("SemanticDecisionRead.Name");
        _unknown = SemanticUnknownMembers.Find(_model, _known);
    }

    [Fact] void should_continue_below_an_unknown_member() => _unknown.Any(member => member.Member == "SemanticDecisionRead.Name").ShouldBeTrue();
    [Fact] void should_preserve_the_command_owner_below_the_collection() => _unknown.ShouldEachConformTo(member => _readingCommands.Any(command => command.Id == member.Owner));
    [Fact] void should_sort_by_path_then_key() => _unknown.SequenceEqual(_unknown.OrderBy(member => member.Path, StringComparer.Ordinal).ThenBy(member => member.Member, StringComparer.Ordinal)).ShouldBeTrue();
    [Fact] void should_produce_the_same_diagnostics_on_a_second_walk() => _unknown.SequenceEqual(SemanticUnknownMembers.Find(_model, _known)).ShouldBeTrue();
}
