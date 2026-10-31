// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticImplementationReach;

public class when_a_rule_has_worker_admission : Specification
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    async Task should_admit_named_rules_without_starting_a_worker(bool concept, bool generated)
    {
        var loaded = await Load(concept, generated);
        var entry = Entry(loaded, true);
        var bodies = SemanticBodyExecution.From(loaded, new(loaded.Plan.Revision.ToString(), generated ? [] : [entry]), () => throw new InvalidOperationException("Admission must not start a worker."));
        SemanticImplementationReach.ForCommand(loaded.Plan, loaded.Plan.Commands.Values.Single(), bodies).ShouldBeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    async Task should_preserve_a_render_refusal_as_a_non_blocking_body_refusal(bool concept)
    {
        var loaded = await Load(concept, false);
        var entry = Entry(loaded, false) with { DiagnosticCode = "STAGE-ESM-022", DiagnosticMessage = "STAGE-ESM-022: Pure validation was refused." };
        var bodies = SemanticBodyExecution.From(loaded, new(loaded.Plan.Revision.ToString(), [entry]), () => Substitute.For<IBodyWorkerClient>());
        var refusal = SemanticImplementationReach.ForCommand(loaded.Plan, loaded.Plan.Commands.Values.Single(), bodies);
        refusal.Details.ShouldEqual(entry.DiagnosticMessage);
        SemanticImplementationReach.IsBodyRefusal(refusal.Details, bodies).ShouldBeTrue();
    }

    [Fact]
    async Task should_refuse_a_named_rule_on_a_generated_property_before_admission()
    {
        var failure = await Assert.ThrowsAsync<InvalidSemanticModel>(() => Load(true, true, true));
        failure.Message.Contains("generated values exist only after validation", StringComparison.Ordinal).ShouldBeTrue();
    }

    [Fact]
    async Task should_keep_the_original_refusal_when_no_worker_is_supplied()
    {
        var loaded = await Load(false, false);
        var refusal = SemanticImplementationReach.ForCommand(loaded.Plan, loaded.Plan.Commands.Values.Single());
        refusal.Details.StartsWith("STAGE-IMPL-003:", StringComparison.Ordinal).ShouldBeTrue();
    }

    static BodyUnitEntry Entry(LoadedSemanticModel loaded, bool admitted)
    {
        var requirement = loaded.ImplementationRequirements.Single();
        return new(requirement.RequirementId, requirement.Role.ToString(), admitted);
    }

    static async Task<LoadedSemanticModel> Load(bool concept, bool generated, bool commandRule = false)
    {
        var primitive = generated ? "Uuid" : "Decimal";
        var conceptValidation = commandRule ? string.Empty : "\n  validate\n    rule Positive\n      ```csharp\n      return true;\n      ```";
        var declaration = concept ? $"concept Amount : {primitive}{conceptValidation}\n" : string.Empty;
        var validation = concept && !commandRule ? string.Empty : "\n        validate\n          amount rule Positive\n            ```csharp\n            return context.Value > 0;\n            ```";
        var source = $$"""
            {{declaration}}module Orders
              feature Ordering
                slice StateChange PlaceOrder
                  command PlaceOrder
                    orderId Uuid identifier
                    amount {{(concept ? "Amount" : "Decimal")}}{{(generated ? " generated" : string.Empty)}}{{validation}}
                    produces OrderPlaced
                      for orderId
                      amount = amount
                  event OrderPlaced
                    amount {{(concept ? "Amount" : "Decimal")}}
            """;
        var root = Path.GetFullPath("../../../../..", AppContext.BaseDirectory);
        var directory = Path.Combine(root, ".ai-work", "891", "specs", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "model.play");
        await File.WriteAllTextAsync(path, source);
        return await SemanticModelLoader.LoadFromPathAsync(path);
    }
}
