// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.Commands.for_SemanticRuleEvaluation.when_checking_not_empty;

public class with_an_empty_collection : given.a_not_empty_rule
{
    void Establish() => Prepare("[]", collection: true);
    void Because() => _failure = SemanticRuleEvaluation.FirstFailure(_command, _specification, _application);

    [Fact] void should_reject_with_the_authored_message() => _failure.ShouldEqual("Name is required");
}
