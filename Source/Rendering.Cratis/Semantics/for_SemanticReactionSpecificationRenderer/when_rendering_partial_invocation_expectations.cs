// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactionSpecificationRenderer;

public class when_rendering_partial_invocation_expectations : given.an_invocation_expectation
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void should_compare_only_stated_fields_and_leave_empty_expectations_unchanged(bool empty)
    {
        var expected = _specification.ThenInvocations.Single();
        var command = _context.Commands[expected.Command];
        SetExpectations(empty ? [] : [expected with { Values = [.. expected.Values.Where(value => command.Properties.Single(property => property.Id == value.TargetProperty).Name == "reason")] }]);
        Render();
        Assert.DoesNotContain("should_match_authored_invocation_1_invoice_id", _source, StringComparison.Ordinal);
        Assert.Equal(!empty, _source.Contains("should_match_authored_invocation_count", StringComparison.Ordinal));
        Assert.Equal(!empty, _source.Contains("should_match_authored_invocation_1_reason", StringComparison.Ordinal));
    }
}
#endif
