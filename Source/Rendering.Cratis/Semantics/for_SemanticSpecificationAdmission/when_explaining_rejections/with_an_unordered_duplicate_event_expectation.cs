// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_explaining_rejections;

public class with_an_unordered_duplicate_event_expectation : given.an_invoice_specification
{
    void Establish()
    {
        Prepare(invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource), "IssuingFirstInvoice");
        _specification = _specification with { ThenEventsInAnyOrder = true, ThenEvents = [_specification.ThenEvents[0], _specification.ThenEvents[0]] };
    }

    void Because() => Validate();

    [Fact] void should_refuse_once_with_the_generic_code() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(["STAGE-ESM-011"]);
    [Fact] void should_explain_the_reason() => Messages().ShouldEqual("STAGE-ESM-011: Specification 'IssuingFirstInvoice' cannot render: The scenario cannot preserve all of its declared fixtures or expectations.");
}
