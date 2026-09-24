// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StringsCatalogInput;

public class when_creating_a_syntactically_valid_unknown_locale : Specification
{
    bool _valid;

    void Because()
    {
        var input = StringsCatalogInput.Create(
            new Dictionary<string, string> { ["invoices.zzzzzzzz-ZZ.strings"] = "invoice.key = \"Value\"\n" },
            "zzzzzzzz-ZZ");
        _valid = StringsCatalogInput.TryRead(input, out _);
    }

    [Fact] void should_validate_without_installed_culture_data() => _valid.ShouldBeTrue();
}
