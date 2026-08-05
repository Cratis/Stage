// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Naming;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_Identifiers;

public class when_escaping_a_reserved_keyword : Specification
{
    string _reserved = null!;
    string _notReserved = null!;

    void Because()
    {
        _reserved = Identifiers.EscapeKeyword("class");
        _notReserved = Identifiers.EscapeKeyword("customerId");
    }

    [Fact] void should_prefix_a_reserved_keyword_with_at() => _reserved.ShouldEqual("@class");
    [Fact] void should_leave_a_non_keyword_untouched() => _notReserved.ShouldEqual("customerId");
}
