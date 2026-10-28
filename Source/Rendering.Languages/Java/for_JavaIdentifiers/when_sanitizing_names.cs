// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaIdentifiers;

public class when_sanitizing_names : Specification
{
    string[] _names = [];

    void Because() => _names = [.. new[] { "order-id", "9lives", "class", "", "_", "Customer", "café" }.Select(JavaIdentifiers.Sanitize)];

    [Fact] void should_sanitize_using_the_ascii_policy() => _names.ShouldEqual(["order_id", "_9lives", "class_", "Generated", "__", "Customer", "caf_"]);
    [Fact] void should_produce_only_valid_identifiers() => _names.ShouldEachConformTo(JavaIdentifiers.IsIdentifier);
}
