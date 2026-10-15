// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_an_optional_numeric_concept_command_rule : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var source = "concept Count : Int\n" + invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource)
            .Replace("        produces InvoiceIssued\n", "        count Count?\n        validate\n          count > 0\n        produces InvoiceIssued\n", StringComparison.Ordinal)
            .Replace("          streamReference = ", "          count = 2\n          streamReference = ", StringComparison.Ordinal);
        _plan = invoice_model.Plan(invoice_model.Compile(source));
    }

    [Fact] void should_admit_the_optional_numeric_concept_rule() => _plan.Success.ShouldBeTrue();
    [Fact] void should_unwrap_present_values_and_accept_absence() => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("Issue.cs", StringComparison.Ordinal)).Bytes.AsSpan()).ShouldContain("value is null || value.Value > 0m");
}
