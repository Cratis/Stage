// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_UiSyntaxAdmission;

public class when_refusing_new_ui_members : Specification
{
    // Members the Scene converters still drop: a Scene dialog template declares no outlets.
    [Theory]
    [InlineData(typeof(DialogTemplateSyntax), "Outlets")]
    public void should_refuse_instead_of_dropping_the_authored_member(Type type, string member)
    {
        var error = Catch.Exception(() => new UiSyntaxAdmission().VisitNode(Authored(type, member)));
        error.ShouldBeOfExactType<UnsupportedUiSyntax>();
        error.Message.ShouldContain(UnsupportedUiSyntax.DiagnosticCode);
    }

    // Members the Scene converters translate, or the canonical screen composition corpus admits.
    [Theory]
    [InlineData(typeof(ApplicationSyntax), "Templates")]
    [InlineData(typeof(ModuleSyntax), "Templates")]
    [InlineData(typeof(FeatureSyntax), "Templates")]
    [InlineData(typeof(SliceSyntax), "Templates")]
    [InlineData(typeof(FormSyntax), "ColumnMode")]
    [InlineData(typeof(FormSyntax), "Columns")]
    [InlineData(typeof(LayoutSyntax), "Category")]
    [InlineData(typeof(LayoutSyntax), "TemplateType")]
    [InlineData(typeof(LayoutSyntax), "Exposes")]
    [InlineData(typeof(LayoutSyntax), "Outlets")]
    [InlineData(typeof(ScreenTemplateSyntax), "Category")]
    [InlineData(typeof(ScreenTemplateSyntax), "TemplateType")]
    [InlineData(typeof(ScreenTemplateSyntax), "Exposes")]
    [InlineData(typeof(ScreenTemplateSyntax), "Outlets")]
    [InlineData(typeof(DialogTemplateSyntax), "Category")]
    [InlineData(typeof(DialogTemplateSyntax), "TemplateType")]
    [InlineData(typeof(DialogTemplateSyntax), "Exposes")]
    [InlineData(typeof(UiProfileSyntax), "Icons")]
    [InlineData(typeof(ScreenComponentSyntax), "Context")]
    [InlineData(typeof(ScreenComponentSyntax), "Properties")]
    [InlineData(typeof(ScreenComponentSyntax), "Exposes")]
    [InlineData(typeof(ScreenComponentSyntax), "Presentation")]
    [InlineData(typeof(ScreenComponentSyntax), "Icon")]
    [InlineData(typeof(ScreenComponentSyntax), "Outlets")]
    [InlineData(typeof(ScreenToolbarSyntax), "Items")]
    [InlineData(typeof(ScreenNavigateSyntax), "Route")]
    [InlineData(typeof(ScreenNavigateSyntax), "Outlet")]
    [InlineData(typeof(ScreenNavigateSyntax), "Parameters")]
    [InlineData(typeof(ScreenSyntax), "Contributions")]
    [InlineData(typeof(ApplicationSyntax), "Exposures")]
    [InlineData(typeof(ApplicationSyntax), "InstanceContributions")]
    [InlineData(typeof(LayoutSyntax), "Scopes")]
    [InlineData(typeof(ScreenTemplateSyntax), "Content")]
    [InlineData(typeof(ScreenTemplateSyntax), "Scopes")]
    [InlineData(typeof(ScreenTemplateSyntax), "DisplayName")]
    [InlineData(typeof(ScreenTemplateSyntax), "Description")]
    [InlineData(typeof(DialogTemplateSyntax), "Content")]
    [InlineData(typeof(DialogTemplateSyntax), "Scopes")]
    [InlineData(typeof(DialogTemplateSyntax), "DisplayName")]
    [InlineData(typeof(DialogTemplateSyntax), "Description")]
    public void should_admit_the_translated_or_corpus_member(Type type, string member) =>
        Catch.Exception(() => new UiSyntaxAdmission().VisitNode(Authored(type, member))).ShouldBeNull();

    [Theory]
    [InlineData(UiBindingKind.DataContext)]
    [InlineData(UiBindingKind.QueryResult)]
    [InlineData(UiBindingKind.ComponentProperty)]
    public void should_admit_every_binding_source(UiBindingKind kind) =>
        Catch.Exception(() => new UiSyntaxAdmission().VisitNode(new UiBindingSyntax(kind, "id", SourceLocation.Start))).ShouldBeNull();

    static SyntaxNode Authored(Type type, string member)
    {
        var node = (SyntaxNode)Create(type);
        var property = type.GetProperty(member)!;
        property.SetValue(node, AuthoredValue(property.PropertyType));
        return node;
    }

    // Neutral constructor values keep each test independent: only the named additive member is authored.
    static object Create(Type type)
    {
        if (type == typeof(string)) return "authored";
        if (type == typeof(SourceLocation)) return SourceLocation.Start;
        if (type.IsValueType) return Activator.CreateInstance(type)!;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)) return Array.CreateInstance(type.GenericTypeArguments[0], 0);
        var constructor = type.GetConstructors().OrderByDescending(candidate => candidate.GetParameters().Length).First();
        return constructor.Invoke([.. constructor.GetParameters().Select(ConstructorValue)]);
    }

    static object? ConstructorValue(ParameterInfo parameter)
    {
        if (parameter.HasDefaultValue) return parameter.DefaultValue;

        return new NullabilityInfoContext().Create(parameter).ReadState == NullabilityState.Nullable ? null : Create(parameter.ParameterType);
    }

    static object AuthoredValue(Type type)
    {
        if (type.IsEnum) return Enum.GetValues(type).GetValue(1)!;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            var elementType = type.GenericTypeArguments[0];
            var values = Array.CreateInstance(elementType, 1);
            values.SetValue(Create(elementType), 0);
            return values;
        }

        return Create(type);
    }
}
