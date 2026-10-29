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
    string? _attribute;

    void Because() => _attribute = Render("InvoiceSummary");

    [Fact] void should_require_authentication() => _attribute.ShouldEqual("Authorize");
    [Fact] void should_report_one_default_note_for_the_synthesized_pair() => _diagnostics.Single().StartsWith("STAGE-ESM-054:", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_report_the_inferred_default() => _diagnostics.Single().ShouldContain("requires an authenticated caller");
}
