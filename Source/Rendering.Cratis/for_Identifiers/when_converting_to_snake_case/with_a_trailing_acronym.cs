// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Naming;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_Identifiers.when_converting_to_snake_case;

public class with_a_trailing_acronym : Specification
{
    string _result = null!;

    void Because() => _result = Identifiers.ToSnakeCase("SendViaHTTP");

    [Fact] void should_keep_the_trailing_acronym_together() => _result.ShouldEqual("send_via_http");
}
