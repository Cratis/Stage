// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer;

public class when_rendering_an_inline_return_type_declared_in_the_model : state_change_slices
{
    [Theory]
    [InlineData("InvoiceRegistered")]
    [InlineData("CratisApp.Billing.Invoices.RegisterInvoice.InvoiceRegistered")]
    [InlineData("global::CratisApp.Billing.Invoices.RegisterInvoice.InvoiceRegistered")]
    public void should_resolve_the_event_from_another_slice_without_warning(string typeName)
    {
        var command = _processBatch.Slice.Commands.Single();
        command = command with { Handler = command.Handler! with { Code = command.Handler.Code! with { Code = $"return new {typeName}(string.Empty);" } } };
        var slice = _processBatch with { Slice = _processBatch.Slice with { Commands = [command] } };

        var file = new StateChangeSliceRenderer().Render(slice, _applicationSet, "CratisApp");

        AssertReturnBinding(file, "CratisApp.Billing.Invoices.RegisterInvoice.InvoiceRegistered", new RenderedFile("Event.cs", "namespace CratisApp.Billing.Invoices.RegisterInvoice; public record InvoiceRegistered(string Value);"));
        file.Content.ShouldContain(command.Handler.Code.Code);
        file.Diagnostics.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("UncatalogedResult")]
    [InlineData("Uncataloged.Result")]
    [InlineData("Uncataloged.EventForEventSourceId")]
    public void should_refuse_an_unproved_generated_signature_without_rewriting_its_authored_expression(string typeName)
    {
        var command = _processBatch.Slice.Commands.Single();
        var body = $"return new {typeName}(string.Empty);";
        command = command with { Handler = command.Handler! with { Code = command.Handler.Code! with { Code = body } } };
        var slice = _processBatch with { Slice = _processBatch.Slice with { Commands = [command] } };
        Assert.Throws<CSharpImportRejected>(() => new StateChangeSliceRenderer().Render(slice, _applicationSet, "CratisApp"));
        Assert.Equal(body, command.Handler.Code.Code);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_bind_a_qualified_event_identity_when_another_slice_declares_the_same_short_name(bool reverse)
    {
        const string identity = "CratisApp.Billing.Invoices.RegisterInvoice.InvoiceRegistered";
        var original = InlineSlice($"global::{identity}");
        var slice = original with { Slice = original.Slice with { Events = _registerInvoice.Slice.Events } };
        var file = new StateChangeSliceRenderer().Render(slice, OrderedApplications(reverse), "CratisApp");
        AssertReturnBinding(file, identity, new RenderedFile("RemoteEvent.cs", "namespace CratisApp.Billing.Invoices.RegisterInvoice; public record InvoiceRegistered(string Value);"));
        Assert.Contains(slice.Slice.Commands.Single().Handler!.Code!.Code, file.Content);
        Assert.Empty(file.Diagnostics);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_refuse_an_ambiguous_bare_event_identity_independent_of_slice_order(bool reverse)
    {
        var original = InlineSlice("InvoiceRegistered");
        var slice = original with { Slice = original.Slice with { Events = _registerInvoice.Slice.Events } };
        Assert.Throws<CSharpImportRejected>(() => new StateChangeSliceRenderer().Render(slice, OrderedApplications(reverse), "CratisApp"));
        Assert.Equal("return new InvoiceRegistered(string.Empty);", slice.Slice.Commands.Single().Handler!.Code!.Code);
    }

    [Fact]
    public void should_accept_repeated_candidates_of_the_same_canonical_identity()
    {
        var command = InlineSlice("InvoiceRegistered").Slice.Commands.Single();
        var slice = _registerInvoice with { Slice = _registerInvoice.Slice with { Commands = [command] } };
        var file = new StateChangeSliceRenderer().Render(slice, _applicationSet, "CratisApp");
        AssertReturnBinding(file, "CratisApp.Billing.Invoices.RegisterInvoice.InvoiceRegistered");
        Assert.Contains(command.Handler!.Code!.Code, file.Content);
        Assert.Empty(file.Diagnostics);
    }

    internal static void AssertReturnBinding(RenderedFile file, string metadataName, params RenderedFile[] declarations)
    {
        var compilation = RenderedOutput.CreateCompilation([file, .. declarations]);
        var tree = compilation.SyntaxTrees.Single(tree => tree.FilePath == file.RelativePath);
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "Handle");
        var model = compilation.GetSemanticModel(tree);
        var expected = compilation.GetTypeByMetadataName(metadataName);
        Assert.NotNull(expected);
        Assert.True(SymbolEqualityComparer.Default.Equals(expected, model.GetTypeInfo(method.ReturnType).Type));
        Assert.Equal(CandidateReason.None, model.GetSymbolInfo(method.ReturnType).CandidateReason);
        Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.Location.SourceTree == tree && diagnostic.Location.SourceSpan.IntersectsWith(method.ReturnType.Span)));
        Assert.DoesNotContain(method.ReturnType.DescendantNodesAndSelf().OfType<AliasQualifiedNameSyntax>(), alias => alias.Alias.Identifier.ValueText == "global");
    }

    LocatedSlice InlineSlice(string typeName)
    {
        var command = _processBatch.Slice.Commands.Single();
        command = command with { Handler = command.Handler! with { Code = command.Handler.Code! with { Code = $"return new {typeName}(string.Empty);" } } };
        return _processBatch with { Slice = _processBatch.Slice with { Commands = [command] } };
    }

    ApplicationSet OrderedApplications(bool reverse)
    {
        if (!reverse) return _applicationSet;
        var application = _applicationSet.Applications.Single();
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var reordered = new ApplicationSet([application with { Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Reverse()] }] }] }]);
        Assert.False(_applicationSet.Slices.Select(slice => slice.Slice.Name).SequenceEqual(reordered.Slices.Select(slice => slice.Slice.Name)));
        return reordered;
    }
}
