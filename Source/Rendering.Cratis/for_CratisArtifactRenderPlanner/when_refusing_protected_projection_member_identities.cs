// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_protected_projection_member_identities
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void should_refuse_protected_join_or_child_identity_without_partial_artifacts(bool join, bool personal)
    {
        var model = invoice_model.Compile(when_rendering_scoped_projections.ScopedSource);
        var execution = SemanticExecutionPlan.Compile(model).Plan!;
        var application = model.Application;
        var concept = application.Concepts.Single(concept => concept.Name == "ProjectName");
        var protectedType = application.Types.Single(type => type.Name == "ProjectNote").Properties.Single(property => property.Name == "name").Type;
        application = application with
        {
            Concepts = [.. application.Concepts.Select(item => item.Id == concept.Id ? item with { Compliance = personal ? new(new(), null) : new(null, new()) } : item)],
            Types = [.. application.Types.Select(type => !join && type.Name == "ProjectNote" ? type with
            {
                Properties = [.. type.Properties.Select(property => property.Name == "noteId" ? property with { Type = protectedType } : property)]
            } : type)],
            Modules = [.. application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => join ? WithProtectedJoin(slice) : slice)]
                })]
            })]
        };
        typeof(ExecutableSemanticModel).GetField($"<{nameof(model.Application)}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, application);
        var options = new CratisRenderingOptions("Projects", "Projects");
        var request = new ArtifactRenderRequest(model, execution, CratisRendering.CreateProfile(application.Name, options), new(ArtifactRenderScopeKind.Application, application.Id));
        var result = new CratisArtifactRenderPlanner().Plan(request);
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-040");
        rendering_refusals.Rejected(result, "STAGE-ESM-040");
    }

    static SemanticSlice WithProtectedJoin(SemanticSlice slice) => slice with
    {
        Projections = [.. slice.Projections.Select(projection => projection.Scope is null ? projection : projection with
        {
            Scope = projection.Scope with
            {
                Joins = [.. projection.Scope.Joins.Select(join => join with
                {
                    On = slice.ReadModels.Single(model => model.Id == projection.ReadModel).Properties.Single(property => property.Name == "name").Id
                })]
            }
        })]
    };
}
