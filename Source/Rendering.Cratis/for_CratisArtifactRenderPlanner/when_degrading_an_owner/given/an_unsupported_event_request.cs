// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_degrading_an_owner.given;

public class an_unsupported_event_request : for_CratisArtifactRenderPlanner.given.a_register_project_render_request
{
    protected SemanticId _independentCommand;
    protected SemanticId _unsupportedEvent;

    void Establish()
    {
        var source = Corpus.SourceForms.Single(form => form.Name == "single").Documents.Single().Text + "\n" +
            for_CratisArtifactRenderPlanner.given.invoice_model.Source("String", for_CratisArtifactRenderPlanner.given.invoice_model.TextSource, for_CratisArtifactRenderPlanner.given.invoice_model.OtherTextSource);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("OwnerOmission"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("omission"), "omission", "omission.play", source);
        var compiled = new SemanticModelCompiler().Compile("OwnerOmission", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compiled.Success, string.Join("; ", compiled.Diagnostics));
        _model = compiled.Value!.Model;
        _module = _model.Application.Modules.Single(module => module.Name == "Projects");
        _feature = _module.Features.Single();
        _registerProject = _feature.Slices.Single(slice => slice.Name == "RegisterProject");
        _projectLookup = _feature.Slices.Single(slice => slice.Name == "ProjectLookup");
        _independentCommand = _model.Application.Modules.Single(module => module.Name == "Billing").Features.Single().Slices.Single().Commands.Single().Id;
        _unsupportedEvent = _registerProject.Events.Single().Id;
        var execution = SemanticExecutionPlan.Compile(_model).Plan!;
        UnsupportedProperties(_registerProject.Events.Single());
        _request = _request with
        {
            Model = _model,
            Profile = CratisRendering.CreateProfile(_model.Application.Name, _options),
            ExecutionPlan = execution,
            Scope = new(ArtifactRenderScopeKind.Application, _model.Application.Id),
            IdentityCatalog = compiled.Value.Documents.IdentityCatalog,
            SemanticIdentities = compiled.Value.Documents.IdentityCatalog.Semantics
        };
    }

    protected void ChangeApplication(Func<SemanticApplication, SemanticApplication> change)
    {
        var model = _request.Model with { };
        Replace(model, "Application", change(model.Application));
        _request = _request with { Model = model };
    }

    protected void ChangeIndependent(Func<SemanticSlice, SemanticSlice> change) => ChangeApplication(application => application with
    {
        Modules = [.. application.Modules.Select(module => module.Name == "Billing"
            ? module with { Features = [.. module.Features.Select(feature => feature with { Slices = [.. feature.Slices.Select(change)] })] }
            : module)]
    });

    protected static void Replace<T>(T node, string property, object value)
        where T : class => typeof(T).GetField($"<{property}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(node, value);

    protected static void UnsupportedProperties(SemanticEventContract owner)
    {
        var name = owner.Properties.Any(property => property.Name == "name") ? "name" : owner.Properties[0].Name;
        var properties = owner.Properties.Select(property => property.Name == name
            ? property with { Type = property.Type with { Kind = (SemanticTypeReferenceKind)999 } } : property).ToImmutableArray();
        typeof(SemanticEventContract).GetField("<Properties>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(owner, properties);
    }
}
