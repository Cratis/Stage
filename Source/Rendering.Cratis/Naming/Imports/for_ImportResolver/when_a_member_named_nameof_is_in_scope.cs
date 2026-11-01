// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_a_member_named_nameof_is_in_scope : given.a_framework_catalog
{
    [Fact]
    void should_refuse_an_unprovable_nameof_binding() => Assert.Throws<CSharpImportRejected>(() =>
        ImportResolver.Resolve(Request(new TypeReference("System", "Guid", 0, ReferenceContext.NameOf)) with { ScopeValueNames = ["nameof"] }, _catalog));
}
