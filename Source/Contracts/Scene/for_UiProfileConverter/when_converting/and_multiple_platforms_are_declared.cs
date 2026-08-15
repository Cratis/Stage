// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_UiProfileConverter.when_converting;

public class and_multiple_platforms_are_declared : Specification
{
    UiProfileSyntax _syntax = null!;
    List<Cratis.Scene.Model.Profiles.UiProfile> _result = null!;

    void Establish() => _syntax = new(
        "Admin",
        ["web", "ios"],
        null,
        ["core", "PrimeReact"],
        SourceLocation.Start);

    void Because() => _result = [.. UiProfileConverter.Convert(_syntax)];

    [Fact] void should_produce_one_profile_per_platform() => _result.Count.ShouldEqual(2);
    [Fact] void should_carry_the_web_platform() => _result.Exists(profile => profile.TargetPlatform == "web").ShouldBeTrue();
    [Fact] void should_carry_the_ios_platform() => _result.Exists(profile => profile.TargetPlatform == "ios").ShouldBeTrue();
    [Fact] void should_carry_the_profile_name_on_every_result() => _result.TrueForAll(profile => profile.Name == "Admin").ShouldBeTrue();
    [Fact] void should_carry_the_packages_on_every_result() => _result.TrueForAll(profile => profile.Packages.SequenceEqual(["core", "PrimeReact"])).ShouldBeTrue();
}
