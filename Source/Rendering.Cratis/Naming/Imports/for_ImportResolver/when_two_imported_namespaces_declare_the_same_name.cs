// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_two_imported_namespaces_declare_the_same_name : given.a_framework_catalog
{
    readonly TypeReference _identity = new("Cratis.Chronicle.Identities", "Identity", 0, ReferenceContext.Type);
    readonly TypeReference _identities = new("Cratis.Chronicle.Identities", "IIdentityProvider", 0, ReferenceContext.Type);
    readonly TypeReference _arc = new("Cratis.Arc.Identity", "IIdentityProvider", 0, ReferenceContext.Type);
    ImportResolution _result = null!;

    void Because() => _result = ImportResolver.Resolve(Request(_identity, _identities, _arc), _catalog);

    [Fact] void should_import_the_namespace_an_unambiguous_name_needs() => string.Join(' ', _result.Usings).ShouldEqual("Cratis.Chronicle.Identities");
    [Fact] void should_spell_the_unambiguous_name_short() => _result.Spell(_identity).ShouldEqual("Identity");
    [Fact] void should_qualify_one_of_the_ambiguous_names() => _result.Spell(_identities).ShouldEqual("global::Cratis.Chronicle.Identities.IIdentityProvider");
    [Fact] void should_qualify_the_other_ambiguous_name() => _result.Spell(_arc).ShouldEqual("global::Cratis.Arc.Identity.IIdentityProvider");
}
