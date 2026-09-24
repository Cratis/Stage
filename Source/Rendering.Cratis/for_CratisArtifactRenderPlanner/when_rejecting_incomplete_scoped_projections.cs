// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rejecting_incomplete_scoped_projections
{
    [Theory]
    [InlineData("missing-root-property")]
    [InlineData("missing-child-property")]
    [InlineData("overwritten-root-identity")]
    [InlineData("overwritten-child-identity")]
    [InlineData("whole-number-arithmetic")]
    public void should_fail_closed_without_artifacts(string variant)
    {
        var source = when_rendering_scoped_projections.ScopedSource;
        if (variant == "whole-number-arithmetic") source = source.Replace("visits Decimal?", "visits Int?", StringComparison.Ordinal);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("scopes"), "scopes", "Scopes.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var original = compilation.Value!.Model;
        var module = original.Application.Modules.Single();
        var feature = module.Features.Single();
        var view = feature.Slices.Single(slice => slice.Kind == SemanticSliceKind.StateView);
        var projection = view.Projections.Single(candidate => candidate.Name == "ProjectSummaryProjection");
        var scope = projection.Scope!;
        var readModel = view.ReadModels.Single(candidate => candidate.Name == "ProjectSummary");
        var name = readModel.Properties.Single(property => property.Name == "name").Id;
        var child = scope.Children.Single();
        var note = original.Application.Types.Single(type => type.Name == "ProjectNote");
        var childName = note.Properties.Single(property => property.Name == "name").Id;

        scope = variant switch
        {
            "missing-root-property" => scope with { From = [.. scope.From.Select(subscription => subscription with { Mappings = [.. subscription.Mappings.Where(mapping => mapping.Target[0] != name)] })] },
            "missing-child-property" => scope with { Children = [child with { Scope = child.Scope with { From = [.. child.Scope.From.Select(subscription => subscription with { Mappings = [.. subscription.Mappings.Where(mapping => mapping.Target[0] != childName)] })] } }] },
            "overwritten-root-identity" => scope with { From = [scope.From[0] with { Key = new SemanticProjectionValueKey(new SemanticProjectionEventSourceIdentity()) }, .. scope.From.Skip(1)] },
            "overwritten-child-identity" => scope with { Children = [child with { Scope = child.Scope with { From = [child.Scope.From[0] with { Key = new SemanticProjectionValueKey(new SemanticProjectionEventSourceIdentity()) }] } }] },
            "whole-number-arithmetic" => scope,
            _ => throw new UnknownVariant(variant)
        };
        var changed = view with { Projections = [.. view.Projections.Select(candidate => candidate.Id == projection.Id ? candidate with { Scope = scope } : candidate)] };
        var model = ExecutableSemanticModel.Create(
            original.LanguageVersion,
            original.SemanticVersion,
            original.Application with { Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(slice => slice.Id == view.Id ? changed : slice)] }] }] });
        var execution = SemanticExecutionPlan.Compile(original);
        Assert.True(execution.Success);
        var options = new CratisRenderingOptions("Projects", "Projects");
        var request = new ArtifactRenderRequest(model, execution.Plan!, CratisRendering.CreateProfile(model.Application.Name, options), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, options);
        Assert.Contains(SemanticCratisAdmission.Evaluate(context, context.SelectedSlices()), diagnostic => diagnostic.Code == "STAGE-ESM-017");
    }

    sealed class UnknownVariant(string variant) : Exception($"Unknown projection variant '{variant}'.");
}
