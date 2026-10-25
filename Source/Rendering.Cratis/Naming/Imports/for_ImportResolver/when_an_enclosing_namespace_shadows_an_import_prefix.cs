// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_an_enclosing_namespace_shadows_an_import_prefix : given.a_framework_catalog
{
    readonly TypeReference _regex = new("System.Text.RegularExpressions", "Regex", 0, ReferenceContext.Type);
    ImportResolution _result = null!;

    void Establish() => _generated = [.. _generated, new("Shop.System.Text.RegularExpressions", "Regex")];
    void Because() => _result = ImportResolver.Resolve(Request(_regex), _catalog);

    [Fact] void should_import_it_above_the_namespace_where_the_prefix_binds_globally() => _result.TopLevelUsings.ShouldContain("System.Text.RegularExpressions");
    [Fact] void should_spell_the_global_type_short() => _result.Spell(_regex).ShouldEqual("Regex");
}
