// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.Commands.for_SemanticRuleEvaluation.when_checking_not_empty;

public class with_a_generated_concept_value : given.a_not_empty_rule
{
    void Establish()
    {
        Prepare("\"   \"", concept: true);
        var property = _command.Properties.Single(property => property.Name == "name");
        _command = _command with { Properties = [.. _command.Properties.Select(candidate => candidate.Id == property.Id ? candidate with { IsGenerated = true } : candidate)] };
        _specification = _specification with { When = _specification.When! with { GeneratedValues = [.. _specification.When.Values.Where(value => value.TargetProperty == property.Id)] } };
    }

    void Because() => _failure = SemanticRuleEvaluation.GeneratedFailure(_command, _specification, _concepts);

    [Fact] void should_reject_generated_whitespace_with_the_authored_message() => _failure.ShouldEqual("Name is required");
}
