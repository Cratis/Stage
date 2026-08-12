// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_ReadModelAuthorization.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ReadModelAuthorization.when_rendering_a_read_models_authorization;

public class and_its_query_answers_with_a_collection : an_application_with_policies
{
    string _attribute = null!;

    void Because() => _attribute = Render("InvoiceList", QueryForMany("ListInvoices", "InvoiceList", "Accountant"));

    [Fact] void should_attribute_it_to_the_read_model_the_return_type_names() => _attribute.ShouldEqual("Roles(\"Accountant\")");
}
