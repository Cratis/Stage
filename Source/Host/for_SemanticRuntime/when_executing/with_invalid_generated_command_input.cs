// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class with_invalid_generated_command_input : a_generated_runtime
{
    SemanticRejected _result = null!;
    async Task Because() => _result = (SemanticRejected)await Execute(name: "");
    [Fact] void should_reject_before_producing_a_response() => _result.Category.ShouldEqual(SemanticRejectionCategory.Validation);
    [Fact] void should_preserve_the_world() => _result.World.Facts.ShouldBeEmpty();
}
