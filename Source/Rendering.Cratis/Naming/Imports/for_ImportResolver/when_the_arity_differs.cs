// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_the_arity_differs : given.a_framework_catalog
{
    readonly TypeReference _scenario = new("Cratis.Chronicle.Testing", "EventScenario", 1, ReferenceContext.Type);
    ImportResolution _result = null!;

    // A non-generic EventScenario in the file's own namespace does not hide EventScenario<T>.
    void Establish() => _generated = [.. _generated, new(FileNamespace, "EventScenario")];

    void Because() => _result = ImportResolver.Resolve(Request(_scenario), _catalog);

    [Fact] void should_spell_the_generic_type_short() => _result.Spell(_scenario).ShouldEqual("EventScenario");
    [Fact] void should_import_its_namespace() => string.Join(' ', _result.TopLevelUsings).ShouldEqual("Cratis.Chronicle.Testing");
}
