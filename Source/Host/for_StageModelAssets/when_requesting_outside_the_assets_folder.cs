// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_StageModelAssets.given;
using Xunit;

namespace Cratis.Stage.Host.for_StageModelAssets;

public class when_requesting_outside_the_assets_folder : a_model_asset_host
{
    string _plain = null!;
    string _encoded = null!;
    string _doubleEncoded = null!;
    string _backslash = null!;
    string _model = null!;
    string _directory = null!;
    HttpResponseMessage _app = null!;

    async Task Because()
    {
        _plain = await Raw("/assets/../model.play");
        _encoded = await Raw("/assets/%2e%2e/model.play");
        _doubleEncoded = await Raw("/assets/%252e%252e/model.play");
        _backslash = await Raw("/assets/..%5cmodel.play");
        _model = await Raw("/model.play");
        _directory = await Raw("/assets/nested/");
        _app = await _client.GetAsync("/index.html");
    }

    [Fact] void should_not_serve_a_plain_traversal() => _plain.ShouldNotContain(SecretText);
    [Fact] void should_not_serve_an_encoded_traversal() => _encoded.ShouldNotContain(SecretText);
    [Fact] void should_not_serve_a_double_encoded_traversal() => _doubleEncoded.ShouldNotContain(SecretText);
    [Fact] void should_not_serve_a_backslash_traversal() => _backslash.ShouldNotContain(SecretText);
    [Fact] void should_not_serve_the_model_source() => _model.ShouldNotContain(SecretText);
    [Fact] void should_not_list_directories() => _directory.ShouldNotContain("mark.svg");
    [Fact] void should_leave_the_application_files_unaffected() => _app.IsSuccessStatusCode.ShouldBeTrue();
}
