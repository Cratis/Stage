// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Profiles;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_UiProfileConverter.when_converting;

public class and_the_profile_declares_none_of_it : Specification
{
    UiProfile _result = null!;

    void Because() => _result = UiProfileConverter.Convert(new UiProfileSyntax("Admin", ["web"], null, ["core"], SourceLocation.Start, Layout: "AppShell")).Single();

    [Fact] void should_have_no_root() => _result.Root.ShouldBeNull();
    [Fact] void should_have_no_appearance() => _result.Appearance.ShouldBeNull();
    [Fact] void should_have_no_defaults() => _result.Defaults.ShouldBeNull();
    [Fact] void should_keep_the_legacy_layout() => _result.Layout.ShouldEqual("AppShell");
}
