// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer;

public class when_invoking_root_string_lookups : given.a_root_string_key_slice
{
    [Theory]
    [InlineData("global")]
    [InlineData(" ._/:*+- AZaz09 ")]
    [InlineData("   ")]
    [InlineData(" blåbær_日本語_é ")]
    public async Task should_preserve_the_public_attribute_record_and_exact_forwarded_event_source_id(string value)
    {
        foreach (var query in new[] { "", "query OrderById => OrderReadModel", "query OrderById => OrderReadModel\n  by lookup String" })
        {
            Compile(Global.Replace("global", value, StringComparison.Ordinal), query);
            var file = Render();
            RenderedOutput.Errors([file]).ShouldBeEmpty();
            if (query.Length > 0)
            {
                RenderedOutput.Warnings([file]).ShouldBeEmpty();
            }

            var assembly = RenderedOutput.Load([file]);
            var model = assembly.GetType("OrdersApp.Sales.Orders.Summary.OrderReadModel", throwOnError: true)!;
            var attribute = Assert.Single(model.GetCustomAttributesData(), attribute => attribute.AttributeType.Name == "FromEventAttribute`1");

            // C# supplies the existing optional key/parentKey constructor defaults in metadata.
            attribute.ConstructorArguments.Count.ShouldEqual(2);
            attribute.ConstructorArguments.All(argument => argument.Value is null).ShouldBeTrue();
            file.Content.ShouldNotContain("constantKey:");
            file.Content.ShouldNotContain("[Key]");
            var argument = Assert.Single(attribute.NamedArguments);
            argument.MemberName.ShouldEqual("ConstantKey");
            argument.TypedValue.Value.ShouldEqual(value);
            model.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(property => property.Name).ShouldContainOnly("Total");
            Assert.Single(model.GetConstructors()).GetParameters().Select(parameter => parameter.Name).ShouldContainOnly("Total");
            var method = model.GetMethod(query.Length == 0 ? "OrderReadModelById" : "OrderById", BindingFlags.Public | BindingFlags.Static)!;
            // Declared as the event source id, not the string it is stored as: Arc validates the argument it
            // coerces to the declared parameter type, so a raw string parameter would reach the lookup
            // unvalidated (ARC0015). Exact forwarding of the literal is unchanged, asserted below.
            method.GetParameters()[1].ParameterType.ShouldEqual(typeof(EventSourceId));
            method.GetParameters()[1].Name.ShouldEqual(query.Contains("by lookup", StringComparison.Ordinal) ? "lookup" : "id");
            var readModels = Substitute.For<IReadModels>();
            await (Task)method.Invoke(null, [readModels, new EventSourceId(value)])!;
            var call = Assert.Single(readModels.ReceivedCalls());
            call.GetMethodInfo().Name.ShouldEqual(nameof(IReadModels.GetInstanceById));
            call.GetMethodInfo().GetGenericArguments().ShouldContainOnly(model);

            // The public client accepts ReadModelKey, implicitly converted from the emitted EventSourceId.
            var forwarded = Assert.IsType<ReadModelKey>(call.GetArguments()[0]);
            forwarded.Value.ShouldEqual(value);
            ((EventSourceId)forwarded).Value.ShouldEqual(value);
            file.Content.ShouldContain("EventSourceId " + method.GetParameters()[1].Name + ")");
        }
    }

    [Fact]
    public void should_keep_live_lookup_names_and_string_types_without_exercising_mongodb()
    {
        Compile(Global, "query LiveOrder => observable OrderReadModel\n  by lookup String");
        var file = Render();
        file.Content.ShouldContain("LiveOrder(IMongoCollection<OrderReadModel> collection, string lookup) => collection.ObserveById(lookup)");
        RenderedOutput.Errors([file]).ShouldBeEmpty();

        // The native SDK control owns the zero-warning gate for MongoDB reference unification.
    }
}
