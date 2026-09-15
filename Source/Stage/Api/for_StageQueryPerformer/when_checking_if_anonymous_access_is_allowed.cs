// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Api.for_StageQueryPerformer.given;
using Xunit;

namespace Cratis.Stage.Api.for_StageQueryPerformer;

public class when_checking_if_anonymous_access_is_allowed : a_stage_query_performer
{
    bool _allowsAnonymousAccess;

    void Because() => _allowsAnonymousAccess = _performer.AllowsAnonymousAccess;

    [Fact] void should_allow_it_behind_studios_authenticated_proxy() => _allowsAnonymousAccess.ShouldBeTrue();
}
