// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_refusing_opaque_policies;

/// <summary>
/// Screenplay defines no three-valued result for an opaque operand, so its executable model cannot carry one under
/// <c language="csharp">not</c>. Stage's own nested-predicate refusal (<c language="csharp">STAGE-ESM-015</c>) is a second, fail-closed guard.
/// </summary>
public class with_an_opaque_operand_under_not : Specification
{
    Exception? _error;

    void Because()
    {
        var model = opaque_policy_model.Load(opaque_policy_model.Source("return true;")).Model;
        var negated = model.Application with
        {
            Policies = [.. model.Application.Policies.Select(policy => policy.Name == "Custom" ? policy with { Condition = new SemanticNotPolicyCondition(policy.Condition) } : policy)]
        };
        _error = Catch.Exception(() => ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, negated));
    }

    [Fact] void should_not_be_representable() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
}
#endif
