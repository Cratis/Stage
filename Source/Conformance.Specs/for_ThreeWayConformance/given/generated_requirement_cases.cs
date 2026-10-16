// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;

internal static class generated_requirement_cases
{
    internal static ExecutableSemanticModel Compile() => invoice_model.Compile(Source);

    internal static ExecutableSemanticModel WithGeneratedRead()
    {
        var model = Compile();
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices[0];
        var command = slice.Commands.Single();
        var generated = command.Properties.Single(property => property.IsGenerated);
        var requirement = command.Requirements.Single() with
        {
            Condition = new SemanticComparison(new(generated.Id, null), SemanticComparisonOperator.Equal, new(generated.Id, null))
        };
        var application = model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [slice with { Commands = [command with { Requirements = [requirement] }] }] }] }]
        };

        // Fault injection: Screenplay's typed-model validator also rejects generated reads.
        typeof(ExecutableSemanticModel).GetField("<Application>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, application);
        return model;
    }

    const string Source = """
        concept Receipt : Uuid
        concept InviteCode : String
          generate nanoid 64 alphabet "ab"
          validate
            matches "^(?!a{63}b$)[ab]{64}$"
        module Orders
          feature Registration
            slice StateChange Register
              command RegisterOrder
                id String identifier
                receipt Receipt generated
                quantity Int
                validate
                  quantity max 10
                  require quantity > 0
                    message "Quantity must be positive"
                produces event OrderRegistered
                  for id
                  receipt Receipt = receipt
                  quantity Int = quantity
              specification RequirementMet
                when RegisterOrder
                  id = "one"
                  generated receipt = "11111111-1111-4111-8111-111111111111"
                  quantity = 1
                then OrderRegistered
                  for "one"
                  receipt = "11111111-1111-4111-8111-111111111111"
                  quantity = 1
              specification RequirementUnmet
                when RegisterOrder
                  id = "one"
                  quantity = 0
                then error "Quantity must be positive"
            slice StateChange Invite
              command InviteCustomer
                id String identifier
                code InviteCode generated
                quantity Int
                validate
                  require quantity > 0
                    message "Quantity must be positive"
                produces event CustomerInvited
                  for id
                  code InviteCode = code
              specification PinnedConceptRule
                when InviteCustomer
                  id = "one"
                  generated code = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
                  quantity = 1
                then CustomerInvited
                  for "one"
                  code = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
        """;
}
#endif
