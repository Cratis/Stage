// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_ReadModelAuthorization.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ReadModelAuthorization.when_rendering_a_read_models_authorization;

public class and_no_query_returns_it : an_application_with_policies
{
    string _attribute = null!;

    void Because() => _attribute = Render("InvoiceSummary", QueryForMany("GetOverdueInvoices", "OverdueInvoices", "Accountant"));

    [Fact] void should_require_an_authenticated_caller() => _attribute.ShouldEqual("Authorize");
    [Fact] void should_report_that_nothing_states_who_may_read_it() =>
        _diagnostics.ShouldContain(
            "Read model 'InvoiceSummary' is returned by none of the 1 query declaration(s) in its slice — the document " +
            "states who may read the other read models and nothing about this one, so its rendered read surface " +
            "requires an authenticated caller rather than being left open to everyone.");
}
