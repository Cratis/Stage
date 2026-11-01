// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_a_pinned_name_already_binds_to_the_type_an_inner_import_brings : given.a_framework_catalog
{
    // Shop.Common.Guid is ambiguous with System.Guid above the namespace. The file also writes 'OrderId' itself, which
    // already binds to Shop.Common.OrderId through the import above and still does through the import inside.
    readonly TypeReference _guid = new("Shop.Common", "Guid", 0, ReferenceContext.Type);
    ImportResolution _result = null!;

    void Establish() => _generated = [.. _generated, new("Shop.Common", "Guid")];
    void Because() => _result = ImportResolver.Resolve(
        Request(_guid) with { PinnedNames = ["OrderId"], RequiredImports = ["Shop.Common"] },
        _catalog);

    [Fact] void should_import_inside_the_namespace() => _result.Usings.ShouldContain("Shop.Common");
    [Fact] void should_spell_the_type_short() => _result.Spell(_guid).ShouldEqual("Guid");
}
