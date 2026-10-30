// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Naming;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_readable_inline_names;

public class with_colliding_candidates : Specification
{
    readonly SemanticInlineNameSite[] _sites =
    [
        new("first", "Handle place_order", "owner/first", "command"),
        new("second", "HandlePlaceOrder", "owner/second", "command"),
        new("context", "PlaceOrderContext", "owner/context", "command"),
        new("independent", "HandlePlaceOrder", "owner/other", "other-command"),
        new("file-a", "PolicyBody", "policy/a", "namespace-a", PathScope: "files"),
        new("file-b", "POLICYBody", "policy/b", "namespace-b", PathScope: "files"),
        new("visible-type", "PlaceOrderCommand", "owner/snapshot", "command", "visible-types")
    ];
    readonly SemanticInlineNameReservation[] _reserved =
    [
        new("command", "PlaceOrderContext"),
        new("visible-types", "PlaceOrderCommand")
    ];
    SemanticInlineNames _names = null!;
    SemanticInlineNames _reversed = null!;
    SemanticInlineNames _with_suffix_reservation = null!;

    void Because()
    {
        _names = new(_sites, _reserved);
        _reversed = new(_sites.Reverse(), _reserved.Reverse());
        _with_suffix_reservation = new(_sites, [.. _reserved, new("command", _names["first"])]);
    }

    [Fact] void should_suffix_both_normalized_identifier_collisions() => Assert.All(["first", "second"], key => Assert.StartsWith("HandlePlaceOrder_", _names[key], StringComparison.Ordinal));
    [Fact] void should_not_suffix_a_legal_independent_scope() => _names["independent"].ShouldEqual("HandlePlaceOrder");
    [Fact] void should_reserve_existing_members() => Assert.StartsWith("PlaceOrderContext_", _names["context"], StringComparison.Ordinal);
    [Fact] void should_reserve_visible_modeled_types() => Assert.StartsWith("PlaceOrderCommand_", _names["visible-type"], StringComparison.Ordinal);
    [Fact] void should_not_depend_on_traversal_order() => Assert.All(_sites, site => _names[site.Key].ShouldEqual(_reversed[site.Key]));
    [Fact] void should_recheck_suffixes_against_existing_members() => Assert.NotEqual(_names["first"], _with_suffix_reservation["first"]);
    [Fact] void should_distinguish_all_final_identifiers() => Assert.NotEqual(_names["first"], _names["second"]);
    [Fact] void should_distinguish_paths_without_case_sensitivity() => Assert.False(string.Equals(_names["file-a"], _names["file-b"], StringComparison.OrdinalIgnoreCase));
}
