// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_described_command_handlers : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticSpecificationRunReport _report = null!;
    readonly Dictionary<SemanticId, int> _facts = [];

    void Establish() => _plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(DescribedCommandHandlersCorpus.V23.EsmBytes.AsSpan()), []).Plan!;
    async Task Because() => _report = await new SemanticSpecificationExecutor { ObserveWorldFacts = (id, count) => _facts[id] = count }.Run(_plan, new([]), new());

    [Fact] void should_admit_the_model_and_run_every_specification() => _report.Results.Count.ShouldEqual(7);
    [Fact] void should_pass_denied_invalid_history_and_refused_invocations() => _report.Results.Where(result => result.Name is not ("Body" or "InvokedBody")).ShouldEachConformTo(result => result.Outcome == SemanticSpecificationOutcome.Passed);
    [Fact] void should_refuse_only_the_reached_bodies() => _report.Results.Where(result => result.Name is "Body" or "InvokedBody").ShouldEachConformTo(result => result.Outcome == SemanticSpecificationOutcome.Unsupported && result.Unsupported!.Capability.ToString() == "ImplementationBody");
    [Fact] void should_name_the_command_and_requirement_at_the_body() => _report.Results.Where(result => result.Name is "Body" or "InvokedBody").ShouldEachConformTo(result => result.Unsupported!.Details.StartsWith("STAGE-IMPL-002:", StringComparison.Ordinal) && result.Unsupported.Details.Contains("ApplyLateFee", StringComparison.Ordinal) && result.Unsupported.Details.Contains(_plan.Commands.Values.Single(command => command.Handler is not null).Handler!.RequirementId, StringComparison.Ordinal));
    [Fact] void should_append_no_handler_facts() => _facts[_plan.Specifications.Values.Single(specification => specification.Name == "Body").Id].ShouldEqual(0);
    [Fact] void should_retain_only_the_invocation_trigger() => _facts[_plan.Specifications.Values.Single(specification => specification.Name == "InvokedBody").Id].ShouldEqual(1);
    [Fact] void should_run_the_unrelated_no_op_over_history() => _report.Results.Single(result => result.Name == "History").Trace!.Facts.ShouldBeEmpty();

    [Theory]
    [InlineData("Denied")]
    [InlineData("Invalid")]
    [InlineData("Body")]
    public async Task should_not_require_a_production_destination_before_the_handler(string name)
    {
        var model = _plan.Model;
        var application = model.Application with
        {
            Modules = [.. model.Application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        Commands = [.. slice.Commands.Select(command => command.Handler is null ? command : command with
                        {
                            Destination = null,
                            Produces = [.. command.Produces.Select(produced => produced with { Destination = null })]
                        })]
                    })]
                })]
            })]
        };
        var plan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application), []).Plan!;
        var specification = plan.Specifications.Values.Single(specification => specification.Name == name);
        var result = (await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results.Single();
        result.Outcome.ShouldEqual(name == "Body" ? SemanticSpecificationOutcome.Unsupported : SemanticSpecificationOutcome.Passed);
        if (name == "Body") result.Unsupported!.Capability.ToString().ShouldEqual("ImplementationBody");
    }
}
