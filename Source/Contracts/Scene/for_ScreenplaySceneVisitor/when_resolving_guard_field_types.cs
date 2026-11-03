// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Guards;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenplaySceneVisitor;

public class when_resolving_guard_field_types : Specification
{
    static readonly Cratis.Screenplay.Diagnostics.SourceLocation _at = new(1, 1, "types.play");

    [Theory]
    [InlineData("String", GuardFieldKind.Text)]
    [InlineData("Int", GuardFieldKind.Number)]
    [InlineData("Decimal", GuardFieldKind.Number)]
    [InlineData("Bool", GuardFieldKind.Boolean)]
    [InlineData("Boolean", GuardFieldKind.Boolean)]
    [InlineData("Uuid", GuardFieldKind.Identifier)]
    [InlineData("Guid", GuardFieldKind.Identifier)]
    [InlineData("Date", GuardFieldKind.DateTime)]
    [InlineData("DateTime", GuardFieldKind.DateTime)]
    [InlineData("Missing", GuardFieldKind.Unknown)]
    public void should_classify_optional_fields(string type, object expected)
    {
        var kind = (GuardFieldKind)expected;
        var subject = Subject(type, optional: true);
        var field = subject.Field("item.value");
        field.Kind.ShouldEqual(kind);
        field.IsOptional.ShouldBeTrue();
        field.Admits(GuardOperator.Equal, null).ShouldEqual(kind != GuardFieldKind.Unknown);
        field.Admits(GuardOperator.Contains, "text").ShouldEqual(kind == GuardFieldKind.Text);
        field.Admits(GuardOperator.StartsWith, "text").ShouldEqual(kind == GuardFieldKind.Text);
    }

    [Fact] void should_match_field_names_ordinally() => Subject("String").Field("item.Value").Kind.ShouldEqual(GuardFieldKind.Unknown);
    [Fact] void should_reject_non_item_paths() => Subject("String").Field("$context.value").Kind.ShouldEqual(GuardFieldKind.Unknown);
    [Fact] void should_not_treat_collections_as_text() => Subject("String", collection: true).Field("item.value").Kind.ShouldEqual(GuardFieldKind.Collection);
    [Fact] void should_not_walk_through_a_collection() => Subject("String", collection: true).Field("item.value.length").Kind.ShouldEqual(GuardFieldKind.Unknown);

    [Fact]
    void should_resolve_optional_string_backed_concepts()
    {
        var app = Application("Note");
        app = app with { Concepts = [new ConceptSyntax("Note", "String", [], [], _at)] };
        new GuardSubject(new GuardDeclarations(app), [], [new("Subject", false, false, _at)]).Field("item.value").Kind.ShouldEqual(GuardFieldKind.Text);
    }

    [Fact]
    void should_fail_closed_for_concept_cycles()
    {
        var app = Application("Note");
        app = app with { Concepts = [new ConceptSyntax("Note", "Other", [], [], _at), new ConceptSyntax("Other", "Note", [], [], _at)] };
        new GuardSubject(new GuardDeclarations(app), [], [new("Subject", false, false, _at)]).Field("item.value").Kind.ShouldEqual(GuardFieldKind.Unknown);
    }

    [Fact]
    void should_retain_candidates_until_the_table_selects_its_row()
    {
        var app = given.guard_context_application.Create("mixed-row", ComparisonOperator.Contains);
        var directives = app.Modules.Single().Features.Single().Slices.Single().Screens.Single().Directives;
        var contexts = new GuardContexts(app);
        var row = contexts.At(directives.OfType<ScreenTableSyntax>().Single())!;
        row.Candidates.Count.ShouldEqual(1);
        row.Field("item.note").Kind.ShouldEqual(GuardFieldKind.Text);
    }

    [Fact]
    void should_resolve_table_column_options_through_the_row_type()
    {
        var app = given.guard_context_application.Create("row-enumeration", ComparisonOperator.Equal);
        var module = app.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var screen = slice.Screens.Single();
        var table = screen.Directives.OfType<ScreenTableSyntax>().Single() with { Columns = [new ScreenColumnSyntax("note", "Note", _at)] };
        screen = screen with { Directives = [screen.Directives.OfType<ScreenDataSyntax>().Single(), table] };
        app = app with { Modules = [module with { Features = [feature with { Slices = [slice with { Screens = [screen] }] }] }] };
        var scene = new ScreenplaySceneVisitor().Visit(app);
        var converted = scene.Screens.Single().SlotContent.Values.SelectMany(elements => elements)
            .OfType<global::Cratis.Scene.Model.Elements.ExternalComponent>().Single(element => element.ComponentName == "core:table");
        var column = (global::Cratis.Scene.Model.Elements.ExternalComponent)converted.Slots["columns"].Single();
        ((IReadOnlyList<string>)column.Properties["options"]!).ShouldEqual("open", "closed");
    }

    static GuardSubject Subject(string type, bool optional = false, bool collection = false) =>
        new(new GuardDeclarations(Application(type, optional, collection)), [], [new("Subject", false, false, _at)]);

    static ApplicationSyntax Application(string type, bool optional = false, bool collection = false) => new([], [], [], [], _at)
    {
        Types = [new TypeSyntax("Subject", [new PropertySyntax("value", new(type, collection, optional, _at), _at)], _at)]
    };
}
