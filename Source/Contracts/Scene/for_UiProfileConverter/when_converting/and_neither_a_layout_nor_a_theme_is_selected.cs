// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_UiProfileConverter.when_converting;

public class and_neither_a_layout_nor_a_theme_is_selected : Specification
{
    UiProfileSyntax _syntax = null!;
    Cratis.Scene.Model.Profiles.UiProfile _result = null!;

    void Establish() => _syntax = new("Admin", ["web"], null, ["PrimeReact"], SourceLocation.Start);

    void Because() => _result = UiProfileConverter.Convert(_syntax).Single();

    [Fact] void should_leave_the_layout_unselected() => _result.Layout.ShouldBeNull();
    [Fact] void should_leave_the_theme_unselected() => _result.Theme.ShouldBeNull();
}
