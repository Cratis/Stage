// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.given;

internal static class routed_specifications
{
    internal const string Source = """
        eventsource Account
          id "stored-account"
          identifier String
          stream Transactions
            id "stored-transactions"
            streamId String
        module Banking
          feature Deposits
            slice StateChange Deposit
              command Deposit
                accountId String identifier
                period String
                stream Account.Transactions
                  streamId = period
                produces Deposited
                  for accountId
              event Deposited
              event Historical
              specification Depositing
                given Historical
                  for "history"
                  stream Account.Transactions
                    streamId = "previous"
                when Deposit
                  accountId = "acc-1"
                  period = "2026-10"
                then Deposited
                  stream Account.Transactions
                    streamId = "2026-10"
        """;

    internal static ExecutableSemanticModel Compile(string source = Source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Routed"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("routes"), "routes", "routes.play", source);
        var compilation = new SemanticModelCompiler().Compile("Routed", SemanticDocumentSet.Create([document], catalog));
        Xunit.Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        return compilation.Value!.Model;
    }

    internal static ArtifactRenderPlan Plan(ExecutableSemanticModel model) => CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Routed", "Routed"));

    internal static string Code(ArtifactRenderPlan plan) => System.Text.Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("when_depositing.cs", StringComparison.Ordinal)).Bytes.AsSpan());

    internal static ExecutableSemanticModel WithUnroutedExpectation(ExecutableSemanticModel model, string name = "Depositing")
    {
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var changed = slice with { Specifications = [.. slice.Specifications.Select(specification => specification.Name == name
            ? specification with { ThenEvents = [specification.ThenEvents[0] with { Route = null, Unrouted = true }] }
            : specification)] };
        return ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [changed] }] }]
        });
    }

    internal static ExecutableSemanticModel Composite(bool wrongPart = false)
    {
        var model = Compile("concept Month : Int\n" + Source);
        var source = model.Application.EventSources[0];
        var stream = source.Streams[0] with
        {
            StreamIdType = null,
            StreamIdParts = [new("account", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Uuid)), new("month", SemanticTypeReference.ForConcept(model.Application.Concepts.Single(concept => concept.Name == "Month").Id)), new("label", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text))]
        };
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var command = slice.Commands.Single();
        var specification = slice.Specifications.Single();
        SemanticFixtureRoutePart[] parts = [new("account", SemanticValue.Text("3FA85F64-5717-4562-B3FC-2C963F66AFA6")), new("month", SemanticValue.Number(10)), new("label", SemanticValue.Text("a|b%"))];
        var expected = specification.ThenEvents[0];
        var fixtureRoute = expected.Route! with { StreamId = null, StreamIdParts = [.. parts] };
        var commandRoute = command.Route! with { StreamId = null, StreamIdParts = [.. parts.Select(part => new SemanticCommandRoutePart(part.Part, SemanticExpression.FromValue(part.Value)))] };
        var changed = slice with
        {
            Commands = [command with { Route = commandRoute }],
            Specifications = [specification with
            {
                GivenEvents = [specification.GivenEvents[0] with { Route = fixtureRoute }],
                ThenEvents = [expected with { Route = wrongPart ? fixtureRoute with { StreamIdParts = [.. parts.Select(part => part.Part == "month" ? part with { Value = SemanticValue.Number(11) } : part)] } : fixtureRoute }]
            }]
        };
        return ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            EventSources = [source with { Streams = [stream] }],
            Modules = [module with { Features = [feature with { Slices = [changed] }] }]
        });
    }

    internal static ExecutableSemanticModel SingleAnyOrder(bool legacy = false)
    {
        var model = Compile();
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var command = slice.Commands.Single();
        var specification = slice.Specifications.Single();
        var changed = slice with
        {
            Commands = [command with { Route = legacy ? null : command.Route }],
            Specifications = [specification with
            {
                GivenEvents = [],
                When = specification.When! with { EventSource = new(command.Properties[0].Type, SemanticValue.Text("acc-1")) },
                ThenEventsInAnyOrder = true,
                ThenEvents = [specification.ThenEvents[0] with
                {
                    EventSource = new(command.Properties[0].Type, SemanticValue.Text("acc-2")),
                    Route = legacy ? null : specification.ThenEvents[0].Route
                }]
            }]
        };
        var application = model.Application with
        {
            EventSources = legacy ? [] : model.Application.EventSources,
            Policies = legacy ? [new("NotGuest", new SemanticNotPolicyCondition(new SemanticRoleCondition("Guest")))] : model.Application.Policies,
            Modules = [module with { Features = [feature with { Slices = [changed] }] }]
        };

        return ExecutableSemanticModel.Create(legacy ? LanguageVersion.V7 : LanguageVersion.V8, legacy ? SemanticVersion.V7 : SemanticVersion.V8, application);
    }

    internal static ExecutableSemanticModel Assignment(bool legacy = false)
    {
        var model = Compile();
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var command = slice.Commands.Single();
        var other = command.Properties[1] with { IsIdentifier = true };
        var specification = slice.Specifications.Single();
        var wildcard = specification.ThenEvents[0] with { Route = null };
        var exact = wildcard with { EventSource = new(command.Properties[0].Type, SemanticValue.Text("acc-1")), Route = legacy ? null : specification.ThenEvents[0].Route };
        var changed = slice with
        {
            Commands = [command with
            {
                Route = legacy ? null : command.Route,
                Properties = [command.Properties[0], other],
                Produces = [command.Produces[0], command.Produces[0] with { Destination = new SemanticResolvedExpression(SemanticExpressionRootKind.Command, SemanticExpressionSourceKind.Property, other.Id) }]
            }],
            Specifications = [specification with { GivenEvents = [], ThenEventsInAnyOrder = true, ThenEvents = [wildcard, exact] }]
        };
        var application = model.Application with
        {
            EventSources = legacy ? [] : model.Application.EventSources,
            Policies = legacy ? [new("NotGuest", new SemanticNotPolicyCondition(new SemanticRoleCondition("Guest")))] : model.Application.Policies,
            Modules = [module with { Features = [feature with { Slices = [changed] }] }]
        };
        return ExecutableSemanticModel.Create(legacy ? LanguageVersion.V7 : LanguageVersion.V8, legacy ? SemanticVersion.V7 : SemanticVersion.V8, application);
    }
}
