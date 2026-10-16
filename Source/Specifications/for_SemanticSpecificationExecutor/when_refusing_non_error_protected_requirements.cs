// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_refusing_non_error_protected_requirements : Specification
{
    [Theory]
    [InlineData(SemanticValidationSeverity.Warning, "RequirementAfterPresence", "RequireApproval")]
    [InlineData(SemanticValidationSeverity.Information, "RequirementAfterPresence", "RequireApproval")]
    [InlineData(SemanticValidationSeverity.Warning, "ReactionInvokesProtectedRead", "Consume")]
    [InlineData(SemanticValidationSeverity.Information, "ReactionInvokesProtectedRead", "Consume")]
    [InlineData(SemanticValidationSeverity.Warning, "ReactionInvokesProtectedReadOfMissing", "Consume")]
    [InlineData(SemanticValidationSeverity.Information, "ReactionInvokesProtectedReadOfMissing", "Consume")]
    public async Task should_refuse_before_any_facts_or_rules_are_evaluated(SemanticValidationSeverity severity, string specificationName, string commandName)
    {
        var model = SemanticModelSerializer.Deserialize(ProtectedCommandReadsCorpus.V25.EsmBytes.AsSpan());
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single(slice => slice.Name == "Use");
        var original = slice.Commands.Single(command => command.Name == commandName);
        var changed = original with { Requirements = [original.Requirements.Single() with { Severity = severity }] };
        var commandFeature = Assert.Single(SemanticRunAdmission.CommandFeatures(changed));
        Assert.StartsWith("STAGE-ESM-045:", commandFeature.Details);
        commandFeature.Details.ShouldContain("non-Error requirement");
        model = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(candidate => candidate.Id == slice.Id ? slice with
            {
                Commands = [.. slice.Commands.Select(command => command.Id == changed.Id ? changed : command)]
            } : candidate)] }] }]
        });
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        var specification = plan.Specifications.Values.Single(specification => specification.Name == specificationName);
        var observed = new Dictionary<SemanticId, int>();
        var executor = new SemanticSpecificationExecutor { ObserveWorldFacts = (id, count) => observed.Add(id, count) };
        var result = Assert.Single((await executor.Run(plan, new([specification.Id]), new())).Results);
        result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
        result.Unsupported!.Capability.ShouldEqual(StageExecutionCapability.Command);
        Assert.StartsWith("STAGE-ESM-045:", result.Unsupported.Details);
        result.Unsupported.Details.ShouldContain("non-Error requirement");
        result.Trace.ShouldBeNull();

        // Initial admission refuses before the executor creates a world or seeds even the Given facts.
        observed.ShouldBeEmpty();
    }
}
