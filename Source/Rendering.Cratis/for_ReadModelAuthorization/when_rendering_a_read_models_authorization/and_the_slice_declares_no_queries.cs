// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_ReadModelAuthorization.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ReadModelAuthorization.when_rendering_a_read_models_authorization;

/// <summary>
/// A slice declaring no query leaves its synthesized queries subject to the host's authorization configuration.
/// </summary>
public class and_the_slice_declares_no_queries : an_application_with_policies
{
    string? _attribute;

    void Because() => _attribute = Render("InvoiceSummary");

    [Fact] void should_leave_authorization_to_the_host() => _attribute.ShouldBeNull();
    [Fact] void should_report_nothing() => _diagnostics.ShouldBeEmpty();
}
