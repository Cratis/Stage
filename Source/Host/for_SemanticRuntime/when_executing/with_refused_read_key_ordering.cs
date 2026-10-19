// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class with_refused_read_key_ordering : Specification
{
    [Theory]
    [InlineData(false, "", SemanticRejectionCategory.Unauthorized, null)]
    [InlineData(false, "target", SemanticRejectionCategory.Unauthorized, null)]
    [InlineData(true, "", SemanticRejectionCategory.Validation, "Id is required.")]
    [InlineData(true, "target", SemanticRejectionCategory.Validation, "Current '*' does not exist.")]
    public async Task should_preserve_authorization_then_property_rules_then_reads_then_requirements(bool authenticated, string id, SemanticRejectionCategory category, string? message)
    {
        var plan = compiled_plan.From("""
            command reads protected
            policy SignedIn
              require authenticated
            module M
              feature F
                slice StateChange Use
                  command Consume
                    id String identifier
                    sourceId String
                    approved Bool
                    authorize SignedIn
                    reads Current by sourceId
                    validate
                      id not empty message "Id is required."
                      require approved == true
                        message "Approval required."
                    produces Taken
                      for id
                      value = Current.value
                  event Taken
                    value String
                  event Stored
                    value String
                  event Removed
                slice StateView State
                  readmodel Current
                    id String key
                    value String
                  projection Current => Current
                    from Stored
                      id = $eventSourceId
                      value = value
                    remove with Removed
            """);
        var log = new a_semantic_fact_log();
        using var runtime = log.Runtime(plan, SemanticWorld.Empty);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([], authenticated ? "spec" : null));
        var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(new { id, sourceId = "*", approved = false }))!;
        foreach (var validateOnly in new[] { false, true })
        {
            var rejected = Assert.IsType<SemanticRejected>(await runtime.Execute(plan.Commands.Values.Single(), payload, principal, new(DateTimeOffset.UnixEpoch, "owner", "Owner", "owner"), validateOnly));
            rejected.Category.ShouldEqual(category);
            if (message is not null) rejected.Details.ShouldEqual(message);
            rejected.World.Facts.ShouldBeEmpty();
        }
        log.Attempts.ShouldBeEmpty();
    }
}
