// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_a_pinned_name_binds_elsewhere_before_an_inner_import : given.a_framework_catalog
{
    // The file writes 'IIdentityProvider' itself, which the global using binds to Arc's; an import inside would rebind it.
    readonly TypeReference _identities = new("Cratis.Chronicle.Identities", "IIdentityProvider", 0, ReferenceContext.Type);
    ImportResolution _result = null!;

    void Because() => _result = ImportResolver.Resolve(Request(_identities) with { PinnedNames = ["IIdentityProvider"] }, _catalog);

    [Fact] void should_not_import_inside_the_namespace() => _result.Usings.ShouldBeEmpty();
    [Fact] void should_qualify_the_type() => _result.Spell(_identities).ShouldEqual("global::Cratis.Chronicle.Identities.IIdentityProvider");
}
