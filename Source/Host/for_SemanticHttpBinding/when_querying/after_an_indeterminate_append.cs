// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticHttpBinding.given;
using Cratis.Stage.Semantics;
using Microsoft.AspNetCore.Builder;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHttpBinding.when_querying;

public class after_an_indeterminate_append : a_bound_semantic_model
{
    int _keyedStatus;
    int _compatibilityStatus;
    string _body = null!;
    string _statusBody = null!;

    void Establish()
    {
        var tail = ulong.MaxValue;
        ((ISemanticFactTail)_facts).Tail().Returns(_ => tail);
        _facts.Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>(), Arg.Any<ulong>())
            .Returns(_ =>
            {
                tail = 0;
                return Task.FromException(new SemanticCommandExecutionFailed("Acknowledgment lost."));
            });
    }

    async Task Because()
    {
        _app.MapGet("/stage/status", () => SemanticHost.Status(_semanticRuntime!, "Projects", "/tmp/smoke23"));
        BuildRouting();
        await Request("POST", Route, Payload);
        (_keyedStatus, _body, _) = await Request("GET", "/api/projects/registration/public-lookup/public-by-id?projectId=3fa85f64-5717-4562-b3fc-2c963f66afa6");
        (_compatibilityStatus, _, _) = await Request("GET", "/api/projects/registration/public-lookup/all-public-summaries");
        (_, _statusBody, _) = await Request("GET", "/stage/status");
    }

    [Fact] void should_rewrite_the_keyed_query_as_unsupported() => _keyedStatus.ShouldEqual(501);
    [Fact] void should_rewrite_compatibility_as_unsupported() => _compatibilityStatus.ShouldEqual(501);
    [Fact] void should_report_the_world_capability() => _body.ShouldContain("Unsupported(World)");
    [Fact] void should_report_a_faulted_status() => _statusBody.ShouldContain("unsupported");
    [Fact] void should_report_the_fault_reason() => _statusBody.ShouldContain("Acknowledgment lost");
}
