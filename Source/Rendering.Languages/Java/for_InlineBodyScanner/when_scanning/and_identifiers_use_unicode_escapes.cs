// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_InlineBodyScanner.when_scanning;

public class and_identifiers_use_unicode_escapes : given.a_native_body
{
    void Establish() => _body = "\\u0053tring value; int \\u0063om = 0; Δelta 𐐀name;";
    void Because() => Scan();

    [Fact] void should_reserve_the_decoded_type() => _identifiers.Contains("String").ShouldBeTrue();
    [Fact] void should_reserve_the_decoded_package_obscuring_local() => _identifiers.Contains("com").ShouldBeTrue();
    [Fact] void should_recognize_unicode_identifiers() => _identifiers.Contains("Δelta").ShouldBeTrue();
    [Fact] void should_recognize_supplementary_identifiers() => _identifiers.Contains("𐐀name").ShouldBeTrue();
}
