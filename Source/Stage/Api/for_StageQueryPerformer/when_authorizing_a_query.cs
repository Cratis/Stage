// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Specifications;
using Cratis.Stage.Api.for_StageQueryPerformer.given;
using Xunit;

namespace Cratis.Stage.Api.for_StageQueryPerformer;

public class when_authorizing_a_query : a_stage_query_performer
{
    bool _isAuthorized;

    void Because() => _isAuthorized = _performer.IsAuthorized(QueryContext.NotSet);

    [Fact] void should_authorize_it_in_the_sandbox_the_session_owns() => _isAuthorized.ShouldBeTrue();
}
