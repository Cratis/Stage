// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_expanding_outputs;

public class and_when_is_false : given.an_expansion
{
    DeclarativeOutputExpansion _expansion = null!;

    void Because() => _expansion = Expand(
        Request(Profile(Archive())),
        Manifest.Replace("\"forEach\":\"model.items.commands\"", "\"forEach\":\"model.items.commands\",\"when\":\"item.name == 'Echo'\"", StringComparison.Ordinal)
            .Replace("\"id\":\"index\"", "\"id\":\"index\",\"when\":\"false\"", StringComparison.Ordinal));

    [Fact] void should_keep_only_the_included_item() => Names(_expansion).Count(_ => _.StartsWith("command:", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_skip_the_single_output() => Names(_expansion).ShouldNotContain("index:");
    [Fact] void should_not_refuse() => _expansion.Refusal.ShouldBeNull();
}
