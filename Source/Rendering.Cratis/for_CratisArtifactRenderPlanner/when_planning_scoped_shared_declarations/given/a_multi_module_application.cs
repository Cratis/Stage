// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_scoped_shared_declarations.given;

public class a_multi_module_application : Specification
{
    protected ExecutableSemanticModel _model = null!;
    protected SemanticModule _customers = null!;
    protected SemanticModule _sales = null!;
    protected SemanticSlice _placeOrder = null!;
    protected SemanticSlice _customerLookup = null!;
    protected ArtifactRenderPlan _application = null!;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Shop"));
        var documents = new[]
        {
            SemanticSourceDocument.Create(catalog.ResolveDocument("customers"), "customers", "Customers.play", """
                concept CustomerId : Uuid
                concept PostalCode : String
                concept CustomerRegion : String
                type Address
                  postalCode PostalCode
                type ShippingAddress
                  address Address
                policy Guests
                  require not authenticated
                module Customers
                  feature Registration
                    slice StateChange Register
                      command RegisterCustomer
                        authorize Guests
                        customerId CustomerId identifier
                        region CustomerRegion
                        produces CustomerRegistered
                          for customerId
                          customerId = customerId
                          region = region
                      event CustomerRegistered
                        customerId CustomerId
                        region CustomerRegion
                """),
            SemanticSourceDocument.Create(catalog.ResolveDocument("sales"), "sales", "Sales.play", """
                concept Money : Decimal
                concept Unused : String
                type UnusedType
                  value Unused
                policy Staff
                  require role "Staff"
                module Sales
                  feature Orders
                    slice StateChange PlaceOrder
                      command PlaceOrder
                        authorize Staff
                        orderId String identifier
                        customerId CustomerId
                        amount Money
                        shipTo ShippingAddress
                        produces OrderPlaced
                          for orderId
                          amount = amount
                      event OrderPlaced
                        amount Money
                    slice StateView CustomerLookup
                      readmodel CustomerSummary
                        customerId CustomerId
                      query CustomerById => CustomerSummary?
                        by customerId CustomerId
                      projection CustomerSummaryProjection => CustomerSummary
                        from CustomerRegistered key customerId
                          customerId = customerId
                """)
        };
        var compilation = new SemanticModelCompiler().Compile("Shop", SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        _model = compilation.Value!.Model;
        _customers = _model.Application.Modules.Single(module => module.Name == "Customers");
        _sales = _model.Application.Modules.Single(module => module.Name == "Sales");
        _placeOrder = _sales.Features.Single().Slices.Single(slice => slice.Name == "PlaceOrder");
        _customerLookup = _sales.Features.Single().Slices.Single(slice => slice.Name == "CustomerLookup");
        _application = Plan(new(ArtifactRenderScopeKind.Application, _model.Application.Id));
        Assert.True(_application.Success, string.Join(Environment.NewLine, _application.Diagnostics));
    }

    protected ArtifactRenderPlan Plan(ArtifactRenderScope scope) =>
        CratisRendering.Plan(_model, SemanticExecutionPlan.Compile(_model).Plan!, scope, new("Shop", "Shop"));

    protected static bool SameArtifact(ArtifactRenderPlan application, ArtifactRenderPlan scoped, string path)
    {
        var expected = application.Artifacts.Single(artifact => artifact.RelativePath == path);
        var actual = scoped.Artifacts.Single(artifact => artifact.RelativePath == path);
        return actual.Bytes.SequenceEqual(expected.Bytes) && actual.Sources.SequenceEqual(expected.Sources);
    }

    protected static IEnumerable<RenderedFile> Files(ArtifactRenderPlan plan) =>
        plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, Text(artifact)));

    protected static string Text(PlannedArtifact artifact) => Encoding.UTF8.GetString(artifact.Bytes.AsSpan());
}
