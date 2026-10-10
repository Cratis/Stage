// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

public class an_append_comparison : a_routed_plan
{
    protected async Task RunAppend(SemanticVersion version)
    {
        var model = _plan.Model;
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var expected = _specification.ThenEvents[0] with { Route = null };
        var specification = _specification with
        {
            When = null,
            WhenAppended = new(expected.EventContract, expected.Values) { EventSource = new(Text, SemanticValue.Text("acc-1")) },
            ThenEvents = [expected]
        };
        var specifications = new List<SemanticSpecification> { specification };
        if (version == SemanticVersion.V6)
        {
            // An unselected clock fixture makes this a valid v6 model without a reachable reaction.
            var address = SemanticAddress.ForSlice(ApplicationIdentity.Create("Routed"), "Banking", "Deposits", "Deposit");
            specifications.Add(_specification with
            {
                Id = SemanticId.Create(SemanticAddress.ForSpecification(address, "AtReceipt")),
                Name = "AtReceipt",
                GivenClock = "2026-10-01T00:00:00.0000000Z",
                ThenEvents = [expected]
            });
        }
        var application = model.Application with
        {
            EventSources = [],
            Policies = version == SemanticVersion.V7 ? [new("NotGuest", new SemanticNotPolicyCondition(new SemanticRoleCondition("Guest")))] : [],
            Modules = [module with { Features = [feature with { Slices = [slice with { Commands = [_command with { Route = null }], Specifications = [.. specifications] }] }] }]
        };
        var language = LanguageVersion.V2;
        if (version == SemanticVersion.V6) language = LanguageVersion.V6;
        else if (version == SemanticVersion.V7) language = LanguageVersion.V7;
        var plan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(language, version, application)).Plan!;
        _reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        _result = (await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results.Single();
    }
}
