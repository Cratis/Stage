// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_querying;

public class with_query_filters : Specification
{
    ISemanticRuntime _runtime = null!;
    SemanticKeyedQuery _query = null!;
    SemanticExecutionResult _result = null!;
    SemanticExecutionResult _omitted = null!;
    SemanticExecutionResult _missing = null!;

    async Task Establish()
    {
        var plan = compiled_plan.From("""
            module Projects
              feature Registration
                slice StateChange Register
                  command RegisterProject
                    projectId String identifier
                    name String
                    produces ProjectRegistered
                      for projectId
                      projectId = projectId
                      name = name
                  event ProjectRegistered
                    projectId String
                    name String
                slice StateView List
                  readmodel ProjectSummary
                    projectId String
                    name String
                  query FilteredProjects => ProjectSummary[]
                    filter name String
                    filter projectId String optional
                  projection ProjectSummaryProjection => ProjectSummary
                    from ProjectRegistered key projectId
                      projectId = projectId
                      name = name
            """);
        var appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        var facts = new List<SemanticFact>();
        ((ISemanticFactTail)appender).Tail().Returns(_ => facts.Count == 0 ? ulong.MaxValue : (ulong)facts.Count - 1);
        appender.Append(Arg.Do<IReadOnlyList<SemanticFact>>(facts.AddRange), Arg.Any<SemanticCommandOccurrence>()).Returns(Task.CompletedTask);
        _runtime = SemanticRuntimeHosting.Create(plan, appender);
        _query = plan.Queries.Values.Single();
        foreach (var (id, name) in new[] { ("one", "Shared"), ("two", "Shared"), ("three", "Other") })
        {
            var outcome = await _runtime.Execute(plan.Commands.Values.Single(), new Dictionary<string, JsonElement>
            {
                ["projectId"] = JsonSerializer.SerializeToElement(id),
                ["name"] = JsonSerializer.SerializeToElement(name)
            }, new ClaimsPrincipal(), new(DateTimeOffset.UtcNow, "subject", "name", "user"), false);
            outcome.ShouldBeOfExactType<SemanticAccepted>();
        }
    }

    async Task Because()
    {
        ImmutableArray<SemanticQueryFilterValue> filters = [new(_query.Filters[0].Id, SemanticValue.Text("Shared")), new(_query.Filters[1].Id, SemanticValue.Text("two"))];
        _result = await _runtime.Query(_query, SemanticValue.Null, new ClaimsPrincipal(), filters);
        _omitted = await _runtime.Query(_query, SemanticValue.Null, new ClaimsPrincipal(), [filters[0]]);
        _missing = await _runtime.Query(_query, SemanticValue.Null, new ClaimsPrincipal());
    }

    [Fact] void should_combine_filter_values() => ((SemanticAccepted)_result).Queries.Single().Results.Select(result => result.Key).ShouldContainOnly(SemanticValue.Text("two"));
    [Fact] void should_preserve_omitted_optional_filters() => ((SemanticAccepted)_omitted).Queries.Single().Results.Length.ShouldEqual(2);
    [Fact] void should_reject_a_missing_required_filter() => ((SemanticRejected)_missing).Category.ShouldEqual(SemanticRejectionCategory.Contract);
}
