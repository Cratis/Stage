// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader;

public class when_loading_a_plain_source : given.a_source_tree
{
    async Task Because() => _result = await SemanticModelLoader.LoadAsync(_root, ["nested/source.play"], null, "Shop");
    [Fact] void should_load_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_compile_the_source() => _result.Loaded!.Plan.Commands.Count.ShouldEqual(1);
}
#endif
