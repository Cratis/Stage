// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_ReadModelAuthorization.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ReadModelAuthorization.when_rendering_a_read_models_authorization;

/// <summary>
/// A slice declaring no query says nothing about reading anything, which is a different silence from one that
/// states read authorization for its other read models and none for this one.
/// </summary>
public class and_the_slice_declares_no_queries : an_application_with_policies
{
    string _attribute = null!;

    void Because() => _attribute = Render("InvoiceSummary");

    [Fact] void should_state_the_absence_as_anonymous() => _attribute.ShouldEqual("AllowAnonymous");
    [Fact] void should_report_nothing() => _diagnostics.ShouldBeEmpty();
}
