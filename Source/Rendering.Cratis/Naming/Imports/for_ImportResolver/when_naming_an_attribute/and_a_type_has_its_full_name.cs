// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver.when_naming_an_attribute;

public class and_a_type_has_its_full_name : given.a_framework_catalog
{
    readonly TypeReference _key = new("Cratis.Chronicle.Keys", "KeyAttribute", 0, ReferenceContext.Attribute);
    ImportResolution _result = null!;

    void Establish() => _generated = [.. _generated, new(FileNamespace, "KeyAttribute")];

    void Because() => _result = ImportResolver.Resolve(Request(_key), _catalog);

    [Fact] void should_qualify_the_attribute() => _result.Spell(_key).ShouldEqual("global::Cratis.Chronicle.Keys.KeyAttribute");
}
