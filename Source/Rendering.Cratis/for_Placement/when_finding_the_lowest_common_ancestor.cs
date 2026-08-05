// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_Placement;

public class when_finding_the_lowest_common_ancestor : Specification
{
    IReadOnlyList<string> _sharedFeature = null!;
    IReadOnlyList<string> _sharedModule = null!;
    IReadOnlyList<string> _acrossModules = null!;
    IReadOnlyList<string> _singlePath = null!;
    IReadOnlyList<string> _noPaths = null!;

    void Because()
    {
        _sharedFeature = Placement.LowestCommonAncestor(
        [
            ["Billing", "Invoices", "Register"],
            ["Billing", "Invoices", "Cancel"],
        ]);

        _sharedModule = Placement.LowestCommonAncestor(
        [
            ["Billing", "Invoices", "Register"],
            ["Billing", "Payments", "Refund"],
        ]);

        _acrossModules = Placement.LowestCommonAncestor(
        [
            ["Billing", "Invoices", "Register"],
            ["Support", "Tickets", "Open"],
        ]);

        _singlePath = Placement.LowestCommonAncestor([["Billing", "Invoices", "Register"]]);
        _noPaths = Placement.LowestCommonAncestor([]);
    }

    [Fact] void should_stop_at_the_shared_feature() => _sharedFeature.ShouldContainOnly("Billing", "Invoices");
    [Fact] void should_stop_at_the_shared_module() => _sharedModule.ShouldContainOnly("Billing");
    [Fact] void should_be_empty_across_different_modules() => _acrossModules.ShouldBeEmpty();
    [Fact] void should_return_the_single_path_unchanged() => _singlePath.ShouldContainOnly("Billing", "Invoices", "Register");
    [Fact] void should_be_empty_for_no_paths() => _noPaths.ShouldBeEmpty();
}
