// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Diagnostics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_ModelAssets;

public class when_loading_asset_warnings : for_SemanticModelLoader.given.a_source_tree
{
    void Establish()
    {
        var assets = Path.Combine(_root, "assets");
        Directory.CreateDirectory(assets);
        File.WriteAllBytes(Path.Combine(assets, "oversized.bin"), new byte[(5 * 1024 * 1024) + 1]);
        File.WriteAllText(Path.Combine(assets, "index-abc.js"), "bundle collision");
        File.WriteAllText(Path.Combine(assets, "logo.svg"), "<svg/>");
    }

    async Task Because() => _result = await SemanticModelLoader.LoadAsync(_root, [], null, "AssetWarnings");

    [Fact] void should_not_block_loading() => _result.Success.ShouldBeTrue();
    [Fact] void should_omit_refused_files() => _result.Loaded!.ModelAssets.Files.Keys.ShouldEqual(["logo.svg"]);
    [Fact] void should_append_warnings_to_the_loaded_model() => _result.Loaded!.Diagnostics.Where(diagnostic => diagnostic.Code.StartsWith("STAGE-ASSET-", StringComparison.Ordinal)).Select(diagnostic => diagnostic.Severity).ShouldEqual([DiagnosticSeverity.Warning, DiagnosticSeverity.Warning]);
}
#endif
