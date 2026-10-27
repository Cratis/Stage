// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

public class when_checking_resolved_output : Specification
{
    SymbolPlan _plan = null!;
    Exception _error = null!;

    void Establish()
    {
        // Individually safe fields become an eligible private-use escape only after name substitution.
        TypeName external = new("library", @"\u005c", []);
        _plan = new([new("crafted", "example", [new NameSegment(external), new TextSegment(@"\\uE000")])], [external]);
    }

    void Because() => _error = Catch.Exception(() => JavaLanguagePack.Resolve(_plan));

    [Fact] void should_accept_the_fields_before_resolution() => Catch.Exception(() => JavaLanguagePack.Validate(_plan)).ShouldBeNull();
    [Fact] void should_refuse_the_final_resolved_private_use_escape() => ((LanguagePackRefusal)_error).Code.ShouldEqual(JavaRefusalCodes.PrivateUseCodePoint);
}
