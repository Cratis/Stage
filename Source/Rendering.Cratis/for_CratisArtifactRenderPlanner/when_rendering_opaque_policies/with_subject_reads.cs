// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_opaque_policies;

public class with_subject_reads : Specification
{
    const string Body = "return string.Equals(context.Subject, context.Identity.ClaimValue(\"owner\"), StringComparison.Ordinal);";
    ExecutableSemanticModel _model = null!;
    Assembly _assembly = null!;

    void Because()
    {
        var loaded = opaque_policy_model.Load(opaque_policy_model.Source(Body));
        _model = loaded.Model;
        var plan = opaque_policy_model.Plan(loaded);
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        _assembly = opaque_policy_model.Compile(plan);
    }

    [Fact] void should_supply_the_command_identifier() => opaque_policy_model.AllowsCommand(_assembly, _model, Owner("invoice-one"), opaque_policy_model.Receipt).ShouldBeTrue();
    [Fact] void should_compare_the_command_identifier() => opaque_policy_model.AllowsCommand(_assembly, _model, Owner("invoice-two"), opaque_policy_model.Receipt).ShouldBeFalse();
    [Fact] void should_supply_the_query_key() => opaque_policy_model.AllowsQuery(_assembly, _model, Owner("invoice-one"), opaque_policy_model.Receipt, "invoice-one").ShouldBeTrue();
    [Fact] void should_compare_the_query_key() => opaque_policy_model.AllowsQuery(_assembly, _model, Owner("invoice-two"), opaque_policy_model.Receipt, "invoice-one").ShouldBeFalse();
    [Fact] void should_deny_a_query_without_its_key() => opaque_policy_model.AllowsQuery(_assembly, _model, Owner(string.Empty), opaque_policy_model.Receipt, null).ShouldBeFalse();

    static System.Security.Claims.ClaimsPrincipal Owner(string invoice) =>
        new(new System.Security.Claims.ClaimsIdentity([new System.Security.Claims.Claim("OWNER", invoice)], "fixture"));
}
#endif
