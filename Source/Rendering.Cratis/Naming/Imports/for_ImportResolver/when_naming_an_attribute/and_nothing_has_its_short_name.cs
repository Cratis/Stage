// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver.when_naming_an_attribute;

public class and_nothing_has_its_short_name : given.a_framework_catalog
{
    readonly TypeReference _key = new("Cratis.Chronicle.Keys", "KeyAttribute", 0, ReferenceContext.Attribute);
    ImportResolution _result = null!;

    void Because() => _result = ImportResolver.Resolve(Request(_key), _catalog);

    [Fact] void should_drop_the_attribute_suffix() => _result.Spell(_key).ShouldEqual("Key");
    [Fact] void should_import_its_namespace() => string.Join(' ', _result.TopLevelUsings).ShouldEqual("Cratis.Chronicle.Keys");
}
