// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_BodyWorkerSupervisor;

public class when_validating_responses : Specification
{
    [Fact]
    async Task should_reject_a_version_one_worker_before_sending_a_request()
    {
        var root = Path.GetFullPath("../../../../..", AppContext.BaseDirectory);
        var cache = Path.Combine(root, ".ai-work", "891", "specs", Guid.NewGuid().ToString("N"));
        var hello = JsonSerializer.Serialize(new BodyWorkerHello("hello", 1, "old-worker"), BodyWorkerMessages.Json);
        var script = $"printf '%s\\n' '{hello}'\nread -r line";
        await using var worker = new BodyWorkerSupervisor(new(string.Empty, cache) { Launch = new("/bin/sh", ["-c", script]), TimeoutMs = 10000 });
        var result = await worker.SendAsync(new("probe", "validate"));
        Assert.StartsWith(BodyDiagnostics.WorkerFailed, result.Evaluation.Diagnostic, StringComparison.Ordinal);
        worker.ProcessId.ShouldBeNull();
    }

    [Theory]
    [InlineData("validate", "satisfied", "empty", true)]
    [InlineData("validate", "unsatisfied", "messages", true)]
    [InlineData("validate", "unsatisfied", "empty", false)]
    [InlineData("validate", "state", "state", false)]
    [InlineData("validate", "delete", "empty", false)]
    [InlineData("reduce", "state", "state", true)]
    [InlineData("reduce", "state", "empty", false)]
    [InlineData("reduce", "delete", "empty", true)]
    [InlineData("reduce", "satisfied", "empty", false)]
    [InlineData("evaluate", "state", "state", false)]
    [InlineData("validate", "refused", "empty", true)]
    [InlineData("reduce", "faulted", "empty", true)]
    [InlineData("handle", "state", "empty", false)]
    [InlineData("handle", "produced", "empty", false)]
    [InlineData("handle", "produced", "events", true)]
    [InlineData("handle", "produced", "state", false)]
    [InlineData("handle", "produced", "composite-without-value", false)]
    [InlineData("handle", "produced", "property-without-value", false)]
    [InlineData("handle", "produced", "bad-nested-kind", false)]
    [InlineData("handle", "produced", "bad-number", false)]
    [InlineData("handle", "produced", "invalid-id", false)]
    [InlineData("handle", "produced", "nonempty-events", true)]
    [InlineData("handle", "produced", "null-text", false)]
    [InlineData("handle", "produced", "null-number", false)]
    [InlineData("handle", "produced", "null-boolean", false)]
    [InlineData("handle", "produced", "null-array", false)]
    [InlineData("handle", "produced", "null-composite", false)]
    [InlineData("handle", "produced", "number-not-string", false)]
    [InlineData("handle", "produced", "null-with-value", false)]
    [InlineData("handle", "produced", "duplicate-field", false)]
    [InlineData("handle", "produced", "unknown-field", false)]
    [InlineData("handle", "produced", "messages", false)]
    [InlineData("handle", "refused", "empty", true)]
    [InlineData("handle", "faulted", "empty", true)]
    async Task should_accept_only_the_outcomes_of_the_requested_kind(string kind, string outcome, string payload, bool valid)
    {
        var root = Path.GetFullPath("../../../../..", AppContext.BaseDirectory);
        var cache = Path.Combine(root, ".ai-work", "891", "specs", Guid.NewGuid().ToString("N"));
        var response = new BodyWorkerResponse("probe") { Evaluation = new(outcome, Payload(payload), payload == "messages" ? ["first"] : []) };
        var hello = JsonSerializer.Serialize(new BodyWorkerHello("hello", BodyWorkerMessages.ProtocolVersion, "fixture"), BodyWorkerMessages.Json);
        var script = $"printf '%s\\n' '{hello}'\nread -r line\nprintf '%s\\n' '{JsonSerializer.Serialize(response, BodyWorkerMessages.Json)}'\nread -r another";
        await using var worker = new BodyWorkerSupervisor(new(string.Empty, cache) { Launch = new("/bin/sh", ["-c", script]), TimeoutMs = 10000 });
        var result = await worker.SendAsync(new("probe", kind));
        if (valid)
        {
            result.Evaluation.Outcome.ShouldEqual(outcome);
            worker.ProcessId.ShouldNotBeNull();
        }
        else
        {
            result.Evaluation.Outcome.ShouldEqual("faulted");
            Assert.StartsWith(BodyDiagnostics.WorkerFailed, result.Evaluation.Diagnostic, StringComparison.Ordinal);
            worker.ProcessId.ShouldBeNull();
        }
    }

    static string PropertyId => SemanticId.Create(SemanticKind.Property, "fixture").ToString();

    static JsonElement? Payload(string payload) => payload switch
    {
        "state" => JsonSerializer.SerializeToElement(1),
        "events" => Wire("array", new[] { Wire("composite", Array.Empty<object>()) }),
        "composite-without-value" => Wire("array", new[] { JsonSerializer.SerializeToElement(new { kind = "composite" }) }),
        "property-without-value" => Wire("array", new[] { Wire("composite", new[] { new { property = PropertyId } }) }),
        "bad-nested-kind" => EventPayload(Wire("mystery", Array.Empty<object>())),
        "bad-number" => EventPayload(Wire("number", "x")),
        "invalid-id" => Wire("array", new[] { Wire("composite", new[] { new { property = "a", value = Wire("text", "valid") } }) }),
        "nonempty-events" => EventPayload(Wire("array", new[] { Wire("text", "valid"), Wire("text", string.Empty), Wire("number", "12.34"), Wire("boolean", true), JsonSerializer.SerializeToElement(new { kind = "null" }) })),
        "null-text" => EventPayload(Wire<string?>("text", null)),
        "null-number" => EventPayload(Wire<string?>("number", null)),
        "null-boolean" => EventPayload(Wire<bool?>("boolean", null)),
        "null-array" => EventPayload(Wire<object?>("array", null)),
        "null-composite" => EventPayload(Wire<object?>("composite", null)),
        "number-not-string" => EventPayload(Wire("number", 12)),
        "null-with-value" => EventPayload(Wire("null", "ignored")),
        "duplicate-field" => EventPayload(DuplicateField()),
        "unknown-field" => EventPayload(JsonSerializer.SerializeToElement(new { kind = "text", value = "valid", extra = 1 })),
        _ => null
    };

    static JsonElement EventPayload(JsonElement value) =>
        Wire("array", new[] { Wire("composite", new[] { new { property = PropertyId, value } }) });

    static JsonElement DuplicateField()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("kind", "text");
            writer.WriteString("value", "first");
            writer.WriteString("value", "last");
            writer.WriteEndObject();
        }
        using var document = JsonDocument.Parse(stream.ToArray());
        return document.RootElement.Clone();
    }

    static JsonElement Wire<T>(string kind, T value) => JsonSerializer.SerializeToElement(new { kind, value });
}
