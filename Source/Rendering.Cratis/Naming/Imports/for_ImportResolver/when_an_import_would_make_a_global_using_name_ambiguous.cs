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

    [Fact] void should_keep_the_reference_fully_qualified() => _result.Spell(_identities).ShouldEqual("global::Cratis.Chronicle.Identities.IIdentityProvider");
    [Fact] void should_not_keep_an_import_nothing_uses() => _result.Usings.ShouldBeEmpty();
}
