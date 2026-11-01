// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_a_required_import_is_an_enclosing_namespace : given.a_framework_catalog
{
    readonly TypeReference _placeOrder = new(FileNamespace, "PlaceOrder", 0, ReferenceContext.Type);
    readonly TypeReference _order = new("Shop.Orders", "Order", 0, ReferenceContext.Type);
    ImportResolution _result = null!;

    void Establish() => _generated = [.. _generated, new("Shop.Orders", "Order")];

    void Because() => _result = ImportResolver.Resolve(Request(_placeOrder, _order) with { RequiredImports = [FileNamespace, "Shop.Orders"] }, _catalog);

    [Fact] void should_spell_the_type_short() => _result.Spell(_placeOrder).ShouldEqual("PlaceOrder");
    [Fact] void should_spell_the_enclosing_namespace_type_short() => _result.Spell(_order).ShouldEqual("Order");
    [Fact] void should_preserve_explicit_imports_for_referenced_types_to_keep_existing_header_bytes() =>
        string.Join(' ', _result.Usings.Concat(_result.TopLevelUsings).Order(StringComparer.Ordinal)).ShouldEqual("Shop.Orders Shop.Orders.Placing");
}
