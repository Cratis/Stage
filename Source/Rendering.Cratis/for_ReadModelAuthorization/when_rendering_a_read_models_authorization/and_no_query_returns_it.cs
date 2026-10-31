// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_ReadModelAuthorization.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ReadModelAuthorization.when_rendering_a_read_models_authorization;

public class and_no_query_returns_it : an_application_with_policies
{
    string? _attribute;

    void Because() => _attribute = Render("InvoiceSummary", QueryForMany("GetOverdueInvoices", "OverdueInvoices", "Accountant"));

    [Fact] void should_not_infer_authorization_from_a_sibling_query() => _attribute.ShouldBeNull();
    [Fact] void should_not_report_inferred_authorization() => _diagnostics.ShouldBeEmpty();
}
