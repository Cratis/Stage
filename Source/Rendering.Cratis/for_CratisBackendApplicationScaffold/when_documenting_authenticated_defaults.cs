// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

public class when_documenting_authenticated_defaults : given.a_current_scaffold
{
    string _readme = string.Empty;

    void Because() => _readme = Content("README.md");

    [Fact] void should_document_the_authenticated_default() => _readme.ShouldContain("STAGE-ESM-054");
    [Fact] void should_document_the_inferred_system_identity() => _readme.ShouldContain("STAGE-ESM-055");
    [Fact] void should_show_the_explicit_anonymous_policy() => _readme.ShouldContain("policy Anyone\n  require anonymous");
    [Fact] void should_show_how_to_reopen_an_endpoint() => _readme.ShouldContain("authorize Anyone");
    [Fact] void should_document_the_anonymous_diagnostic() => _readme.ShouldContain("STAGE-ESM-060");
    [Fact] void should_document_the_anonymous_semantic_version() => _readme.ShouldContain("ESM v31");
    [Fact] void should_not_recommend_a_guest_or_staff_workaround() => _readme.ShouldNotContain("authorize Guests or Staff");
    [Fact] void should_keep_authentication_application_owned() => _readme.ShouldContain("Customizations/");
    [Fact] void should_not_install_a_fallback_policy() => Content("Program.cs").ShouldNotContain("FallbackPolicy");
    [Fact] void should_not_trust_forwarded_principals_by_default() => Content("appsettings.json").ShouldNotContain("TrustForwardedIdentityHeaders");
}
