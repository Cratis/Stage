// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_an_enclosing_namespace_could_rebind_an_inner_import_prefix : given.a_framework_catalog
{
    // Shop.Cratis encloses nothing of the file, but a directive inside Shop.Orders.Placing looks 'Cratis' up in Shop first.
    readonly TypeReference _identities = new("Cratis.Chronicle.Identities", "IIdentityProvider", 0, ReferenceContext.Type);
    ImportResolution _result = null!;

    void Establish() => _generated = [.. _generated, new("Shop.Cratis.Things", "Thing")];
    void Because() => _result = ImportResolver.Resolve(Request(_identities), _catalog);

    [Fact] void should_import_inside_the_namespace() => _result.Usings.ShouldContain("Cratis.Chronicle.Identities");
    [Fact] void should_root_the_import() => _result.Inner("Cratis.Chronicle.Identities").ShouldEqual("global::Cratis.Chronicle.Identities");
}
