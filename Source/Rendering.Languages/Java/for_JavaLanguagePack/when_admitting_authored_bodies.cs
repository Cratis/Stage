// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

public class when_admitting_authored_bodies : Specification
{
    readonly JavaLanguagePack _pack = new();
    AuthoredAdmission _clean = null!;
    AuthoredAdmission _privateUse = null!;
    AuthoredAdmission _escapedSurrogate = null!;

    void Because()
    {
        _clean = _pack.Admit("R1", "return 1;");
        _privateUse = _pack.Admit("R1", "int x; \ue000");
        _escapedSurrogate = _pack.Admit("R1", "int x; \\ud800");
    }

    [Fact] void should_admit_clean_text() => _clean.Outcome.ShouldEqual(AuthoredAdmissionOutcome.Admitted);
    [Fact] void should_refuse_a_private_use_character() => _privateUse.Code.ShouldEqual(JavaRefusalCodes.AuthoredIntegrity);
    [Fact] void should_refuse_an_escaped_lone_surrogate() => _escapedSurrogate.Code.ShouldEqual(JavaRefusalCodes.AuthoredIntegrity);
}
