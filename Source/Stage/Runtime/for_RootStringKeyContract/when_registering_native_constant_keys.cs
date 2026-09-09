// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Serialization;
using Cratis.Specifications;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

using NativeProjections = Cratis.Chronicle.Projections.Projections;

namespace Cratis.Stage.Runtime.for_RootStringKeyContract;

public class when_registering_native_constant_keys : Specification
{
    [Theory]
    [InlineData(typeof(RootStringKeyContractGlobal), "global")]
    [InlineData(typeof(RootStringKeyContractPunctuation), " /tenant:global_*+.- ")]
    [InlineData(typeof(RootStringKeyContractSpace), " ")]
    [InlineData(typeof(RootStringKeyContractUnicode), "æ漢字é١")]
    public async Task should_preserve_the_full_constant_in_the_public_registration_definition(Type model, string value)
    {
        model.GetCustomAttributes().OfType<IFromEventAttribute>().Single().ConstantKey.ShouldEqual(value);
        model.GetProperties().Select(property => property.Name).ShouldContainOnly("Status");

        var definition = await BuildDefinition(model, typeof(RootStringKeyContractUpdated));

        definition.From.Single().Value.Key.ShouldEqual($"$value({value})");
        ((EventSourceId)value).Value.ShouldEqual(value);
    }

    [Fact]
    public async Task should_characterize_the_pinned_targets_ignored_constant_on_a_propertyless_record()
    {
        var model = typeof(RootStringKeyContractEmpty);
        model.GetCustomAttributes().OfType<IFromEventAttribute>().Single().ConstantKey.ShouldEqual("global");
        model.GetProperties().ShouldBeEmpty();
        model.GetConstructors().Single().GetParameters().ShouldBeEmpty();

        // This static fixture characterizes Chronicle 16.38.2, not acceptance of rendered output.
        var definition = await BuildDefinition(model, typeof(RootStringKeyContractUpdated));

        definition.From.Single().Value.Key.ShouldEqual(WellKnownExpressions.EventSourceId);
        definition.From.Single().Value.Key.ShouldNotEqual("$value(global)");
    }

    static async Task<ProjectionDefinition> BuildDefinition(Type model, params Type[] events)
    {
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        var services = Substitute.For<IServices>();
        ((IChronicleServicesAccessor)connection).Services.Returns(services);
        var store = Substitute.For<IEventStore>();
        store.Connection.Returns(connection);
        store.Name.Returns(new EventStoreName("stage-root-string-key-contract"));
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.ModelBoundProjections.Returns([model]);
        var eventTypes = Substitute.For<IEventTypes>();
        foreach (var eventType in events)
        {
            eventTypes.GetEventTypeFor(eventType).Returns(new EventType(eventType.Name, EventTypeGeneration.First));
        }

        var projections = new NativeProjections(store, eventTypes, artifacts, new DefaultNamingPolicy(), Substitute.For<IClientArtifactsActivator>(), new JsonSerializerOptions(), NullLogger<NativeProjections>.Instance);
        RegisterRequest? registration = null;

        // Capture the public service boundary; no kernel or transport is invoked.
        services.Projections.Register(Arg.Do<RegisterRequest>(request => registration = request)).Returns(Task.CompletedTask);
        await projections.Discover();
        await projections.Register();
        registration.ShouldNotBeNull();
        return Assert.Single(registration!.Projections);
    }
}
#endif
