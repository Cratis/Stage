// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_a_namespace_is_not_recorded : given.a_framework_catalog
{
    [Theory]
    [InlineData("Unrecorded")]
    [InlineData("System")]
    void should_refuse_an_uncataloged_type_even_in_a_known_namespace(string ns) => Assert.Throws<CSharpImportRejected>(() =>
        ImportResolver.Resolve(Request(new TypeReference(ns, "Thing", 0, ReferenceContext.Type)), _catalog));
}
