// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.given;

internal static class conditional_routed_specifications
{
    internal static ExecutableSemanticModel Model(bool wrongSource = false)
    {
        var model = routed_specifications.Compile(routed_specifications.Source + """

                  specification RejectingEmptyRoute
                    when Deposit
                      accountId = "acc-1"
                      period = ""
                    then error "Stream id property 'Period' must be nonempty, well-formed Unicode NFC text."
            """);
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var command = slice.Commands.Single();
        var account = command.Properties.Single(property => property.Name == "accountId");
        SemanticProducedEvent Production(string accountId) => command.Produces[0] with
        {
            When = new SemanticComparison(new(account.Id, null), SemanticComparisonOperator.Equal, new(default, SemanticValue.Text(accountId)))
        };
        var changed = slice with
        {
            Commands = [command with { Produces = [Production("acc-1"), Production("skipped")] }],
            Specifications = [.. slice.Specifications.Select(specification => specification with
            {
                GivenEvents = [],
                ThenEvents = [.. specification.ThenEvents.Select(expected => wrongSource
                    ? expected with { EventSource = new(account.Type, SemanticValue.Text("wrong-source")) }
                    : expected)]
            })]
        };

        return ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [changed] }] }]
        });
    }
}
