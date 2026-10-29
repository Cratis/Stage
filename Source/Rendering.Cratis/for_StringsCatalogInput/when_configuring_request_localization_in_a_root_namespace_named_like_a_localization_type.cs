// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StringsCatalogInput;

public class when_configuring_request_localization_in_a_root_namespace_named_like_a_localization_type : Specification
{
    string _program = null!;

    void Because()
    {
        var input = StringsCatalogInput.Create(new Dictionary<string, string> { ["invoices.en.strings"] = "invoice.key = \"Value\"\n" }, "en");
        StringsCatalogInput.TryRead(input, out var catalog);
        _program = StringsCatalogInput.ConfigureProgram("app.UseCratis();", catalog!, "CultureInfo.Billing");
    }

    [Fact] void should_qualify_the_colliding_invariant_culture() => _program.ShouldContain("global::System.Globalization.CultureInfo.InvariantCulture");
    [Fact] void should_qualify_the_colliding_culture_lookup() => _program.ShouldContain("global::System.Globalization.CultureInfo.GetCultureInfo(\"en\")");
    [Fact] void should_not_import_the_qualified_type_namespace() => _program.ShouldNotContain("using System.Globalization;");
    [Fact] void should_keep_the_request_culture_short() => _program.ShouldContain("new RequestCulture(");
    [Fact] void should_import_the_request_culture_namespace() => _program.ShouldContain("using Microsoft.AspNetCore.Localization;");
}
