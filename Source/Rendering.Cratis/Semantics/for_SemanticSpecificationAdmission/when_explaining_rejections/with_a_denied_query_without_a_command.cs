// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_explaining_rejections;

public class with_a_denied_query_without_a_command : given.an_invoice_specification
{
    void Establish()
    {
        Prepare(invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource), "IssuingFirstInvoice");
        _specification = _specification with { When = null, ThenDenied = true };
    }

    void Because() => Validate();

    [Fact] void should_refuse_once_with_the_generic_code() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(["STAGE-ESM-011"]);
    [Fact] void should_explain_the_reason() => Messages().ShouldEqual("STAGE-ESM-011: Specification 'IssuingFirstInvoice' cannot render: A denied query must be executed through Arc's query pipeline to assert Unauthorized and no returned data; a direct read-model lookup does not evaluate its policy.");
}
