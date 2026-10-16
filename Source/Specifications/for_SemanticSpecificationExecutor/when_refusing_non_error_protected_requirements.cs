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
    [InlineData(SemanticValidationSeverity.Warning)]
    [InlineData(SemanticValidationSeverity.Information)]
    public async Task should_retain_a_precise_refusal_for_host_and_executor_admission(SemanticValidationSeverity severity)
    {
        var model = SemanticModelSerializer.Deserialize(ProtectedCommandReadsCorpus.V25.EsmBytes.AsSpan());
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single(slice => slice.Name == "Use");
        var original = slice.Commands.Single(command => command.Name == "RequireApproval");
        var changed = original with { Requirements = [original.Requirements.Single() with { Severity = severity }] };
        var commandFeature = Assert.Single(SemanticRunAdmission.CommandFeatures(changed));
        Assert.StartsWith("STAGE-ESM-044:", commandFeature.Details);
        commandFeature.Details.ShouldContain("non-Error requirement");
        model = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(candidate => candidate.Id == slice.Id ? slice with
            {
                Commands = [.. slice.Commands.Select(command => command.Id == changed.Id ? changed : command)]
            } : candidate)] }] }]
        });
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        var specification = plan.Specifications.Values.Single(specification => specification.Name == "RequirementAfterPresence");
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results);
        result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
        result.Unsupported!.Details.ShouldContain("non-Error requirement");
    }
}
