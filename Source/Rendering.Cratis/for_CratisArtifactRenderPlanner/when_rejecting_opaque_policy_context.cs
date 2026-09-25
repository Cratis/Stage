// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rejecting_opaque_policy_context
{
    const string Policy = """
        policy CustomAccess
          ```csharp
          return context.Identity.IsAuthenticated;
          ```
        """;

    [Fact]
    public void should_name_the_missing_occurrence_for_a_command()
    {
        var invoice = invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource);
        var source = Policy + "\n" + invoice[..invoice.IndexOf("      specification", StringComparison.Ordinal)]
            .Replace("command IssueInvoice\n", "command IssueInvoice\n        authorize CustomAccess\n", StringComparison.Ordinal);
        AssertRejected(invoice_model.Plan(invoice_model.Compile(source)), "IssueInvoice");
    }

    [Fact]
    public void should_name_the_missing_occurrence_for_a_query()
    {
        var source = when_rendering_portable_authorization.Source.Replace(
            "policy OwnQuery\n  require claim \"owner\" matches subject",
            Policy.Replace("CustomAccess", "OwnQuery", StringComparison.Ordinal),
            StringComparison.Ordinal);
        AssertRejected(invoice_model.Plan(invoice_model.Compile(source)), "InvoiceById");
    }

    static void AssertRejected(global::Cratis.Stage.Contracts.Rendering.ArtifactRenderPlan plan, string operation)
    {
        Assert.False(plan.Success);
        Assert.Empty(plan.Artifacts);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-015" &&
            diagnostic.Message.Contains(operation, StringComparison.Ordinal) &&
            diagnostic.Message.Contains("PolicyContext.Occurred", StringComparison.Ordinal) &&
            diagnostic.Message.Contains("Arc's authorization boundary", StringComparison.Ordinal));
    }
}
