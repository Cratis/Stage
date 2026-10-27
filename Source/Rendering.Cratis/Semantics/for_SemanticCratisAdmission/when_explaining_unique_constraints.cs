// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission;

public class when_explaining_unique_constraints : Specification
{
    [Theory]
    [InlineData("String optional", "Constraint 'UniqueDescription' targets optional properties (description) on 'InvoiceIssued'; Chronicle #4122 indexes null as an empty-string claim.")]
    [InlineData("Int", "Constraint 'UniqueDescription' targets nontext or collection property 'description' on 'InvoiceIssued'; Chronicle's string hash can collide with distinct Screenplay values.")]
    void should_name_the_property_and_its_exact_limitation(string type, string expected)
    {
        var source = invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource)
            .Replace("description String", $"description {type}", StringComparison.Ordinal)
            .Replace("\"First payload\"", type == "Int" ? "1" : "\"First payload\"", StringComparison.Ordinal)
            .Replace("\"Second payload\"", type == "Int" ? "2" : "\"Second payload\"", StringComparison.Ordinal)
            .Replace("      event InvoiceIssued\n", "      constraint UniqueDescription\n        unique description on InvoiceIssued\n      event InvoiceIssued\n", StringComparison.Ordinal);
        var model = invoice_model.Compile(source);
        var plan = invoice_model.Plan(model);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-014" && diagnostic.Message == expected);
        Assert.Empty(plan.Artifacts);
    }
}
