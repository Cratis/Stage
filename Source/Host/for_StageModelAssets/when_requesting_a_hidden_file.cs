// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Cratis.Stage.Host.for_StageModelAssets.given;
using Xunit;

namespace Cratis.Stage.Host.for_StageModelAssets;

public class when_requesting_a_hidden_file : a_model_asset_host
{
    HttpResponseMessage _hidden = null!;

    async Task Because() => _hidden = await _client.GetAsync("/assets/.hidden.svg");

    [Fact] void should_answer_not_found() => _hidden.StatusCode.ShouldEqual(HttpStatusCode.NotFound);
}
