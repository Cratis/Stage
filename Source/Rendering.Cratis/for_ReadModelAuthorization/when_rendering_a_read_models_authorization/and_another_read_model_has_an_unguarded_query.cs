// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_ReadModelAuthorization.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ReadModelAuthorization.when_rendering_a_read_models_authorization;

public class and_another_read_model_has_an_unguarded_query : an_application_with_policies
{
    string? _attribute;

    void Because() => _attribute = Render(
        "InvoiceSummary",
        Query("GetInvoiceSummary", "InvoiceSummary", "Accountant"),
        QueryForMany("GetOverdueInvoices", "OverdueInvoices"));

    [Fact] void should_leave_each_querys_authorization_on_its_generated_method() => _attribute.ShouldBeNull();
}
