// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Projections;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Runtime.for_StageRuntimeRegistrar;

public class when_registering_an_invalid_projection
{
    [Fact]
    public async Task should_fault_before_resolving_a_client_or_writing_remotely()
    {
        var model = EventModelLoader.LoadFromSource("""
            module Catalog
              feature Items
                slice StateView Summary
                  projection Summary => SummaryModel
                    from ItemRegistered key id
                      id = id
            """);
        var collection = model.Collections.Single();
        var module = collection.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var readModel = slice.ReadModel!;
        var projection = readModel.Projection!;
        var invalid = projection with { From = new Dictionary<string, FromDefinition>
        {
            ["ItemRegistered"] = projection.From.Single().Value with { Key = "$composite(OrderKey, region=$value(us-east), id=id)" }
        } };
        var invalidModel = model with { Collections = [collection with { Modules = [module with { Features = [feature with
        {
            Slices = [slice with { ReadModel = readModel with { Projection = invalid } }]
        }] }] }] };
        var services = Substitute.For<IServiceProvider>();

        await Assert.ThrowsAsync<UnsupportedProjectionRuntimeExpression>(() =>
            StageRuntimeRegistrar.RegisterAsync(services, "event-store", invalidModel, NullLogger.Instance));
        services.DidNotReceive().GetService(Arg.Any<Type>());
    }
}
#endif
