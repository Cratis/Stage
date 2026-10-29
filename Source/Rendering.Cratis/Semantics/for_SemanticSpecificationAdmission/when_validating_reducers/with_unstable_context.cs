// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_reducers;

public class with_unstable_context : given.a_reducer_specification
{
    [Theory]
    [InlineData("Occurred", "context.Occurred.Day")]
    [InlineData("SequenceNumber", "context.SequenceNumber")]
    [InlineData("Tenant", "context.Tenant == Projects.TypedContexts.TenantId.Default ? 1 : 0")]
    public void should_name_only_the_unstable_member_read(string member, string expression)
    {
        var body = $"return new Total(context.Event.Id, {expression});";
        var requirement = _request.ImplementationRequirements.Single();
        _request = _request with
        {
            ImplementationContents = _request.ImplementationContents.SetItem(requirement.RequirementId, body),
            ImplementationRequirements = [requirement with { ContentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body))).ToLowerInvariant() }]
        };
        Validate();
        var reason = Refusal();
        Assert.Contains($"A reducer body reads context.{member}, which Chronicle's ReadModelScenario cannot supply deterministically.", reason, StringComparison.Ordinal);
        foreach (var other in new[] { "Occurred", "SequenceNumber", "Tenant" }.Where(other => other != member))
        {
            Assert.DoesNotContain($"context.{other}", reason, StringComparison.Ordinal);
        }
    }
}
