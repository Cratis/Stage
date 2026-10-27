// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

public class when_a_native_span_has_an_unknown_context : given.a_symbol_plan
{
    Exception?[] _errors = [];

    void Because() => _errors = [.. new[] { "", "expression", "Type", "MEMBER", "statements" }
        .Select(context => WithSegment(new NativeSegment("java", "body", "owner", context, [])))
        .Select(plan => Catch.Exception(() => JavaLanguagePack.Validate(plan)))];

    [Fact] void should_refuse_unknown_and_differently_cased_contexts() =>
        _errors.Cast<LanguagePackRefusal>().Select(error => error.Code).ShouldEachConformTo(code => code == JavaRefusalCodes.UnsupportedContext);
}
