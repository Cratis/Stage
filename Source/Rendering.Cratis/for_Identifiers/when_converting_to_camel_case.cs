// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Naming;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_Identifiers;

public class when_converting_to_camel_case : Specification
{
    string _result = null!;

    void Because() => _result = Identifiers.ToCamelCase("InvoiceNumber");

    [Fact] void should_lowercase_the_first_letter() => _result.ShouldEqual("invoiceNumber");
}
