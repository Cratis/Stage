// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_ReadModelAuthorization.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ReadModelAuthorization.when_rendering_a_read_models_authorization;

public class and_another_read_model_has_an_unguarded_query : an_application_with_policies
{
    string _attribute = null!;

    void Because() => _attribute = Render(
        "InvoiceSummary",
        Query("GetInvoiceSummary", "InvoiceSummary", "Accountant"),
        QueryForMany("GetOverdueInvoices", "OverdueInvoices"));

    [Fact] void should_keep_the_guard_its_own_query_declares() => _attribute.ShouldEqual("Roles(\"Accountant\")");
}
