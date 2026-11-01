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
        _program = StringsCatalogInput.ConfigureProgram("app.UseCratis();", catalog!, "Invoices");
    }

    [Fact] void should_keep_formatting_invariant() => _program.ShouldContain("SupportedCultures = [CultureInfo.InvariantCulture]");
    [Fact] void should_set_only_the_default_ui_culture() => _program.ShouldContain("new RequestCulture(CultureInfo.InvariantCulture, CultureInfo.GetCultureInfo(\"en\"))");
    [Fact] void should_use_the_implicitly_imported_options_type() => _program.ShouldContain("app.UseRequestLocalization(new RequestLocalizationOptions");
    [Fact] void should_spell_no_type_with_global() => _program.ShouldNotContain("global::");
    [Fact] void should_import_the_localization_namespaces() => _program.StartsWith("using Microsoft.AspNetCore.Localization;\nusing System.Globalization;\n\n", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_import_the_builder_namespace() => _program.ShouldNotContain("using Microsoft.AspNetCore.Builder;");
}
