// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationValuesRenderer;

public class when_rendering_fixture_values : Specification
{
    SemanticTypeSystem _types = null!;
    SemanticApplicationContext _context = null!;

    void Establish()
    {
        var model = invoice_model.Compile("type Details\n  title String\n" + invoice_model.Source("Uuid", invoice_model.UuidSource, invoice_model.OtherUuidSource));
        var options = new CratisRenderingOptions("Invoices", "Invoices");
        _context = new(new(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile(model.Application.Name, options), new(ArtifactRenderScopeKind.Application, model.Application.Id)), options);
        _types = new(_context) { SpecificationValues = new(_context, _context.Specifications.Values.First()) };
    }

    [Fact] void should_declare_each_repeated_value_once()
    {
        var value = SemanticValue.Text("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        var type = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Uuid);
        var first = _types.Value(value, type);
        var second = _types.Value(value, type);
        var source = Render($"{first}; {second};");
        source.Split("Guid.Parse(", StringSplitOptions.None).Length.ShouldEqual(2);
        source.ShouldContain("static readonly global::System.Guid _streamReference =");
        source.ShouldContain("_streamReference; _streamReference;");
    }

    [Fact] void should_keep_a_single_simple_literal_inline() => Render(_types.Value(SemanticValue.Text("one"), SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text))).ShouldNotContain("static readonly");
    [Fact] void should_discard_unused_values()
    {
        _types.Value(SemanticValue.Text("unused"), SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text));
        Render(string.Empty).ShouldNotContain("unused");
    }

    [Fact] void should_suffix_colliding_property_names()
    {
        var type = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Uuid);
        var first = _types.Value(SemanticValue.Text("3fa85f64-5717-4562-b3fc-2c963f66afa6"), type);
        var second = _types.Value(SemanticValue.Text("4fa85f64-5717-4562-b3fc-2c963f66afa7"), type);
        var third = _types.Value(SemanticValue.Text("5fa85f64-5717-4562-b3fc-2c963f66afa8"), type);
        var fourth = _types.Value(SemanticValue.Text("6fa85f64-5717-4562-b3fc-2c963f66afa9"), type);
        Render($"{first}; {second}; {third}; {fourth};").ShouldContain("_uuid2");
    }

    [Theory]
    [InlineData(SemanticPrimitiveType.Date, "2026-01-02", "DateOnly")]
    [InlineData(SemanticPrimitiveType.DateTime, "2026-01-02T03:04:05Z", "DateTimeOffset")]
    public void should_name_single_use_complex_values(SemanticPrimitiveType primitive, string value, string typeName) => Render(_types.Value(SemanticValue.Text(value), SemanticTypeReference.ForPrimitive(primitive))).ShouldContain($"static readonly global::System.{typeName}");

    [Fact] void should_name_composite_values()
    {
        var type = _context.Types.Values.Single();
        var value = new SemanticCompositeValue([new(type.Properties.Single().Id, SemanticValue.Text("Title"))]);
        Render(_types.Value(value, SemanticTypeReference.ForCompositeType(type.Id))).ShouldContain("static readonly global::Invoices.Common.Details _details = new global::Invoices.Common.Details(\"Title\");");
    }

    string Render(string body) => _types.SpecificationContent($"    {SemanticSpecificationValuesRenderer.Declarations}{Environment.NewLine}{body}");
}
