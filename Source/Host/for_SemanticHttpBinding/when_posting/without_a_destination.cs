// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticHttpBinding.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHttpBinding.when_posting;

public class without_a_destination : a_bound_semantic_model
{
    int _status;
    string _body = null!;

    async Task Because() => (_status, _body, _) = await Request("POST", "/api/projects/registration/register-project/unallocated-project", Payload);

    [Fact] void should_report_not_implemented() => _status.ShouldEqual(501);
    [Fact] void should_report_identity_allocation() => _body.ShouldContain("IdentityAllocation");
}
