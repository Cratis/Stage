// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.SizeClasses;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_UiProfileConverter.when_converting;

public class and_a_default_size_class_is_declared : Specification
{
    UiProfileSyntax _syntax = null!;
    SizeClass? _result;

    void Establish() => _syntax = new("Admin", ["web"], "compact", ["core"], SourceLocation.Start);

    void Because() => _result = UiProfileConverter.Convert(_syntax).Single().DefaultSizeClass;

    [Fact] void should_apply_the_size_class_to_the_width_axis() => _result!.Width.ShouldEqual(WidthSizeClass.Compact);
    [Fact] void should_apply_the_same_size_class_to_the_height_axis() => _result!.Height.ShouldEqual(HeightSizeClass.Compact);
}
