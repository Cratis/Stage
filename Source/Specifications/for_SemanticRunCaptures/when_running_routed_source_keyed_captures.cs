// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticRunCaptures;

public class when_running_routed_source_keyed_captures : Specification
{
    const string Model = """
        eventsource Order
          identifier String
          stream Shipping
            streamId String
        module Sales
          feature Orders
            slice Translate ReceiveOrders
              direction inbound
              event OrderPlaced from "storefront"
                orderId String
              event OrderReceived
                orderId String
              capture OrderIntake
                source events
                  from OrderPlaced
                key orderId is event source
                append OrderReceived
                  orderId = $.orderId
              specification ReceivingAnOrder
                when append OrderPlaced
                  for "order-1"
                  stream Order.Shipping
                    streamId = "dispatch"
                  orderId = "order-1"
                then OrderReceived
                  for "order-1"
                  orderId = "order-1"
        """;

    [Theory]
    [InlineData("wrong")]
    [InlineData("order-1")]
    public async Task should_validate_routed_delivery_without_inferring_the_source(string? source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("RoutedCapture"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "model.play", Model);
        var compiled = new SemanticModelCompiler().Compile("RoutedCapture", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compiled.Success, string.Join(';', compiled.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var model = compiled.Value!.Model;
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var specification = slice.Specifications.Single();
        specification = specification with
        {
            WhenAppended = specification.WhenAppended! with { EventSource = source is null ? null : new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), SemanticValue.Text(source)) }
        };
        var application = model.Application with { Modules = [module with { Features = [feature with { Slices = [slice with { Specifications = [specification] }] }] }] };
        var plan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application)).Plan!;
        var expected = new SemanticSpecificationRunner().Run(plan, specification.Id);
        var record = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
        if (source == "order-1")
        {
            expected.Passed.ShouldBeTrue();
            Assert.True(record.Outcome == SemanticSpecificationOutcome.Passed, $"{record.Unsupported?.Details} {string.Join(';', record.Failures)}");
        }
        else
        {
            var rejected = Assert.IsType<SemanticRejected>(expected.Execution);
            rejected.Category.ShouldEqual(SemanticRejectionCategory.Contract);
            record.Trace!.Rejection.ShouldEqual(rejected.Details);
            record.Trace.Facts.ShouldBeEmpty();
        }
    }
}
