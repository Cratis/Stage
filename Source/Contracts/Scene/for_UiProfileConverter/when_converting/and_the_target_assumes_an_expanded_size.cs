// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.SizeClasses;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_UiProfileConverter.when_converting;

/// <summary>
/// 'target size expanded' is valid Screenplay and appears in Screenplay's own documentation, but the converter
/// parsed it against the two-axis arrangement matrix - which has no such value - and threw. A sample
/// application declaring a desktop profile is what surfaced it.
/// </summary>
public class and_the_target_assumes_an_expanded_size : Specification
{
    UiProfileSyntax _syntax = null!;
    TargetSizeClass? _result;

    void Establish() => _syntax = new("Desktop", ["web"], "expanded", ["core"], SourceLocation.Start);

    void Because() => _result = UiProfileConverter.Convert(_syntax).Single().DefaultSizeClass;

    [Fact] void should_carry_it_rather_than_throw() => _result!.Value.ShouldEqual(TargetSizeClass.Expanded);
}
