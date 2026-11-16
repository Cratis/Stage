// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_ModelAssets;

public class when_assets_are_absent : given.an_asset_directory
{
    void Establish() => Directory.Delete(_assets);
    void Because() => _result = ModelAssets.Read(_root);
    [Fact] void should_be_empty() => _result.ShouldEqual(ModelAssets.None);
}
#endif
