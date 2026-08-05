// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Naming;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_Identifiers;

public class when_converting_to_pascal_case : Specification
{
    string _fromCamelCase = null!;
    string _fromPascalCase = null!;
    string _fromSeparatedWords = null!;
    string _fromEmpty = null!;
    string _fromLeadingDigit = null!;

    void Because()
    {
        _fromCamelCase = Identifiers.ToPascalCase("invoiceId");
        _fromPascalCase = Identifiers.ToPascalCase("RegisterInvoice");
        _fromSeparatedWords = Identifiers.ToPascalCase("invoice_number");
        _fromEmpty = Identifiers.ToPascalCase(string.Empty);
        _fromLeadingDigit = Identifiers.ToPascalCase("2ndAttempt");
    }

    [Fact] void should_capitalize_a_camel_case_name() => _fromCamelCase.ShouldEqual("InvoiceId");
    [Fact] void should_leave_an_already_pascal_case_name_unchanged() => _fromPascalCase.ShouldEqual("RegisterInvoice");
    [Fact] void should_join_separated_words_into_one_pascal_case_word() => _fromSeparatedWords.ShouldEqual("InvoiceNumber");
    [Fact] void should_fall_back_to_item_for_an_empty_name() => _fromEmpty.ShouldEqual("Item");
    [Fact] void should_prefix_an_underscore_when_the_result_starts_with_a_digit() => _fromLeadingDigit.ShouldEqual("_2ndAttempt");
}
