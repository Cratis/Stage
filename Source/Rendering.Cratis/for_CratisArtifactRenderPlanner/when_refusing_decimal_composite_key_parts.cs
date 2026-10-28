// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Decimal composite key parts stay refused until Cratis/Stage#847. #847 flips this file to
/// <c>when_rendering_decimal_composite_key_parts</c> and asserts that no <c>STAGE-ESM-048</c> diagnostic is reported.
/// </summary>
public class when_refusing_decimal_composite_key_parts
{
    public static TheoryData<string, string> Shapes => new()
    {
        { "a decimal primitive part", when_refusing_composite_key_shapes.Valid.Replace("reference Reference", "reference Decimal", StringComparison.Ordinal) },
        { "a concept over decimal part", when_refusing_composite_key_shapes.Valid.Replace("concept Reference : String", "concept Reference : Decimal", StringComparison.Ordinal) }
    };

    [Theory]
    [MemberData(nameof(Shapes))]
    public void should_refuse_the_decimal_part(string shape, string source)
    {
        var plan = invoice_model.Plan(invoice_model.Compile(source));
        var refusals = plan.Diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-ESM-048" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).ToArray();

        Assert.True(
            refusals.Length == 1 && new[] { "Read model 'PaymentTotals'", "'reference'", "decimal", "Cratis/Chronicle#5427", "Cratis/Stage#847" }.All(text => refusals[0].Message.Contains(text, StringComparison.Ordinal)),
            $"{shape}: artifacts={plan.Artifacts.Length} refusals={string.Join(',', plan.Refusals.SelectMany(r => r.Codes))} gaps={plan.ImplementationGaps.Length} {string.Join(" | ", plan.Diagnostics.Select(diagnostic => $"{diagnostic.Code} {diagnostic.Message}"))}");
    }
}
