// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_ReadModelAuthorization.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ReadModelAuthorization.when_rendering_a_read_models_authorization;

/// <summary>
/// Guarded and unguarded queries are separate Arc operations. Neither may widen the other through a shared
/// read-model attribute; each generated method carries its own authorization instead.
/// </summary>
public class and_one_of_its_own_queries_is_unguarded : an_application_with_policies
{
    string? _attribute;

    void Because() => _attribute = Render(
        "InvoiceSummary",
        Query("GetInvoiceSummary", "InvoiceSummary", "Accountant"),
        Query("PeekInvoiceSummary", "InvoiceSummary"));

    [Fact] void should_not_union_or_widen_the_method_policies_at_type_level() => _attribute.ShouldBeNull();
}
