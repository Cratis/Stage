// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Xunit;

namespace Cratis.Stage.Host.for_StageHttpSurface;

public class when_planning_in_reversed_order : Specification
{
    StageHttpSurface _forward = null!;
    StageHttpSurface _reverse = null!;

    void Because()
    {
        var source = EventModelLoader.LoadFromSource(given.RouteModels.SameNamedCommands);
        var original = source.Collections[0].Modules[0].Features[0];
        var model = given.RouteModels.WithFeatures(original,
            given.RouteModels.Feature("Unique", [given.RouteModels.Command("First", "One"), given.RouteModels.Command("Second", "Two")]),
            given.RouteModels.Feature("Reports", [given.RouteModels.Query("First"), given.RouteModels.Query("Second")]),
            given.RouteModels.Feature("Singleton", [given.RouteModels.Command("Only")]));
        _forward = StageHttpSurface.Create(model);
        _reverse = StageHttpSurface.Create(given.RouteModels.Reverse(model));
    }

    [Fact] void should_order_operations_identically() => _forward.Operations.SequenceEqual(_reverse.Operations).ShouldBeTrue();
    [Fact] void should_make_identical_alias_decisions() => _forward.Operations.All(operation => _forward.AliasFor(operation.Method, operation.CanonicalPath) == _reverse.AliasFor(operation.Method, operation.CanonicalPath)).ShouldBeTrue();
    [Fact] void should_retain_unique_aliases() => _forward.Operations.Any(operation => _forward.AliasFor(operation.Method, operation.CanonicalPath) is not null).ShouldBeTrue();
    [Fact] void should_omit_ambiguous_aliases() => _forward.Operations.Where(operation => operation.LegacyPath.StartsWith("/api/orders/checkout/", StringComparison.Ordinal)).All(operation => _forward.AliasFor(operation.Method, operation.CanonicalPath) is null).ShouldBeTrue();
}
