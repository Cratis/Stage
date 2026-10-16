// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Specifications.Commands.for_SemanticRuleEvaluation.when_checking_not_empty;

public class with_a_generated_concept_value : given.a_not_empty_rule
{
    void Establish() => Prepare("\"   \"", concept: true);

    void Because() => _failure = SemanticGenerationShapes.GeneratedFailure(_concepts.Single(), "name", SemanticValue.Text("   "));

    [Fact] void should_report_the_reference_generated_contract_message_instead_of_the_authored_rule_message() => _failure.ShouldEqual("Generated fixture 'name' fails concept 'Name' rules");
}
