// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticCommandOutcome;

public class when_mapping_validation : Specification
{
    CommandResult _result = null!;

    void Because()
    {
        var rejected = new SemanticRejected(SemanticWorld.Empty, SemanticRejectionCategory.Validation, null, "First")
        {
            ValidationFailures =
            [
                new("First", SemanticValidationSeverity.Error),
                new("Second", SemanticValidationSeverity.Warning)
            ]
        };
        _result = SemanticCommandOutcome.Map(rejected, CorrelationId.NotSet, null, "command-1");
    }

    [Fact] void should_return_all_failures() => _result.ValidationResults.Select(failure => failure.Message).ShouldContainOnly(["First", "Second"]);
}
