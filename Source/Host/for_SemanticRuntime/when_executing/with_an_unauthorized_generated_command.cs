// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class with_an_unauthorized_generated_command : a_generated_runtime
{
    SemanticRejected _result = null!;
    async Task Because() => _result = (SemanticRejected)await Execute(authenticated: false, forged: true);
    [Fact] void should_deny_before_inspecting_the_forged_input() => _result.Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
    [Fact] void should_preserve_the_world() => _result.World.Facts.ShouldBeEmpty();
}
