// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_an_unfiltered_query_as_before : Specification
{
    byte[] _actual = [];
    byte[] _expected = [];

    void Establish() => _expected = Encoding.UTF8.GetBytes("    public static async global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<InvoiceSummary>> AllInvoices(global::Cratis.Chronicle.ReadModels.IReadModels readModels) => await readModels.GetInstances<InvoiceSummary>();\n");

    void Because()
    {
        var plan = invoice_model.Plan(invoice_model.Compile(a_query_filter_model.Source.Replace("=> observable InvoiceSummary[]", "=> InvoiceSummary[]", StringComparison.Ordinal)));
        Assert.True(plan.Success, string.Join("; ", plan.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
        var content = Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("Lookup.cs", StringComparison.Ordinal)).Bytes.AsSpan());
        _actual = Encoding.UTF8.GetBytes(content.Split('\n').Single(line => line.Contains(" AllInvoices(", StringComparison.Ordinal)) + "\n");
    }

    [Fact] void should_preserve_the_unfiltered_query_bytes() => Assert.Equal(_expected, _actual);
}
