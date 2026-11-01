// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_TemplateResult;

public class when_checking_success : Specification
{
    [Theory]
    [InlineData("value", null, true)]
    [InlineData(null, null, true)]
    [InlineData(null, TemplateCodes.Input, false)]
    public void should_succeed_only_without_a_refusal(string? value, string? refusalCode, bool expected)
    {
        var refusal = refusalCode is null ? null : new TemplateDiagnostic(refusalCode, "Invalid input.", string.Empty, 0, 0);
        var result = new TemplateResult<string>(value, refusal);
        result.Succeeded.ShouldEqual(expected);
    }
}
