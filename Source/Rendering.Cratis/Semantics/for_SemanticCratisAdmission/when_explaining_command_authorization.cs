// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission;

public class when_explaining_command_authorization : Specification
{
    [Fact]
    void should_name_the_missing_identifier_before_policy_traversal()
    {
        var source = "policy Staff\n  require authenticated\n" + invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource)
            .Replace("command IssueInvoice\n", "command IssueInvoice\n        authorize Staff\n", StringComparison.Ordinal)
            .Replace("when IssueInvoice\n", "given caller\n          authenticated\n        when IssueInvoice\n", StringComparison.Ordinal)
            .Replace("streamReference String identifier", "streamReference String", StringComparison.Ordinal);
        var plan = invoice_model.Plan(invoice_model.Compile(source));
        Assert.Equal("Authorized command 'IssueInvoice' declares no identifier; it needs exactly one identifier to preserve the reference subject.", Assert.Single(plan.Diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-ESM-015")).Message);
        Assert.Empty(plan.Artifacts);
    }
}
