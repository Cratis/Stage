// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticReadModelBrowser.given;
using Cratis.Stage.Semantics;
using NSubstitute;
using NSubstitute.Extensions;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticReadModelBrowser.when_browsing;

public class with_a_fault_while_waiting_for_the_runtime : a_browser
{
    readonly TaskCompletionSource _appendEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _appendOutcome = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _listStatus;
    int _detailStatus;
    JsonElement _list;
    JsonElement _detail;

    async Task Establish()
    {
        await Register();
        _facts.Configure().Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>()).Returns(_ =>
        {
            _appendEntered.TrySetResult();
            return _appendOutcome.Task;
        });
        BuildRouting();
    }

    async Task Because()
    {
        var command = _client.PostAsync(CommandRoute, new StringContent(Payload.Replace("Screenplay", "Another", StringComparison.Ordinal), Encoding.UTF8, "application/json"));
        try
        {
            await _appendEntered.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);

            // In-process HTTP dispatch runs synchronously up to ReadModels' gate wait. The blocked append
            // holds that gate, so both requests have passed their initial fault check before we fault it.
            var list = Request("GET", SemanticReadModelBrowser.Route);
            var detail = Request("GET", $"{SemanticReadModelBrowser.Route}/{ModelId}");
            list.IsCompleted.ShouldBeFalse();
            detail.IsCompleted.ShouldBeFalse();
            _appendOutcome.SetException(new SemanticCommandExecutionFailed("Acknowledgment lost."));
            var listResponse = await list.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
            var detailResponse = await detail.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
            _listStatus = listResponse.Status;
            _detailStatus = detailResponse.Status;
            using var listDocument = JsonDocument.Parse(listResponse.Body);
            using var detailDocument = JsonDocument.Parse(detailResponse.Body);
            _list = listDocument.RootElement.Clone();
            _detail = detailDocument.RootElement.Clone();
        }
        finally
        {
            _appendOutcome.TrySetException(new SemanticCommandExecutionFailed("Acknowledgment lost."));
            using var response = await command.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        }
    }

    [Fact] void should_refuse_the_list() => _listStatus.ShouldEqual(503);
    [Fact] void should_refuse_the_detail() => _detailStatus.ShouldEqual(503);
    [Fact] void should_report_the_fault_in_the_list() => _list.GetProperty("state").GetString().ShouldEqual("faulted");
    [Fact] void should_report_the_fault_in_the_detail() => _detail.GetProperty("state").GetString().ShouldEqual("faulted");
    [Fact] void should_include_the_list_fault_reason() => _list.GetProperty("reason").GetString().ShouldEqual(((ISemanticRuntimeStatus)_semanticRuntime!).FaultReason);
    [Fact] void should_include_the_detail_fault_reason() => _detail.GetProperty("reason").GetString().ShouldEqual(((ISemanticRuntimeStatus)_semanticRuntime!).FaultReason);
    [Fact] void should_not_return_counts() => _list.EnumerateObject().Select(property => property.Name).ShouldContainOnly("state", "reason");
    [Fact] void should_not_return_rows() => _detail.EnumerateObject().Select(property => property.Name).ShouldContainOnly("state", "reason");
}
