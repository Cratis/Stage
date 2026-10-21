// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxIdentity.when_selecting_per_operation;

/// <summary>
/// Arc's frontend asks <c>/.cratis/me</c> who the page is. It is not under /api, and answered 401 for the page of every
/// started Stage because nothing gave it a persona (Cratis/Stage#711): the page is whoever the Stage plays.
/// </summary>
public class on_the_identity_of_the_page : given.a_started_multimodule_host
{
    async Task Because() => await Send("GET", "/.cratis/me");

    [Fact] void should_use_the_default_persona() => _persona.ShouldEqual("persona:Shopper");
    [Fact] void should_return_the_expected_status() => _status.ShouldEqual(StatusCodes.Status200OK);
}
