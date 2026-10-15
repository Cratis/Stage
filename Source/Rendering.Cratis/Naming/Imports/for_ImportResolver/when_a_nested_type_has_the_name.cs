// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_a_nested_type_has_the_name : given.a_framework_catalog
{
    readonly TypeReference _string = new("System", "String", 0, ReferenceContext.Type);
    ImportResolution _result = null!;

    void Because() => _result = ImportResolver.Resolve(Request(_string) with { ScopeTypeNames = ["String"] }, _catalog);

    [Fact] void should_qualify_the_type() => _result.Spell(_string).ShouldEqual("global::System.String");
}
