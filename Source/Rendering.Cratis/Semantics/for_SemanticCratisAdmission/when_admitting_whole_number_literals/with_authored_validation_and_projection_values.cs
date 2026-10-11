// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_admitting_whole_number_literals;

public class with_authored_validation_and_projection_values
{
    [Theory]
    [InlineData("concept")]
    [InlineData("command")]
    [InlineData("produced")]
    [InlineData("projection")]
    public void should_refuse_each_emitted_int32_literal_position(string position)
    {
        var source = a_whole_number_model.Source.Replace("900719925474099", "42", StringComparison.Ordinal);
        source = position switch
        {
            "concept" => source.Replace("concept Quantity : Int", "concept Quantity : Int\n  validate\n    < 2147483648", StringComparison.Ordinal),
            "command" => source.Replace("        count Int\n        produces", "        count Int\n        validate\n          count < 2147483648\n        produces", StringComparison.Ordinal),
            "produced" => WithoutSpecifications(source.Replace("          count = count\n      event StockRecorded", "          count = 2147483648\n      event StockRecorded", StringComparison.Ordinal)),
            _ => source.Replace("          count = count\n      query", "          count = 2147483648\n      query", StringComparison.Ordinal)
        };
        var plan = invoice_model.Plan(a_whole_number_model.Compile(SemanticVersion.V7, source));
        plan.Success.ShouldBeFalse();
        plan.Diagnostics.Select(diagnostic => diagnostic.Code).Distinct().ShouldContainOnly("STAGE-ESM-031");
    }

    // The specifications state outcomes for the original mapping; without them the mapping literal is the only out-of-range value.
    static string WithoutSpecifications(string source) =>
        source[..source.IndexOf("      specification ", StringComparison.Ordinal)] + source[source.IndexOf("    slice StateView", StringComparison.Ordinal)..];

    [Fact]
    public void should_leave_decimal_literals_unaffected()
    {
        var model = invoice_model.Compile(invoice_model.Source("Decimal", "2147483648", "-2147483649"));
        invoice_model.Plan(model).Success.ShouldBeTrue();
    }
}
