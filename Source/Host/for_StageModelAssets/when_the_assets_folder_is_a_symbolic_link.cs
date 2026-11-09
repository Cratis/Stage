// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_StageModelAssets.given;
using Xunit;

namespace Cratis.Stage.Host.for_StageModelAssets;

public class when_the_assets_folder_is_a_symbolic_link : a_model_asset_host
{
    string _response = string.Empty;

    protected override bool AssetsFolderIsLink => true;

    async Task Because() => _response = await Raw("/assets/appsettings.json");

    // Platforms that cannot create links have nothing to refuse.
    [Fact] void should_not_serve_a_file_from_the_folder_it_points_at() => (!SymbolicLinksSupported || (_response.Contains(" 404 ") && !_response.Contains(SecretText, StringComparison.Ordinal))).ShouldBeTrue();
}
