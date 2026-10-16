// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_naming_a_type_in_a_namespace_nothing_imports : given.a_framework_catalog
{
    readonly TypeReference _regex = new("System.Text.RegularExpressions", "Regex", 0, ReferenceContext.Type);
    readonly TypeReference _orderId = new("Shop.Common", "OrderId", 0, ReferenceContext.Type);
    ImportResolution _result = null!;

    void Because() => _result = ImportResolver.Resolve(Request(_regex, _orderId), _catalog);

    [Fact] void should_import_both_namespaces_in_ordinal_order() => string.Join(' ', _result.Usings).ShouldEqual("Shop.Common System.Text.RegularExpressions");
    [Fact] void should_spell_the_framework_type_by_its_short_name() => _result.Spell(_regex).ShouldEqual("Regex");
    [Fact] void should_spell_the_generated_type_by_its_short_name() => _result.Spell(_orderId).ShouldEqual("OrderId");
}
