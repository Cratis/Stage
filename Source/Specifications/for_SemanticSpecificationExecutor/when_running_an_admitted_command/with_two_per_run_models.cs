// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_two_per_run_models : a_command_only_plan
{
    SemanticSpecificationRunRecord _first = null!;
    SemanticSpecificationRunRecord _second = null!;
    bool _hostProviderPreserved;
    bool _distinctRevisions;
    bool _referenceAgrees;

    async Task Because()
    {
        var hostField = typeof(IAuthorizationEvaluator).Assembly.GetType("Cratis.Arc.Internals", throwOnError: true)!
            .GetField("_serviceProvider", BindingFlags.NonPublic | BindingFlags.Static)!;
        var previous = hostField.GetValue(null);
        await using var host = new ServiceCollection().BuildServiceProvider();
        hostField.SetValue(null, host);
        try
        {
            var original = SemanticExecutionPlan.Compile(_originalModel).Plan!;
            var alternate = ChangedReadModel(_originalModel);
            _distinctRevisions = original.Revision != alternate.Revision;
            var runner = new SemanticSpecificationRunner();
            _referenceAgrees = runner.Run(original, _original.Id).Passed && runner.Run(alternate, _original.Id).Passed;
            var executor = new SemanticSpecificationExecutor();
            _first = Assert.Single((await executor.Run(original, new([_original.Id]), new())).Results);
            _second = Assert.Single((await executor.Run(alternate, new([_original.Id]), new())).Results);
            _hostProviderPreserved = ReferenceEquals(host, hostField.GetValue(null));
        }
        finally
        {
            hostField.SetValue(null, previous);
        }
    }

    [Fact] void should_keep_the_host_service_provider_after_both_runs() => _hostProviderPreserved.ShouldBeTrue();
    [Fact] void should_use_distinct_model_revisions() => _distinctRevisions.ShouldBeTrue();
    [Fact] void should_project_each_model_without_leaking_an_artifact() =>
        Assert.True(_first.Outcome == SemanticSpecificationOutcome.Passed && _second.Outcome == SemanticSpecificationOutcome.Passed &&
            _referenceAgrees && _first.Trace!.ReadModels.Count > 0 && _second.Trace!.ReadModels.Count > 0,
            $"First: {string.Join("; ", _first.Failures)}; second: {string.Join("; ", _second.Failures)}");

    static SemanticExecutionPlan ChangedReadModel(ExecutableSemanticModel original)
    {
        var application = original.Application;
        var changed = application with
        {
            Modules = [.. application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        ReadModels = [.. slice.ReadModels.Select(model => model with { Name = model.Name + "InSecondRun" })]
                    })]
                })]
            })]
        };
        return SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(original.LanguageVersion, original.SemanticVersion, changed)).Plan!;
    }
}
