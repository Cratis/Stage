// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Projections;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_retaining_classic_projection_fallbacks : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_leave_joins_children_nested_and_every_literals_byte_identical(bool literals)
    {
        var source = literals ? every_literal_projection.Source : when_rendering_scoped_projections.ScopedSource;
        var classic = when_selecting_model_bound_projections.Plan(source, null);
        var selected = when_selecting_model_bound_projections.Plan(source, "model-bound");
        var classicArtifact = Assert.Single(classic.Artifacts, artifact => artifact.RelativePath.EndsWith("/ProjectLookup/ProjectLookup.cs", StringComparison.Ordinal));
        var selectedArtifact = Assert.Single(selected.Artifacts, artifact => artifact.RelativePath == classicArtifact.RelativePath);
        Assert.Equal(classicArtifact.Sha256, selectedArtifact.Sha256);
        Assert.Contains("IProjectionFor", Encoding.UTF8.GetString(selectedArtifact.Bytes.AsSpan()));
        Assert.Contains("#pragma warning disable CHR0029, CHR0042", Encoding.UTF8.GetString(selectedArtifact.Bytes.AsSpan()));
    }

    [Fact]
    public void should_keep_payload_keys_on_the_byte_identical_fluent_path()
    {
        var classic = when_selecting_model_bound_projections.Plan(when_selecting_model_bound_projections.PayloadSource, "classic");
        var selected = when_selecting_model_bound_projections.Plan(when_selecting_model_bound_projections.PayloadSource, "model-bound");
        Assert.Contains("IProjectionFor", when_selecting_model_bound_projections.Code(selected));
        Assert.Equal(when_selecting_model_bound_projections.Code(classic), when_selecting_model_bound_projections.Code(selected));
    }

    [Theory]
    [InlineData("parent")]
    [InlineData("constant-key")]
    [InlineData("guid-literal")]
    [InlineData("concept-literal")]
    [InlineData("date-literal")]
    [InlineData("decimal-literal")]
    [InlineData("nested-target")]
    public void should_not_select_an_inexpressible_scope(string shape)
    {
        var model = when_selecting_model_bound_projections.Compile(when_selecting_model_bound_projections.Source);
        var execution = SemanticExecutionPlan.Compile(model).Plan!;
        var profile = CratisRendering.WithProjectionConvention(CratisRendering.CreateProfile("Projects", new("Projects", "Projects")), ProjectionConventionInput.Create("Projects", "model-bound"));
        var request = new ArtifactRenderRequest(model, execution, profile, new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, new("Projects", "Projects"));
        var slice = context.AllSlices.Single(item => item.Slice.Projections.Length > 0).Slice;
        var projection = slice.Projections.Single();
        var readModel = slice.ReadModels.Single();
        var scope = projection.Scope!;
        var rootFrom = scope.From[0];
        var mapping = rootFrom.Mappings[0];
        var property = readModel.Properties.Single(item => item.Id == mapping.Target[0]);
        var type = shape switch
        {
            "guid-literal" => property.Type with { Kind = SemanticTypeReferenceKind.Primitive, Primitive = SemanticPrimitiveType.Uuid },
            "date-literal" => property.Type with { Kind = SemanticTypeReferenceKind.Primitive, Primitive = SemanticPrimitiveType.Date },
            "decimal-literal" => property.Type with { Kind = SemanticTypeReferenceKind.Primitive, Primitive = SemanticPrimitiveType.DecimalNumber },
            _ => property.Type
        };
        readModel = readModel with { Properties = [.. readModel.Properties.Select(item => item.Id == property.Id ? item with { Type = type } : item)] };
        rootFrom = shape switch
        {
            "parent" => rootFrom with { ParentKey = rootFrom.Key },
            "constant-key" => rootFrom with { Key = new SemanticProjectionValueKey(new SemanticProjectionLiteral(SemanticValue.Text("fixed"))) },
            "nested-target" => rootFrom with { Mappings = [mapping with { Target = [mapping.Target[0], mapping.Target[0]] }] },
            _ => rootFrom with { Mappings = [mapping with { Source = new SemanticProjectionLiteral(shape == "decimal-literal" ? SemanticValue.Number(1.5m) : SemanticValue.Text("2026-10-21")) }] }
        };
        var changed = projection with { Scope = scope with { From = [rootFrom, .. scope.From.Skip(1)] } };
        ModelBoundProjectionEligibility.CanRender(changed, readModel, context).ShouldBeFalse();
    }
}
#endif
