// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_inventorying_allocated_responses;

public class with_completed_inventory
{
    [Theory]
    [InlineData("ordinary", "PlaceOrderResponse")]
    [InlineData("sibling", "PlaceOrderResponse_")]
    [InlineData("field", "PlaceOrderResponse_")]
    [InlineData("namespace", "PlaceOrderResponse_")]
    public void should_match_allocated_emitted_and_referenced_response_identities(string collision, string expected)
    {
        var context = response_inventory_model.Context(collision);
        var command = response_inventory_model.Command(context);
        Assert.Equal(expected, context.ResponseType(command));
        var ns = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(command.Id).Path);
        Assert.Single(context.GeneratedTypes, type => type == new GeneratedTypeName(ns, expected));
        Assert.Equal($"global::{ns}.{expected}", new SemanticTypeSystem(context).SliceDeclaration(command.Id, expected));
        AssertResponseArtifacts(context, ns, expected);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void should_complete_both_lazy_access_orders_without_changing_allocation(bool inventoryFirst)
    {
        var context = response_inventory_model.Context("sibling");
        var command = response_inventory_model.Command(context);
        if (inventoryFirst) Assert.NotEmpty(context.GeneratedTypes);
        Assert.Equal("PlaceOrderResponse_", context.ResponseType(command));
        Assert.Contains(context.GeneratedTypes, type => type.Name == "PlaceOrderResponse_");
        Assert.Contains(context.GeneratedTypes, type => type == new GeneratedTypeName(string.Empty, "Program"));
        Assert.Equal("PlaceOrderResponse_", context.ResponseType(command));
    }

    [Fact]
    public void should_allocate_against_unselected_namespaces_and_keep_selected_file_imports_complete()
    {
        var whole = response_inventory_model.Context("namespace");
        var command = response_inventory_model.Command(whole);
        var located = whole.DeclaringSlice(command.Id);
        var request = whole.Request with { Scope = new(ArtifactRenderScopeKind.Slice, located.Slice.Id) };
        var selected = new SemanticApplicationContext(request, response_inventory_model.Options);
        Assert.Single(selected.SelectedSlices());
        Assert.Equal(whole.ResponseType(command), selected.ResponseType(command));
        Assert.Equal("PlaceOrderResponse_", selected.ResponseType(command));
        Assert.Equal(whole.GeneratedTypes, selected.GeneratedTypes);
        AssertResponseArtifacts(selected, SliceNaming.Namespace(selected.RootNamespace, located.Path), selected.ResponseType(command));
    }

    [Fact]
    public void should_not_depend_on_slice_or_command_traversal_order()
    {
        var whole = response_inventory_model.Context("namespace");
        var application = whole.Application;
        var reversed = application with
        {
            Modules = [.. application.Modules.Reverse().Select(module => module with
            {
                Features = [.. module.Features.Reverse().Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Reverse().Select(slice => slice with { Commands = [.. slice.Commands.Reverse()] })],
                    Features = [.. feature.Features.Reverse()]
                })]
            })]
        };
        var request = response_inventory_model.Request(ExecutableSemanticModel.Create(whole.Request.Model.LanguageVersion, whole.Request.Model.SemanticVersion, reversed));
        var context = new SemanticApplicationContext(request, response_inventory_model.Options);
        Assert.Equal(whole.ResponseType(response_inventory_model.Command(whole)), context.ResponseType(response_inventory_model.Command(context)));
        Assert.Equal(whole.GeneratedTypes.OrderBy(type => type.ToString()), context.GeneratedTypes.OrderBy(type => type.ToString()));
    }

    [Fact]
    public void should_retain_duplicate_allocated_identities_and_reject_the_owning_commands()
    {
        var source = response_inventory_model.SourceFor("sibling")
            .Replace("command PlaceOrderResponse", "command Place_Order", StringComparison.Ordinal)
            .Replace("returns title", "returns\n          label = title", StringComparison.Ordinal);
        var model = given.invoice_model.Compile(source);
        var context = new SemanticApplicationContext(response_inventory_model.Request(model), response_inventory_model.Options);
        var command = response_inventory_model.Command(context);
        Assert.Equal(2, context.GeneratedTypes.Count(type => type.Name == context.ResponseType(command)));
        var diagnostics = SemanticCratisAdmission.Evaluate(context, context.SelectedSlices());
        Assert.All(context.Commands.Keys, owner => Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-012" && diagnostic.Artifact == owner));
    }

    [Theory]
    [InlineData("NotAllocatedResponse", 0)]
    [InlineData("PlaceOrderResponse_", 1)]
    public void should_refuse_an_unallocated_name_or_wrong_arity(string name, int arity)
    {
        var context = response_inventory_model.Context("sibling");
        var command = response_inventory_model.Command(context);
        var ns = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(command.Id).Path);
        var reference = new TypeReference(ns, name, arity, ReferenceContext.Type);
        var request = new ImportRequest(ns + ".Specifications", GeneratedProjects.Specifications, context.GeneratedTypes, [reference]);
        Assert.Throws<CSharpImportRejected>(() => ImportResolver.Resolve(request));
    }

    static void AssertResponseArtifacts(SemanticApplicationContext context, string ns, string response)
    {
        var plan = new CratisArtifactRenderPlanner().Plan(context.Request);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var source = plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal))
            .Select(artifact => CSharpSyntaxTree.ParseText(Encoding.UTF8.GetString(artifact.Bytes.AsSpan())).GetRoot()).ToArray();
        var declaration = Assert.Single(source.SelectMany(root => root.DescendantNodes().OfType<RecordDeclarationSyntax>()), record => record.Identifier.ValueText == response);
        Assert.Equal(ns, declaration.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Single().Name.ToString());
        var specification = Assert.Single(source, root => root.DescendantNodes().OfType<MethodDeclarationSyntax>().Any(method => method.Identifier.ValueText.StartsWith("should_return_", StringComparison.Ordinal)));
        Assert.Contains(specification.DescendantNodes().OfType<IdentifierNameSyntax>(), identifier => identifier.Identifier.ValueText == response);
    }
}
