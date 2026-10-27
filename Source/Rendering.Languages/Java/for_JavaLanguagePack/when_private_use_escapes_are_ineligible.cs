// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

public class when_private_use_escapes_are_ineligible : given.a_symbol_plan
{
    Exception?[] _errors = [];

    void Because() => _errors = [.. new[] { @"\\uE000", @"\\\\uE000", @"\\udb80\\udc00", @"\u005cuE000" }
        .Select(text => WithSegment(new TextSegment(text))).Select(plan => Catch.Exception(() => JavaLanguagePack.Validate(plan)))];

    [Fact] void should_not_treat_ineligible_or_recursively_produced_escapes_as_private_use() => _errors.ShouldEachConformTo(error => error == null);
}
