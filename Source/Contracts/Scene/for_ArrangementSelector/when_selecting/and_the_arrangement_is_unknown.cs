// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Layouts;
using Cratis.Scene.Model.SizeClasses;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ArrangementSelector.when_selecting;

public class and_the_arrangement_is_unknown : Specification
{
    ArrangementSelection? _selection;
    bool _supported;

    void Because() => _supported = ArrangementSelector.TrySelect("Shell", null, new UnknownArrangementType(), new(WidthSizeClass.Regular, HeightSizeClass.Regular), out _selection);

    [Fact] void should_report_an_unsupported_arrangement() => _supported.ShouldBeFalse();
    [Fact] void should_select_nothing() => _selection.ShouldBeNull();
    [Fact] void should_preserve_the_legacy_throw() => Catch.Exception(() => ArrangementSelector.Select("Shell", null, new UnknownArrangementType(), new(WidthSizeClass.Regular, HeightSizeClass.Regular))).ShouldBeOfExactType<UnknownArrangement>();

    sealed record UnknownArrangementType : Arrangement;
}
