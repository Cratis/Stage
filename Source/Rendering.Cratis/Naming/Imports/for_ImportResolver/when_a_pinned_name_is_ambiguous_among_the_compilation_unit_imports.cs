// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_a_pinned_name_is_ambiguous_among_the_compilation_unit_imports : given.a_framework_catalog
{
    // With both identity namespaces imported in the compilation unit, a use of 'IIdentityProvider' that compiles binds to a
    // member, parameter or local before the namespaces, so moving the requested import inside cannot change it.
    readonly TypeReference _identities = new("Cratis.Chronicle.Identities", "IIdentityProvider", 0, ReferenceContext.Type);
    ImportResolution _result = null!;

    void Because() => _result = ImportResolver.Resolve(
        Request(_identities) with { PinnedNames = ["IIdentityProvider"], RequiredImports = ["Cratis.Chronicle.Identities"] },
        _catalog);

    [Fact] void should_import_inside_the_namespace() => _result.Usings.ShouldContain("Cratis.Chronicle.Identities");
    [Fact] void should_not_import_it_above_the_namespace_too() => _result.TopLevelUsings.ShouldNotContain("Cratis.Chronicle.Identities");
    [Fact] void should_spell_the_type_short() => _result.Spell(_identities).ShouldEqual("IIdentityProvider");
}
