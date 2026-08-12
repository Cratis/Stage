// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_ReadModelAuthorization.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ReadModelAuthorization.when_rendering_a_read_models_authorization;

/// <summary>
/// The collapse to anonymous is correct for queries that genuinely read the same read model — the document
/// lets anyone read it through the unguarded one, so the rendered pair standing in for both must too.
/// </summary>
public class and_one_of_its_own_queries_is_unguarded : an_application_with_policies
{
    string _attribute = null!;

    void Because() => _attribute = Render(
        "InvoiceSummary",
        Query("GetInvoiceSummary", "InvoiceSummary", "Accountant"),
        Query("PeekInvoiceSummary", "InvoiceSummary"));

    [Fact] void should_follow_the_query_that_asks_for_nothing() => _attribute.ShouldEqual("AllowAnonymous");
}
