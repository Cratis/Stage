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

    [Fact] void should_qualify_the_framework_type_hidden_by_the_enclosing_type() => _result.Spell(_guid).ShouldEqual("global::System.Guid");
    [Fact] void should_not_repeat_the_global_framework_import() => _result.Usings.ShouldNotContain("System");
}
