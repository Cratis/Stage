// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_checking_publication;

public class with_a_scoped_plan_with_a_domain : given.a_policy_plan
{
    void Establish()
    {
        _plan = CratisRendering.PlanFrom(_loaded, new([PlanSelectionEntry.Slice("Billing", "Invoicing", "Issue")]), new(_loaded.Model.Application.Name, "InvoiceApp", "Invoices") { Domain = "Sales/Retail" }).Plan!;
        _existing["GeneratedPolicies/Policies.cs"] = LegacyPolicies;
    }

    void Because() => _check = CratisRendering.CheckPublication(_plan, Read);
    [Fact] void should_check_the_application_root_policy_paths() => _readPaths.ShouldContainOnly("GeneratedPolicies/Policies.cs", "GeneratedPolicies/PolicyBodies.cs", "TypedContexts/PolicyContext.cs");
    [Fact] void should_name_the_application_root_aggregate() => ((CratisPublicationCheck.RequiresApplicationScope)_check).Paths.ShouldContainOnly("GeneratedPolicies/Policies.cs");
}
#endif
