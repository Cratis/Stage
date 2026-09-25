// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StringsCatalogInput;

public class when_configuring_request_localization : Specification
{
    string _program = null!;

    void Because()
    {
        var input = StringsCatalogInput.Create(new Dictionary<string, string> { ["invoices.en.strings"] = "invoice.key = \"Value\"\n" }, "en");
        StringsCatalogInput.TryRead(input, out var catalog);
        _program = StringsCatalogInput.ConfigureProgram("app.UseCratis();", catalog!);
    }

    [Fact] void should_keep_formatting_invariant() => _program.Contains("SupportedCultures = [System.Globalization.CultureInfo.InvariantCulture]", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_set_only_the_default_ui_culture() => _program.Contains("RequestCulture(System.Globalization.CultureInfo.InvariantCulture, System.Globalization.CultureInfo.GetCultureInfo(\"en\"))", StringComparison.Ordinal).ShouldBeTrue();
}
