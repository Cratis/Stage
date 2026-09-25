// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StringsCatalogInput;

public class when_creating_case_colliding_locales : Specification
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => StringsCatalogInput.Create(
        new Dictionary<string, string>
        {
            ["one.EN.strings"] = "invoice.first = \"First\"\n",
            ["two.en.strings"] = "invoice.second = \"Second\"\n"
        },
        "EN"));

    [Fact] void should_reject_the_ambiguous_catalog() => _error.ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();
}
