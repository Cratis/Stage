// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Xunit;

namespace Cratis.Stage.Api.for_StageModelWalker;

public class when_locating_a_nested_slice : Specification
{
    LocatedSlice _located = null!;
    Slice _slice = null!;
    Slice _deconstructedSlice = null!;
    IReadOnlyList<string> _legacyLocation = [];
    string _namespace = string.Empty;

    void Establish()
    {
        _slice = new Slice(Guid.NewGuid(), "Place Order", SliceType.StateChange, [], null, null, []);
    }

    void Because()
    {
        var nested = new Feature(Guid.NewGuid(), "Packing", null, [], [_slice]);
        var feature = new Feature(Guid.NewGuid(), "Checkout", null, [nested], []);
        var module = new Module(Guid.NewGuid(), Guid.Empty, Guid.Empty, "Orders", [feature]);
        var model = new EventModel(Guid.NewGuid(), "Routes", [new ModuleCollection(Guid.NewGuid(), Guid.Empty, [module])]);
        _located = StageModelWalker.Slices(model).Single();
        (_deconstructedSlice, _legacyLocation, _namespace) = _located;
    }

    [Fact] void should_preserve_the_positional_slice() => ReferenceEquals(_deconstructedSlice, _slice).ShouldBeTrue();
    [Fact] void should_preserve_the_legacy_positional_location_without_the_slice() => _legacyLocation.SequenceEqual(["Stage", "Orders", "Checkout", "Packing"]).ShouldBeTrue();
    [Fact] void should_preserve_the_emitted_type_namespace() => _namespace.ShouldEqual("Stage.Orders.Checkout.Packing.PlaceOrder");
    [Fact] void should_add_the_sanitized_slice_only_to_the_canonical_location() => _located.CanonicalLocation.SequenceEqual(["Stage", "Orders", "Checkout", "Packing", "PlaceOrder"]).ShouldBeTrue();
}
