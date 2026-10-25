// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_an_import_would_make_a_global_using_name_ambiguous : given.a_framework_catalog
{
    // Cratis.Arc.Identity is a global using and declares IIdentityProvider too.
    readonly TypeReference _identities = new("Cratis.Chronicle.Identities", "IIdentityProvider", 0, ReferenceContext.Type);
    ImportResolution _result = null!;

    void Because() => _result = ImportResolver.Resolve(Request(_identities), _catalog);

    [Fact] void should_keep_the_ambiguous_name_qualified() => _result.Spell(_identities).ShouldEqual(_identities.Qualified);
    [Fact] void should_not_add_an_ambiguous_import() => _result.Usings.ShouldBeEmpty();
}
