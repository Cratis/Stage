// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticHttpBinding.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHttpBinding.when_posting;

public class without_authorization : a_bound_semantic_model
{
    int _status;

    async Task Because() => (_status, _, _) = await Request("POST", "/api/projects/registration/register-project/restricted-project", Payload);

    [Fact] void should_forbid_the_caller() => _status.ShouldEqual(403);
}
