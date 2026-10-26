// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_a_member_named_nameof_is_in_scope : given.a_framework_catalog
{
    readonly TypeReference _name = new("System", "Guid", 0, ReferenceContext.NameOf);
    ImportResolution _result = null!;

    void Because() => _result = ImportResolver.Resolve(Request(_name) with { ScopeValueNames = ["nameof"] }, _catalog);

    [Fact] void should_qualify_the_argument() => _result.Spell(_name).ShouldEqual("global::System.Guid");
}
