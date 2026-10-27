// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_the_scope_is_unknown : given.a_framework_catalog
{
    [Fact]
    void should_refuse_instead_of_retaining_qualified_names() => Assert.Throws<CSharpImportRejected>(() =>
        ImportResolver.Resolve(Request(new TypeReference("System", "Guid", 0, ReferenceContext.Type)) with { HasUnknownScope = true, RequiredImports = ["Cratis.Chronicle.Testing"] }, _catalog));
}
