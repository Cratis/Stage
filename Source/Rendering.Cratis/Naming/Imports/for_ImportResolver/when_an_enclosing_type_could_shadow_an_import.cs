// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_an_enclosing_type_could_shadow_an_import : given.a_framework_catalog
{
    readonly TypeReference _guid = new("System", "Guid", 0, ReferenceContext.Type);
    ImportResolution _result = null!;

    void Establish() => _generated = [.. _generated, new("Shop", "Guid"), new(string.Empty, "Guid")];
    void Because() => _result = ImportResolver.Resolve(Request(_guid), _catalog);

    [Fact] void should_bind_through_an_in_namespace_import() => _result.Spell(_guid).ShouldEqual("Guid");
    [Fact] void should_import_the_framework_namespace() => _result.Usings.ShouldContain("System");
}
