// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Semantics;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_StageRoutes;

public class when_describing_same_named_commands : for_StageEndpointMapper.given.a_routed_model
{
    string _directory = null!;
    StageApplication _legacy = null!;
    LoadedSemanticModel _semantic = null!;

    async Task Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"stage-command-routes-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "model.play");
        await File.WriteAllTextAsync(path, Source);
        _legacy = await EventModelLoader.LoadStageApplicationFromPathAsync(path);
        _semantic = await SemanticModelLoader.LoadFromPathAsync(path);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_keep_routes_and_schemas_distinct_and_complete_in_both_engines(bool semantic)
    {
        if (semantic) MapSemanticModel(_legacy.EventModel, _semantic.Plan, Substitute.For<IAppendSemanticFacts>());
        else MapModel(_legacy.EventModel);
        var sceneRoutes = new StageSceneRoutes(_legacy.Scene, _app.Services, NullLogger.Instance);
        var routes = semantic ? StageRoutes.For(sceneRoutes, _semantic.Model) : StageRoutes.For(sceneRoutes, _legacy.EventModel);

        sceneRoutes.CommandRoutes.ContainsKey("Register").ShouldBeFalse();
        sceneRoutes.CommandRoutes.Count.ShouldEqual(1);
        routes.Commands.Count.ShouldEqual(4);
        routes.Commands.ContainsKey("Stage.Billing.Registration.Add.Register").ShouldBeTrue();
        routes.Commands.ContainsKey("Stage.Inventory.Registration.Add.Register").ShouldBeTrue();
        routes.CommandSchemas["Stage.Billing.Registration.Add.Register"].ShouldContain("invoiceNumber");
        routes.CommandSchemas["Stage.Inventory.Registration.Add.Register"].ShouldContain("quantity");
        routes.CommandSchemas.ContainsKey("Register").ShouldBeFalse();
        routes.Commands.Keys.Where(key => !routes.CommandSchemas.ContainsKey(key)).ShouldBeEmpty();
        sceneRoutes.CommandRoutes.Keys.Where(key => !routes.CommandSchemas.ContainsKey(key)).ShouldBeEmpty();
    }

    void Destroy() => Directory.Delete(_directory, true);

    const string Source = """
        module Billing
          feature Registration
            slice StateChange Add
              command Register
                invoiceNumber String
                produces InvoiceRegistered
                  invoiceNumber = invoiceNumber
              event InvoiceRegistered
                invoiceNumber String
        module Inventory
          feature Registration
            slice StateChange Add
              command Register
                quantity Int
                produces StockRegistered
                  quantity = quantity
              event StockRegistered
                quantity Int
        module Operations
          feature Status
            slice StateChange Refresh
              command Refresh
                reason String
                produces Refreshed
                  reason = reason
              event Refreshed
                reason String
        """;
}
