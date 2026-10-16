// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_a_generated_type_in_an_enclosing_namespace_has_the_name : given.a_framework_catalog
{
    readonly TypeReference _guid = new("System", "Guid", 0, ReferenceContext.Type);
    readonly TypeReference _generatedGuid = new("Shop.Orders", "Guid", 0, ReferenceContext.Type);
    ImportResolution _result = null!;

    void Establish() => _generated = [.. _generated, new("Shop.Orders", "Guid")];

    void Because() => _result = ImportResolver.Resolve(Request(_guid, _generatedGuid), _catalog);

    [Fact] void should_qualify_the_framework_type() => _result.Spell(_guid).ShouldEqual("global::System.Guid");
    [Fact] void should_qualify_the_other_ambiguous_type() => _result.Spell(_generatedGuid).ShouldEqual("global::Shop.Orders.Guid");
}
