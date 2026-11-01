// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_expanding_outputs;

public class and_items_arrive_unordered : given.an_expansion
{
    DeclarativeOutputExpansion _forward = null!;
    DeclarativeOutputExpansion _sortedByName = null!;

    void Because()
    {
        var request = Request(Profile(Archive()));
        _forward = Expand(request);
        _sortedByName = Expand(request, Manifest.Replace("model.items.commands", "array.sort model.items.commands 'name'", StringComparison.Ordinal));
    }

    [Fact] void should_follow_the_manifest_order() => Names(_forward).Select(_ => _.Split(':')[0]).ShouldEqual(["command", "command", "reaction", "index"]);
    [Fact] void should_order_items_by_ordinal_id() => _forward.Outputs.Where(_ => _.Output.Id == "command").Select(_ => _.ItemId).ShouldEqual(_forward.Outputs.Where(_ => _.Output.Id == "command").Select(_ => _.ItemId).Order(StringComparer.Ordinal));
    [Fact] void should_not_depend_on_the_arrival_order() => Names(_sortedByName).ShouldEqual(Names(_forward));
    [Fact] void should_not_refuse() => _forward.Refusal.ShouldBeNull();
    [Fact] void should_give_the_single_output_no_item() => _forward.Outputs.Single(_ => _.Output.Id == "index").ItemId.ShouldBeNull();
    [Fact] void should_scope_the_items_with_the_model_and_item() => _forward.Outputs[0].Scope.GetProperty("item").GetProperty("id").GetString().ShouldEqual(_forward.Outputs[0].ItemId);
    [Fact] void should_scope_the_single_output_with_only_the_model() => _forward.Outputs.Single(_ => _.Output.Id == "index").Scope.TryGetProperty("item", out _).ShouldBeFalse();
}
