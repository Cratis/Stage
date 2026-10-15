// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Screens;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_TemplateReferenceConverter;

public class when_collecting_template_references : given.a_translated_application
{
    const string Source =
        """
        layout AppShell
          category application
          content

        screen template OwnShape
          category Workspace
          fits slot content
          main

        module Shop
          template blueprint Business.Orders OrderDialog version "^1.2"
          feature Orders
            template OwnShape
            slice StateView OrderList
              template blueprint Business.Orders OrderList
              screen List
                title "Orders"
        """;

    IReadOnlyList<TemplateAssignment> References => _application.TemplateReferences;

    void Because() => Translate(Source.Replace("screen template OwnShape\n  category Workspace\n  fits slot content\n  main\n\n", string.Empty, StringComparison.Ordinal).Replace("layout AppShell", "layout AppShell", StringComparison.Ordinal));

    [Fact] void should_collect_every_assignment() => References.Count.ShouldEqual(3);
    [Fact] void should_convert_a_blueprint_template_with_its_version_range() => References[0].Reference.ShouldEqual(new TemplateReference(TemplateReferenceSource.Blueprint, "OrderDialog", "Business.Orders", "^1.2"));
    [Fact] void should_record_where_it_was_assigned() => (References[0].Scope, References[0].Owner).ShouldEqual(("module", "Shop"));
    [Fact] void should_convert_an_own_template_by_name() => References[1].Reference.ShouldEqual(new TemplateReference(TemplateReferenceSource.Own, "OwnShape"));
    [Fact] void should_record_a_feature_assignment() => (References[1].Scope, References[1].Owner).ShouldEqual(("feature", "Shop.Orders"));
    [Fact] void should_leave_the_version_open_when_none_is_pinned() => References[2].Reference.Version.ShouldBeNull();
    [Fact] void should_record_a_slice_assignment() => (References[2].Scope, References[2].Owner).ShouldEqual(("slice", "Shop.Orders.OrderList"));
}
