// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Lifecycles.for_LifecycleSubjects.when_building_a_command_source;

public class with_matching_values : given.a_subject
{
    JsonObject _result = null!;

    void Establish() => _command = _command with
    {
        Properties = [.. _command.Properties.Select(property => property.IsIdentifier ? property : property with { Name = "STATUS" }),
            _subject.Properties.Single(property => property.Name == "overdue") with { IsGenerated = true },
            _status with { Id = Id(102), Name = "absent" }]
    };

    void Because() => _result = LifecycleSubjects.CommandSource(_plan, _command,
        [.. Status(SemanticValue.Text("sent")), new(_subject.Properties.Single(property => property.Name == "overdue").Id, SemanticValue.Boolean(true))]);

    [Fact] void should_match_names_case_insensitively_and_keep_command_spelling() => _result["STATUS"]!.GetValue<string>().ShouldEqual("sent");
    [Fact] void should_omit_identifiers_generated_and_unmatched_members() => _result.Count.ShouldEqual(1);
}
