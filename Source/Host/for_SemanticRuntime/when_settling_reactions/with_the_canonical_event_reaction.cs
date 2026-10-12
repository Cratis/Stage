// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_settling_reactions;

public class with_the_canonical_event_reaction : a_reaction_runtime
{
    protected override SemanticExecutionPlan Compile()
    {
        var model = SemanticModelSerializer.Deserialize(ReactionsCorpus.V6.EsmBytes.AsSpan());

        // The complete vector intentionally includes unsupported schedules, captures and application triggers.
        // Keep its original event reaction and commands, removing only those non-live behaviors.
        var application = model.Application with
        {
            Triggers = [],
            Modules = [.. model.Application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Where(slice => slice.Kind != SemanticSliceKind.Translate).Select(slice => slice with
                    {
                        Reactions = [.. slice.Reactions.Where(reaction => reaction.Triggers.All(trigger => trigger.Kind == SemanticReactionTriggerKind.Event))],
                        Specifications = [.. slice.Specifications.Where(specification => specification.WhenTrigger is null && specification.WhenClock is null)]
                    })]
                })]
            })]
        };

        return SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application)).Plan!;
    }

    async Task Because() => _result = await _runtime.Execute(
        _plan.Commands.Values.Single(command => command.Name == "SendInvoice"),
        new Dictionary<string, JsonElement> { ["invoiceId"] = JsonSerializer.SerializeToElement("9c858901-8a57-4791-81fe-4c455b099bc9") },
        new ClaimsPrincipal(),
        new(new DateTimeOffset(2026, 10, 2, 9, 0, 0, TimeSpan.Zero), "subject", "name", "user"),
        false);

    [Fact] void should_admit_the_event_only_corpus() => new SemanticRuntimeAdmission(_plan).Blocking.ShouldBeEmpty();
    [Fact] void should_accept_the_command() => _result.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_append_the_canonical_follow_up_in_one_batch() => FactNames(_batches.Single()).ToArray().ShouldEqual(["InvoiceSent", "ReminderScheduled"]);
    [Fact] void should_use_the_command_occurrence_in_the_reaction() => _batches.Single()[1].Values.Single().Value.ShouldEqual(SemanticValue.Text("2026-10-02T09:00:00.0000000Z"));
}
