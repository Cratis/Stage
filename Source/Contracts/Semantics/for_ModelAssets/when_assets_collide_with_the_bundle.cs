// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_ModelAssets;

public class when_assets_collide_with_the_bundle : given.an_asset_directory
{
    void Establish()
    {
        Write("index-abc123.js", [1]);
        Write("index-abc123.css.map", [1]);
        Write("vendor-abc123.js.map", [1]);
        Write("nested/index-abc123.js", [1]);
    }

    void Because() => _result = ModelAssets.Read(_root);
    [Fact] void should_omit_only_root_bundle_names() => _result.Files.Keys.ShouldEqual(["nested/index-abc123.js"]);
    [Fact] void should_warn_of_bundle_collisions() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(["STAGE-ASSET-002", "STAGE-ASSET-002", "STAGE-ASSET-002"]);
}
#endif
