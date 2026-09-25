// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StringsCatalogInput;

public class when_round_tripping_mixed_case_and_underscore_keys : Specification
{
    bool _valid;
    StringsCatalogInput.Catalog? _catalog;

    void Because()
    {
        var input = StringsCatalogInput.Create(
            new Dictionary<string, string> { ["invoices.en.strings"] = "invoice.B = \"Upper\"\ninvoice.a = \"Lower\"\ninvoice_c = \"Underscore\"\n" },
            "en");
        _valid = StringsCatalogInput.TryRead(input, out _catalog);
    }

    [Fact] void should_preserve_the_canonical_catalog() => _valid.ShouldBeTrue();
    [Fact] void should_keep_every_key() => _catalog!.Locales["en"].Keys.ShouldContainOnly(["invoice.B", "invoice.a", "invoice_c"]);
}
