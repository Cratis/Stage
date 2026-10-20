// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Runtime;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxPersona;

public class when_resolving_the_audit_identity : given.a_persona_model
{
    IReadOnlyDictionary<string, string> _identity = null!;

    void Establish() => _principal = Create(Personas + "\n" + Menu);
    void Because() => _identity = new StageIdentity(new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = _principal! } }).Current();

    [Fact] void should_have_a_stable_subject() => _identity["subject"].ShouldEqual("persona:Shopper");
    [Fact] void should_have_the_display_name() => _identity["name"].ShouldEqual("Ada Lovelace");
}
