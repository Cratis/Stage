// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StringsCatalogInput;

public class when_creating_identical_keys_in_several_files : Specification
{
    bool _valid;
    StringsCatalogInput.Catalog? _catalog;

    void Because()
    {
        var input = StringsCatalogInput.Create(
            new Dictionary<string, string>
            {
                ["Invoicing.en.strings"] = "invoice.number = \"Number\"\ninvoice.amount = \"Amount\"\n",
                ["InvoiceList.en.strings"] = "invoice.number = \"Number\"\ninvoice.amount = \"Amount\"\ninvoice.list = \"Invoices\"\n"
            },
            "en");
        _valid = StringsCatalogInput.TryRead(input, out _catalog);
    }

    [Fact] void should_accept_the_catalog() => _valid.ShouldBeTrue();
    [Fact] void should_keep_each_key_once() => _catalog.Locales["en"].Keys.ShouldContainOnly(["invoice.amount", "invoice.list", "invoice.number"]);
    [Fact] void should_keep_the_shared_value() => _catalog.Locales["en"]["invoice.amount"].ShouldEqual("Amount");
}
