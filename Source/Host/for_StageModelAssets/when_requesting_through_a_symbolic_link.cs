// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_StageModelAssets.given;
using Xunit;

namespace Cratis.Stage.Host.for_StageModelAssets;

public class when_requesting_through_a_symbolic_link : a_model_asset_host
{
    string _file = string.Empty;
    string _folder = string.Empty;
    HttpResponseMessage _logo = null!;

    async Task Because()
    {
        _file = await Raw("/assets/escape.play");
        _folder = await Raw("/assets/escape-folder/model.play");
        _logo = await _client.GetAsync("/assets/logo.svg");
    }

    // Platforms that cannot create links have nothing to refuse, so those facts pass without a link in place.
    [Fact] void should_not_serve_a_linked_file_outside_the_folder() => (!SymbolicLinksSupported || !_file.Contains(SecretText, StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_serve_a_file_below_a_linked_folder() => (!SymbolicLinksSupported || !_folder.Contains(SecretText, StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_serving_regular_files() => _logo.IsSuccessStatusCode.ShouldBeTrue();
}
