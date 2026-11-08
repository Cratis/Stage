// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Realization.for_ForwardedIdentitySigning;

public class when_naming_the_audience_and_key : Specification
{
    [Fact] void should_lower_case_the_application_name() => ForwardedIdentitySigning.Audience("LibraryApp").ShouldEqual("libraryapp-api");
    [Fact] void should_suffix_the_key_id_with_its_version() => ForwardedIdentitySigning.KeyId("LibraryApp").ShouldEqual("libraryapp-v1");
    [Fact] void should_collapse_every_other_character_to_one_dash() => ForwardedIdentitySigning.KeyId("Lending  Library.App").ShouldEqual("lending-library-app-v1");
    [Fact] void should_trim_leading_and_trailing_dashes() => ForwardedIdentitySigning.KeyId("_Lending_").ShouldEqual("lending-v1");
    [Fact] void should_keep_the_key_id_within_what_authproxy_accepts() => ForwardedIdentitySigning.KeyId(new string('a', 100)).Length.ShouldEqual(63);
    [Fact] void should_keep_the_audience_within_the_same_length() => ForwardedIdentitySigning.Audience(new string('a', 100)).Length.ShouldEqual(64);
    [Fact] void should_name_an_application_without_letters_or_digits() => ForwardedIdentitySigning.KeyId("--").ShouldEqual("application-v1");
}
