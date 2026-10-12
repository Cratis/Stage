// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_settling_reactions;

public class with_a_denied_invocation : a_reaction_runtime
{
    protected override string Model => "policy Worker\n  require role \"Automation\"\n" + StartSource + InvokingSource.Replace("amount Decimal\n        produces", "amount Decimal\n        authorize Worker\n        produces", StringComparison.Ordinal);

    async Task Because() => _result = await Execute(principal: new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Automation")], "spec")));

    [Fact] void should_deny_the_invocation_without_forwarding_the_caller() => ((SemanticRejected)_result).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
    [Fact] void should_append_nothing() => _batches.ShouldBeEmpty();
    [Fact] void should_return_the_original_world() => _result.World.Facts.ShouldBeEmpty();
}
