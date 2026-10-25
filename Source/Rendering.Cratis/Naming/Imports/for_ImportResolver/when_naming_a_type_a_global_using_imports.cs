// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_naming_a_type_a_global_using_imports : given.a_framework_catalog
{
    readonly TypeReference _guid = new("System", "Guid", 0, ReferenceContext.Type);
    ImportResolution _result = null!;

    void Because() => _result = ImportResolver.Resolve(Request(_guid) with { RequiredImports = ["System"] }, _catalog);

    [Fact] void should_spell_it_by_its_short_name() => _result.Spell(_guid).ShouldEqual("Guid");
    [Fact] void should_not_repeat_the_global_import() => _result.Usings.ShouldBeEmpty();
    [Fact] void should_not_retain_a_redundant_explicit_namespace_import() => _result.TopLevelUsings.ShouldBeEmpty();
}
