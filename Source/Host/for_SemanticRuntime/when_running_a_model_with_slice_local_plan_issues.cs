// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime;

public class when_running_a_model_with_slice_local_plan_issues : Specification
{
    SemanticRuntimeAdmission _admission = null!;
    LoadedSemanticModel _loaded = null!;
    SemanticExecutionResult _command = null!;
    SemanticExecutionResult _query = null!;

    void Establish() => _loaded = when_loading_a_model_with_slice_local_plan_issues.Create().Loaded!;

    async Task Because()
    {
        _admission = new(_loaded.Plan);
        var runtime = SemanticRuntimeHosting.Create(_loaded.Plan, Substitute.For<IAppendSemanticFacts>());
        using var lifetime = (IDisposable)runtime;
        var properties = new Dictionary<string, JsonElement>
        {
            ["projectId"] = JsonSerializer.SerializeToElement("first"),
            ["description"] = JsonSerializer.SerializeToElement("North")
        };
        _command = await runtime.Execute(_loaded.Plan.Commands.Values.Single(), properties, new ClaimsPrincipal(), new(DateTimeOffset.UnixEpoch, "subject", "name", "user"), true);
        _query = await runtime.Query(_loaded.Plan.Queries.Values.Single(), SemanticValue.Text("first"), new ClaimsPrincipal());
    }

    [Fact] void should_not_block_the_model() => _admission.Blocking.ShouldBeEmpty();
    [Fact] void should_admit_the_sibling_command() => _admission.Entries.Single(entry => entry.Kind == "command").Status.ShouldEqual("supported");
    [Fact] void should_run_the_sibling_command() => _command.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_report_the_query_as_unsupported() => _admission.Entries.Single(entry => entry.Kind == "query").Status.ShouldEqual("unsupported");
    [Fact] void should_refuse_the_query_at_runtime() => Assert.IsType<SemanticUnsupported>(_query).Capability.ShouldEqual(SemanticExecutionCapability.Query);
    [Fact] void should_keep_the_plan_code_in_the_runtime_refusal() => Assert.IsType<SemanticUnsupported>(_query).Details.ShouldContain("STAGE-PLAN-004:");

    [Fact]
    void should_not_admit_an_empty_required_query_assertion()
    {
        var specification = _loaded.Plan.Specifications.Values.Single(value => value.Name == "LookingUp");
        var empty = specification with { ThenQueries = [.. specification.ThenQueries.Select(query => query with { Results = [] })] };
        SemanticRunAdmission.Check(_loaded.Plan, empty)!.Details.ShouldContain("STAGE-PLAN-004:");
    }
}
