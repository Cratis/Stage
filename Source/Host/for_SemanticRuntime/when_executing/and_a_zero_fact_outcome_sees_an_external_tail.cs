// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class and_a_zero_fact_outcome_sees_an_external_tail : a_semantic_runtime
{
    SemanticExecutionResult _result = null!;
    IAppendSemanticFacts _emptyAppender = null!;
    ISemanticRuntime _zeroFactRuntime = null!;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var source = Source.Replace(
            """
                    produces ProjectRegistered
                      for projectId
                      projectId = projectId
                      name = name
                      registeredAt = $context.occurred
                      registeredBy = $context.causedBy.subject
            """,
            string.Empty,
            StringComparison.Ordinal);
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("project-model"), "project-model", "RegisterProject.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        _emptyAppender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        var reads = 0;
        ((ISemanticFactTail)_emptyAppender).Tail().Returns(_ => ++reads == 1 ? ulong.MaxValue : 0UL);
        _zeroFactRuntime = SemanticRuntimeHosting.Create(plan, _emptyAppender);
    }

    async Task Because() => _result = await _zeroFactRuntime.Execute(
        _zeroFactRuntime.Plan.Commands.Values.Single(),
        new Dictionary<string, JsonElement>
        {
            ["projectId"] = JsonSerializer.SerializeToElement("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
            ["name"] = JsonSerializer.SerializeToElement("Screenplay")
        },
        new ClaimsPrincipal(),
        new(DateTimeOffset.UtcNow, "owner", "Owner", "owner"),
        false);

    [Fact] void should_fault_instead_of_reporting_the_stale_world_as_current() => _result.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_report_the_external_writer() => ((ISemanticRuntimeStatus)_zeroFactRuntime).FaultReason.ShouldContain("outside this session");
    [Fact] async Task should_not_append() => await _emptyAppender.DidNotReceive().Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>(), Arg.Any<ulong>());
}
