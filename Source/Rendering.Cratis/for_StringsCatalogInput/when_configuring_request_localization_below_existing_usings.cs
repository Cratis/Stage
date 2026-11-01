// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StringsCatalogInput;

public class when_configuring_request_localization_below_existing_usings : Specification
{
    const string Source = "using Cratis.Arc.MongoDB;\n\nvar app = builder.Build();\napp.UseCratis();\n";
    const string Usings = "using Cratis.Arc.MongoDB;\nusing Microsoft.AspNetCore.Localization;\nusing System.Globalization;\n\n";
    string _program = null!;

    void Because()
    {
        var input = StringsCatalogInput.Create(new Dictionary<string, string> { ["invoices.en.strings"] = "invoice.key = \"Value\"\n" }, "en");
        StringsCatalogInput.TryRead(input, out var catalog);
        _program = StringsCatalogInput.ConfigureProgram(Source, catalog!, "Invoices");
    }

    [Fact] void should_merge_the_using_block_in_ordinal_order() => _program[..(_program.IndexOf("\n\n", StringComparison.Ordinal) + 2)].ShouldEqual(Usings);
    [Fact] void should_include_each_directive_once() => Usings.Split('\n', StringSplitOptions.RemoveEmptyEntries).ShouldEachConformTo(directive => _program.Split(directive, StringSplitOptions.None).Length == 2);
    [Fact] void should_preserve_the_body_except_for_the_localization_block() => _program[Usings.Length..].ShouldEqual("var app = builder.Build();\napp.UseRequestLocalization(new RequestLocalizationOptions\n{\n    DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture, CultureInfo.GetCultureInfo(\"en\")),\n    SupportedCultures = [CultureInfo.InvariantCulture],\n    SupportedUICultures = [CultureInfo.GetCultureInfo(\"en\")]\n});\napp.UseCratis();\n");
}
