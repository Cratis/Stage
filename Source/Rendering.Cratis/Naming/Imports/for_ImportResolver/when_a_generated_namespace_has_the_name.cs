// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_a_generated_namespace_has_the_name : given.a_framework_catalog
{
    // An inner import can prove the type before lookup reaches the parent Shop.Common namespace.
    readonly TypeReference _common = new("Cratis.Chronicle.Keys", "Common", 0, ReferenceContext.Type);
    ImportResolution _result = null!;

    void Establish() => _generated = [.. _generated, new("Cratis.Chronicle.Keys", "Common")];

    void Because() => _result = ImportResolver.Resolve(Request(_common), _catalog);

    [Fact] void should_use_the_proved_inner_import() => _result.Spell(_common).ShouldEqual("Common");
    [Fact] void should_import_the_declaring_namespace() => _result.Usings.ShouldContain("Cratis.Chronicle.Keys");
}
