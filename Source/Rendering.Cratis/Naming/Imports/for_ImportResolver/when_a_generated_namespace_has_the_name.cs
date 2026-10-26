// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_a_generated_namespace_has_the_name : given.a_framework_catalog
{
    // Shop.Common is a namespace, so 'Common' found from inside Shop.Orders.Placing is the namespace, not a type.
    readonly TypeReference _common = new("Cratis.Chronicle.Keys", "Common", 0, ReferenceContext.Type);
    ImportResolution _result = null!;

    void Because() => _result = ImportResolver.Resolve(Request(_common), _catalog);

    [Fact] void should_qualify_the_type() => _result.Spell(_common).ShouldEqual("global::Cratis.Chronicle.Keys.Common");
}
