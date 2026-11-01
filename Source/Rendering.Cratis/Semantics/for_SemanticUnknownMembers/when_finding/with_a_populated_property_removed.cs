// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticUnknownMembers.when_finding.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticUnknownMembers.when_finding;

public class with_a_populated_property_removed : a_populated_model
{
    void Because()
    {
        _known.Remove("SemanticCommand.Reads");
        _unknown = SemanticUnknownMembers.Find(_model, _known);
    }

    [Fact] void should_report_only_populated_occurrences() => _unknown.Length.ShouldEqual(_readingCommands.Length);
    [Fact] void should_identify_the_member_without_values() => _unknown.ShouldEachConformTo(member => member.Member == "SemanticCommand.Reads");
    [Fact] void should_use_the_enclosing_command_ids() => _unknown.Select(member => member.Owner).SequenceEqual(_readingCommands.Select(command => command.Id)).ShouldBeTrue();
    [Fact] void should_fail_closed_for_security() => _unknown.ShouldEachConformTo(member => member.SecurityRelevant);
    [Fact] void should_identify_the_owner_kind() => _unknown.ShouldEachConformTo(member => member.OwnerKind == "command");
    [Fact] void should_use_a_structural_indexed_path() => _unknown[0].Path.ShouldEqual("Application.Modules[0].Features[0].Slices[0].Commands[0].Reads");
    [Fact] void should_not_expose_the_command_name() => _unknown[0].Path.ShouldNotContain(_readingCommand.Name);
}
