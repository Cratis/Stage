// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_the_scope_is_unknown : given.a_framework_catalog
{
    readonly TypeReference _guid = new("System", "Guid", 0, ReferenceContext.Type);
    readonly TypeReference _regex = new("System.Text.RegularExpressions", "Regex", 0, ReferenceContext.Type);
    ImportResolution _result = null!;

    void Because() => _result = ImportResolver.Resolve(Request(_guid, _regex) with { HasUnknownScope = true, RequiredImports = ["Cratis.Chronicle.Testing"] }, _catalog);

    [Fact] void should_keep_only_the_required_imports() => string.Join(' ', _result.Usings).ShouldEqual("Cratis.Chronicle.Testing");
    [Fact] void should_qualify_a_type_a_global_using_imports() => _result.Spell(_guid).ShouldEqual("global::System.Guid");
    [Fact] void should_qualify_a_type_needing_an_import() => _result.Spell(_regex).ShouldEqual("global::System.Text.RegularExpressions.Regex");
}
