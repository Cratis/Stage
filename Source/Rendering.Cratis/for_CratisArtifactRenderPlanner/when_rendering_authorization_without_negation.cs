// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// A model without `not` keeps its two-valued policy rendering: without negation, deny-on-unknown decides exactly
/// as treating an unknown comparison as false. Shared helpers are emitted independently of that choice.
/// </summary>
public class when_rendering_authorization_without_negation : Specification
{
    ArtifactRenderPlan _plan = null!;
    string _policies = null!;

    void Because()
    {
        _plan = invoice_model.Plan(invoice_model.Compile(when_rendering_portable_authorization.Source));
        _policies = string.Join('\n', _plan.Artifacts.Where(artifact => artifact.RelativePath.StartsWith("GeneratedPolicies/StagePolicy_", StringComparison.Ordinal)).Select(Text));
    }

    [Fact] void should_plan_the_application() => _plan.Success.ShouldBeTrue();
    [Fact] void should_keep_registration_without_anonymous_opt_in() => _policies.Contains("evaluatesAnonymous", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_keep_the_two_valued_claim_comparison() => _policies.Contains("PolicyValues.Match(context, \"owner\", ", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_the_operation_two_valued() => (_policies.Contains("Truth(", StringComparison.Ordinal) || _policies.Contains("PolicyValues.Not(", StringComparison.Ordinal) || _policies.Contains("bool?", StringComparison.Ordinal)).ShouldBeFalse();

    static string Text(PlannedArtifact artifact) => Encoding.UTF8.GetString(artifact.Bytes.AsSpan());
}
