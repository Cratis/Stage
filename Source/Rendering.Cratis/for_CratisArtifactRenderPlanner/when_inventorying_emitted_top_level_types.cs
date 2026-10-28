// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
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
    readonly Dictionary<string, int> _typedContexts = [];
    int _declarations;
    int _specificationClasses;

    void Because()
    {
        foreach (var (name, request, options) in inventory_fixtures.All())
        {
            Check(name, request, options);
        }
    }

    [Fact]
    void should_catalog_every_emitted_top_level_type()
    {
        Assert.True(_declarations > 0, "The rendering fixture corpus emitted no top-level declarations.");
        Assert.True(_missing.Count == 0, string.Join('\n', _missing));
    }

    [Fact]
    void should_have_exercised_specification_classes_and_every_typed_context_kind()
    {
        Assert.True(_specificationClasses > 0, "No fixture emitted a specification class.");
        foreach (var name in inventory_fixtures.TypedContextNames)
        {
            Assert.True(_typedContexts.GetValueOrDefault(name) > 0, $"{name}: no typed-context declaration was emitted.");
        }
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
        Check(name, new(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile(model.Application.Name, _options), new(ArtifactRenderScopeKind.Application, model.Application.Id)), _options);
        should_catalog_every_emitted_top_level_type();
    }

    void Check(string fixture, ArtifactRenderRequest request, CratisRenderingOptions options)
    {
        var context = new SemanticApplicationContext(request, options);
        context.ScopeFlatProjections();
        var inventory = context.GeneratedTypes.Select(type => CatalogNames.Qualify(type.Namespace, CatalogNames.MetadataName(type.Name, type.Arity))).ToHashSet(StringComparer.Ordinal);
        var plan = _planner.Plan(request);
        Assert.True(plan.Success, $"{fixture}: {string.Join('\n', plan.Diagnostics)}");

        var declarations = plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal))
            .SelectMany(artifact => CSharpSyntaxTree.ParseText(Encoding.UTF8.GetString(artifact.Bytes.AsSpan())).GetRoot().DescendantNodes()
                .OfType<MemberDeclarationSyntax>().Where(node => node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax))
            .Where(node => !node.Ancestors().OfType<BaseTypeDeclarationSyntax>().Any());
        foreach (var declaration in declarations)
        {
            var qualified = DeclarationName(declaration);
            _declarations++;
            if (declaration is ClassDeclarationSyntax type && IsSpecificationClass(type)) _specificationClasses++;
            if (qualified.Contains(".TypedContexts.", StringComparison.Ordinal)) _typedContexts[fixture] = _typedContexts.GetValueOrDefault(fixture) + 1;
            if (!inventory.Contains(qualified)) _missing.Add($"{fixture}: {qualified}");
        }
    }

    static bool IsSpecificationClass(ClassDeclarationSyntax type) =>
        type.BaseList?.Types.Any(baseType => string.Equals(baseType.Type.ToString(), "global::Cratis.Specifications.Specification", StringComparison.Ordinal) || string.Equals(baseType.Type.ToString(), "Specification", StringComparison.Ordinal)) == true;

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
#endif
