// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Runtime;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxPersona;

public class when_policies_include_an_audit_subject : given.a_persona_model
{
    IReadOnlyDictionary<string, string> _identity = null!;

    void Establish() => _principal = Create("""
        policy IsShopper
          require claim "sub" matches "other-subject"
        persona Shopper
          policy IsShopper
        """);

    void Because() => _identity = new StageIdentity(new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = _principal! } }).Current();

    [Fact] void should_keep_the_stable_audit_subject() => _identity["subject"].ShouldEqual("persona:Shopper");
}
