// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_a_namespace_is_not_recorded : given.a_framework_catalog
{
    readonly TypeReference _thing = new("Unrecorded", "Thing", 0, ReferenceContext.Type);
    ImportResolution _result = null!;

    void Because() => _result = ImportResolver.Resolve(Request(_thing), _catalog);

    [Fact] void should_not_import_it() => _result.Usings.ShouldBeEmpty();
    [Fact] void should_qualify_the_type() => _result.Spell(_thing).ShouldEqual("global::Unrecorded.Thing");
}
