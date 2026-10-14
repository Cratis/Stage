// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_reaction_appends;

public class with_a_coded_refusal_error : for_SemanticReactionSpecificationRenderer.given.a_reaction_specification
{
    void Establish() => Prepare(InvocationSource
        .Replace("      command Complete", "      command Start\n        id String identifier\n        produces Started\n          for id\n          id = id\n      command Complete", StringComparison.Ordinal)
        .Replace("        produces Completed", "        validate\n          id not empty message \"required\"\n        produces Completed", StringComparison.Ordinal)
        .Replace("            id = id", "            id = \"\"\n            on refused by authorization\n              acknowledge", StringComparison.Ordinal)
        .Replace("        when append Started", "        when append Started\n          for \"one\"", StringComparison.Ordinal)
        .Replace("        then Completed", "        then error \"required\"", StringComparison.Ordinal));

    [Theory]
    [InlineData(null)]
    [InlineData("WrongCode")]
    public void should_not_silently_discard_an_expected_error_code(string? code)
    {
        _specification = _specification with { ThenErrors = [new(code, "required")] };
        Validate();
        if (code is null)
        {
            _diagnostics.ShouldBeEmpty();
        }
        else
        {
            _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(["STAGE-ESM-011"]);
            _diagnostics.Single().Message.ShouldContain("cannot faithfully assert coded");
        }
    }
}
