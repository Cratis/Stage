// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticHttpBinding.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHttpBinding.when_refused;

public class with_event_contract_blocker : a_bound_semantic_model
{
    int _status;
    string _body = null!;
    int _statusCode;
    string _statusBody = null!;

    async Task Because()
    {
        SemanticHost.MapRefused(_app, [new StageUnsupportedIssue("EventContract", "project-registered", "Revision is unsupported.")]);
        BuildRouting();
        (_status, _body, _) = await Request("GET", "/api/refused");
        (_statusCode, _statusBody, _) = await Request("GET", "/stage/status");
    }

    [Fact] void should_return_unsupported() => _status.ShouldEqual(501);
    [Fact] void should_preserve_the_capability() => _body.ShouldContain("Unsupported(EventContract)");
    [Fact] void should_report_unsupported_status() => _statusCode.ShouldEqual(200);
    [Fact] void should_report_the_typed_issue() => _statusBody.ShouldContain("EventContract");
}
