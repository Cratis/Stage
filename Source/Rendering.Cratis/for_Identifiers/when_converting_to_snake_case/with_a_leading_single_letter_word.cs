// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Naming;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_Identifiers.when_converting_to_snake_case;

public class with_a_leading_single_letter_word : Specification
{
    string _result = null!;

    void Because() => _result = Identifiers.ToSnakeCase("ADraftInvoice");

    [Fact] void should_split_the_single_letter_word() => _result.ShouldEqual("a_draft_invoice");
}
