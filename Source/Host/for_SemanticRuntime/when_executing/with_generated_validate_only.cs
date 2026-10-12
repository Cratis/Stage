// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class with_generated_validate_only : a_generated_runtime
{
    SemanticAccepted _first = null!;
    SemanticAccepted _second = null!;

    async Task Because()
    {
        _first = (SemanticAccepted)await Execute();
        _second = (SemanticAccepted)await Execute();
    }

    [Fact] void should_generate_fresh_values_per_execution() => ((SemanticScalarExecutionResponse)_first.Response!).Value.ShouldNotEqual(((SemanticScalarExecutionResponse)_second.Response!).Value);
    [Fact] async Task should_not_append_during_validation() => await ((IAppendSemanticFacts)_appender).DidNotReceive().Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>());
}
