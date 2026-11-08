// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Cratis.Stage.Host.for_StageModelAssets.given;
using Xunit;

namespace Cratis.Stage.Host.for_StageModelAssets;

public class when_the_model_has_no_assets_folder : a_model_asset_host
{
    HttpResponseMessage _logo = null!;

    protected override bool HasAssets => false;

    async Task Because() => _logo = await _client.GetAsync("/assets/logo.svg");

    [Fact] void should_answer_not_found() => _logo.StatusCode.ShouldEqual(HttpStatusCode.NotFound);
}
