// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StringsCatalogInput;

public class when_creating_conflicting_keys_in_several_files : Specification
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => StringsCatalogInput.Create(
        new Dictionary<string, string>
        {
            ["Invoicing.en.strings"] = "invoice.amount = \"Amount\"\n",
            ["Payments.en.strings"] = "invoice.amount = \"Paid amount\"\n"
        },
        "en"));

    [Fact] void should_reject_the_catalog() => _error.ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();
    [Fact] void should_name_the_key() => _error!.Message.ShouldContain("'invoice.amount'");
    [Fact] void should_name_the_locale() => _error!.Message.ShouldContain("'en'");
    [Fact] void should_name_the_first_file() => _error!.Message.ShouldContain("'Invoicing.en.strings'");
    [Fact] void should_name_the_second_file() => _error!.Message.ShouldContain("'Payments.en.strings'");
}
