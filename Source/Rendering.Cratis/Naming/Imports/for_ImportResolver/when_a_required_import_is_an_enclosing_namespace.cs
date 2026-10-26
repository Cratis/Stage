// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_a_required_import_is_an_enclosing_namespace : given.a_framework_catalog
{
    readonly TypeReference _placeOrder = new(FileNamespace, "PlaceOrder", 0, ReferenceContext.Type);
    ImportResolution _result = null!;

    void Because() => _result = ImportResolver.Resolve(Request(_placeOrder) with { RequiredImports = [FileNamespace, "Shop.Orders"] }, _catalog);

    [Fact] void should_spell_the_type_short() => _result.Spell(_placeOrder).ShouldEqual("PlaceOrder");
    [Fact] void should_not_import_the_file_namespace_or_one_around_it() => _result.Usings.Concat(_result.TopLevelUsings).ShouldBeEmpty();
}
