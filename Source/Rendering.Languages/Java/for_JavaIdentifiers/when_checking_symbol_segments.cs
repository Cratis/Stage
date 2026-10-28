// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaIdentifiers;

public class when_checking_symbol_segments : Specification
{
    bool[] _accepted = [];
    bool[] _refused = [];

    void Because()
    {
        _accepted = [.. new[] { "Order", "order_1", "_x", "A" }.Select(JavaIdentifiers.IsSymbolSegment)];
        _refused = [.. new[] { "Outer$Inner", "Caf\u00e9", "class", "record", "var", "yield", "sealed", "permits", "when", "_", "", "1abc", "a-b" }.Select(JavaIdentifiers.IsSymbolSegment)];
    }

    [Fact] void should_accept_ascii_identifiers() => _accepted.ShouldEachConformTo(value => value);
    [Fact] void should_refuse_dollar_non_ascii_reserved_contextual_and_underscore_names() => _refused.ShouldEachConformTo(value => !value);
}
