// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_checking_publication;

public class without_overwriting_aggregate_paths : given.a_policy_plan
{
    void Establish()
    {
        _plan = ArtifactRenderPlan.Create(new(_loaded.Model, _loaded.Plan, CratisRendering.CreateProfile(_loaded.Model.Application.Name, new("InvoiceApp", "Invoices")), _plan.Scope), [PlannedArtifact.CreateText("Billing/Unprotected.cs", "// no policies")], []);
        _existing["GeneratedPolicies/Policies.cs"] = LegacyPolicies;
    }

    void Because() => _check = CratisRendering.CheckPublication(_plan, Read);
    [Fact] void should_be_compatible() => _check.ShouldBeOfExactType<CratisPublicationCheck.Compatible>();
    [Fact] void should_not_read_files_the_plan_does_not_write() => _readPaths.ShouldBeEmpty();
}
#endif
