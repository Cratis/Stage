// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticReadModelBrowser.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticReadModelBrowser.when_browsing;

public class with_a_journey_replay_on_a_refused_model : a_browser
{
    HttpStatusCode _status;
    string _body = string.Empty;

    protected override bool Refused => true;

    async Task Because()
    {
        using var response = await _client.PostAsync(StageJourneyReplay.Route, new StringContent("""{ "schema": "stage-journey/v1", "journeys": [] }""", Encoding.UTF8, "application/json"));
        _status = response.StatusCode;
        _body = await response.Content.ReadAsStringAsync();
    }

    [Fact] void should_answer_service_unavailable() => _status.ShouldEqual(HttpStatusCode.ServiceUnavailable);
    [Fact] void should_carry_the_refusal() => _body.ShouldContain("The model is not admitted.");
}
