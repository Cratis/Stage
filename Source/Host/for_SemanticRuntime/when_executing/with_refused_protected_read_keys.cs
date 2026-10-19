// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class with_refused_protected_read_keys : Specification
{
    SemanticExecutionPlan _plan = null!;
    a_semantic_fact_log _log = null!;
    SemanticRuntime _runtime = null!;

    void Establish()
    {
        _plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(ProtectedCommandReadsCorpus.V25.EsmBytes.AsSpan())).Plan!;
        _log = new();
        _runtime = _log.Runtime(_plan, SemanticWorld.Empty);
    }

    void Destroy() => _runtime.Dispose();

    [Theory]
    [InlineData("RefusedEmptyKey", "", false)]
    [InlineData("RefusedWhitespaceKey", "   ", false)]
    [InlineData("RefusedStarKey", "*", false)]
    [InlineData("RefusedHashKey", "a#b", false)]
    [InlineData("RefusedPaddedKey", " padded ", false)]
    [InlineData("RefusedStarKeyWithStoredInstance", "*", false)]
    [InlineData("RefusedEmptyKey", "", true)]
    [InlineData("RefusedWhitespaceKey", "   ", true)]
    [InlineData("RefusedStarKey", "*", true)]
    [InlineData("RefusedHashKey", "a#b", true)]
    [InlineData("RefusedPaddedKey", " padded ", true)]
    [InlineData("RefusedStarKeyWithStoredInstance", "*", true)]
    public async Task should_reject_each_published_key_before_lookup(string name, string key, bool validateOnly)
    {
        var specification = _plan.Specifications.Values.Single(specification => specification.Name == name);
        foreach (var given in specification.GivenEvents)
        {
            var source = Assert.IsType<SemanticTextValue>(given.EventSource!.Value).Value;
            var value = Assert.IsType<SemanticTextValue>(given.Values.Single().Value).Value;
            Assert.IsType<SemanticAccepted>(await Execute("Store", new { id = source, value }));
        }
        var command = _plan.Commands[specification.When!.Command];
        var sourceId = command.Properties.Single(property => property.Name == "sourceId").Id;
        specification.When.Values.Single(value => value.TargetProperty == sourceId).Value.ShouldEqual(SemanticValue.Text(key));
        var attempts = _log.Attempts.Count;
        var rejected = Assert.IsType<SemanticRejected>(await Execute(command.Name, new { id = "target", sourceId = key }, validateOnly));
        rejected.Category.ShouldEqual(SemanticRejectionCategory.Validation);
        rejected.Details.ShouldEqual($"Current '{key}' does not exist.");
        rejected.Details.ShouldEqual(specification.ThenErrors.Single().Message);
        rejected.World.Facts.Length.ShouldEqual(specification.GivenEvents.Length);
        _log.Facts.Count.ShouldEqual(specification.GivenEvents.Length);
        _log.Attempts.Count.ShouldEqual(attempts);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_preserve_valid_keys_containing_a_non_sentinel_star(bool validateOnly)
    {
        Assert.IsType<SemanticAccepted>(await Execute("Store", new { id = "a*b", value = "present" }));
        var accepted = Assert.IsType<SemanticAccepted>(await Execute("Consume", new { id = "target", sourceId = "a*b" }, validateOnly));
        accepted.Facts.Single().Values.Single().Value.ShouldEqual(SemanticValue.Text("present"));
        _log.Writes.ShouldEqual(validateOnly ? 1 : 2);
    }

    Task<SemanticExecutionResult> Execute(string name, object payload, bool validateOnly = false)
    {
        var command = _plan.Commands.Values.Single(command => command.Name == name);
        var json = JsonSerializer.SerializeToElement(payload);
        var values = command.Properties.Where(property => !property.IsGenerated && json.TryGetProperty(property.Name, out _))
            .ToDictionary(property => property.Name, property => json.GetProperty(property.Name));

        return _runtime.Execute(command, values, new ClaimsPrincipal(), new(DateTimeOffset.UnixEpoch, "owner", "Owner", "owner"), validateOnly);
    }
}
