// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_an_import_would_bring_a_type_named_like_a_pinned_name : given.a_framework_catalog
{
    // An authored body writes 'Match', which the resolver does not spell; importing RegularExpressions would rebind it.
    readonly TypeReference _regex = new("System.Text.RegularExpressions", "Regex", 0, ReferenceContext.Type);
    ImportResolution _result = null!;

    void Because() => _result = ImportResolver.Resolve(Request(_regex) with { PinnedNames = ["Match"] }, _catalog);

    [Fact] void should_not_import_the_namespace() => _result.Usings.ShouldBeEmpty();
    [Fact] void should_qualify_the_type() => _result.Spell(_regex).ShouldEqual("StageImport0::Regex");
}
