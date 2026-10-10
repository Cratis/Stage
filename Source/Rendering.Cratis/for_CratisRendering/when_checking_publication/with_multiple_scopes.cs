// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_checking_publication;

public class with_multiple_scopes : given.a_policy_plan
{
    void Establish()
    {
        _plan = CratisRendering.PlanFrom(_loaded, new([PlanSelectionEntry.Slice("Billing", "Invoicing", "Issue"), PlanSelectionEntry.Slice("Billing", "Invoicing", "Lookup")]), new(_loaded.Model.Application.Name, "InvoiceApp", "Invoices")).Plan!;
        _existing["GeneratedPolicies/Policies.cs"] = LegacyPolicies;
    }

    void Because() => _check = CratisRendering.CheckPublication(_plan, Read);
    [Fact] void should_not_treat_a_union_of_all_slices_as_application_scope() => _check.ShouldBeOfExactType<CratisPublicationCheck.RequiresApplicationScope>();
    [Fact] void should_retain_the_primary_scope() => _plan.Scope.Kind.ShouldEqual(ArtifactRenderScopeKind.Slice);
    [Fact] void should_retain_the_additional_scope() => _plan.AdditionalScopes.Length.ShouldEqual(1);
    [Fact] void should_keep_selection_metadata_out_of_the_output_digest() => ArtifactRenderPlan.Create(new(_loaded.Model, _loaded.Plan, CratisRendering.CreateProfile(_loaded.Model.Application.Name, new("InvoiceApp", "Invoices")), new(ArtifactRenderScopeKind.Application, _loaded.Model.Application.Id)), _plan.Artifacts, _plan.Diagnostics).Digest.ShouldEqual(_plan.Digest);
}
#endif
