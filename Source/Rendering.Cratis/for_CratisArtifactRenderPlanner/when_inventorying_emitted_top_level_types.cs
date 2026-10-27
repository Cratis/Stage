// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_inventorying_emitted_top_level_types : a_register_project_render_request
{
    readonly List<string> _missing = [];
    int _declarations;

    void Because()
    {
        foreach (var form in Corpus.SourceForms)
        {
            var model = Compile(form).Model;
            Check(form.Name, new(model, SemanticExecutionPlan.Compile(model).Plan!, _request.Profile, new(ArtifactRenderScopeKind.Application, model.Application.Id)));
        }

        var composite = SemanticModelSerializer.Deserialize(CompositeReadModelKeysCorpus.V27.EsmBytes.AsSpan());
        var module = composite.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        slice = slice with { Specifications = [slice.Specifications.Single(specification => specification.Name == "RecreateRemovedParts")], Queries = [] };
        composite = ExecutableSemanticModel.Create(composite.LanguageVersion, composite.SemanticVersion, composite.Application with { Modules = [module with { Features = [feature with { Slices = [slice] }] }] });
        Check("composite keys", new(composite, SemanticExecutionPlan.Compile(composite).Plan!, CratisRendering.CreateProfile(composite.Application.Name, _options), new(ArtifactRenderScopeKind.Application, composite.Application.Id)));

        var documented = when_rendering_authoring_documentation.Compile(when_rendering_authoring_documentation.Source).Model;
        var profile = CratisRendering.CreateProfile(documented.Application.Name, _options, scene: null, stringsFiles: new Dictionary<string, string> { ["projects.en.strings"] = "project.name = \"Project name\"\n" }, defaultLocale: "en");
        Check("strings", new(documented, SemanticExecutionPlan.Compile(documented).Plan!, profile, new(ArtifactRenderScopeKind.Application, documented.Application.Id)));
    }

    [Fact]
    void should_catalog_every_emitted_top_level_type()
    {
        Assert.True(_declarations > 0, "The rendering fixture corpus emitted no top-level declarations.");
        Assert.True(_missing.Count == 0, string.Join('\n', _missing));
    }

    [Theory]
    [InlineData("@class")]
    [InlineData("ShipmentDispat\\u0063hed")]
    [InlineData("ShipmentDispat\u200cched")]
    public void should_catalog_decoded_capture_source_event_declarations(string name)
    {
        var model = SemanticModelSerializer.Deserialize(PublicEventsCorpus.V9.EsmBytes.AsSpan());
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slices = feature.Slices.Select(slice => slice with
        {
            Specifications = [],
            Events = [.. slice.Events.Select(contract => contract.Name == "ShipmentDispatched" ? contract with { Name = name } : contract)]
        });
        model = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [.. slices] }] }]
        });
        Check(name, new(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile(model.Application.Name, _options), new(ArtifactRenderScopeKind.Application, model.Application.Id)));
        should_catalog_every_emitted_top_level_type();
    }

    void Check(string fixture, ArtifactRenderRequest request)
    {
        var context = new SemanticApplicationContext(request, _options);
        context.ScopeFlatProjections();
        var inventory = context.GeneratedTypes.Select(type => CatalogNames.Qualify(type.Namespace, CatalogNames.MetadataName(type.Name, type.Arity))).ToHashSet(StringComparer.Ordinal);
        var plan = _planner.Plan(request);
        Assert.True(plan.Success, $"{fixture}: {string.Join('\n', plan.Diagnostics)}");

        // leaf types no generated code references; strict resolution cannot reject them; inventory completeness tracked in Cratis/Stage#858
        var specificationClasses = context.Specifications.Values.SelectMany(specification => SemanticSpecificationArtifactRenderer.Render(specification, context))
            .SelectMany(file => CSharpSyntaxTree.ParseText(file.Content).GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
            .Where(type => type.BaseList?.Types.Any(baseType => string.Equals(baseType.Type.ToString(), "global::Cratis.Specifications.Specification", StringComparison.Ordinal) || string.Equals(baseType.Type.ToString(), "Specification", StringComparison.Ordinal)) == true)
            .Select(DeclarationName).ToHashSet(StringComparer.Ordinal);
        var declarations = plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal))
            .SelectMany(artifact => CSharpSyntaxTree.ParseText(Encoding.UTF8.GetString(artifact.Bytes.AsSpan())).GetRoot().DescendantNodes()
                .OfType<MemberDeclarationSyntax>().Where(node => node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax))
            .Where(node => !node.Ancestors().OfType<BaseTypeDeclarationSyntax>().Any());
        foreach (var declaration in declarations)
        {
            var qualified = DeclarationName(declaration);
            if (declaration is ClassDeclarationSyntax && specificationClasses.Contains(qualified)) continue;
            _declarations++;
            if (!inventory.Contains(qualified)) _missing.Add($"{fixture}: {qualified}");
        }
    }

    static string DeclarationName(MemberDeclarationSyntax declaration)
    {
        var ns = string.Join('.', declaration.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(node => node.Name.ToString()));
        var name = SemanticCaptureIdentifier.Decode(declaration is BaseTypeDeclarationSyntax baseType ? baseType.Identifier.Text : ((DelegateDeclarationSyntax)declaration).Identifier.Text);
        var arity = declaration switch
        {
            TypeDeclarationSyntax type => type.TypeParameterList?.Parameters.Count ?? 0,
            DelegateDeclarationSyntax type => type.TypeParameterList?.Parameters.Count ?? 0,
            _ => 0
        };
        return CatalogNames.Qualify(ns, CatalogNames.MetadataName(name, arity));
    }
}
