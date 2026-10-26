// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_a_model_collides_with_a_non_global_framework_import : given.a_framework_catalog
{
    readonly TypeReference _model = new("Shop.Common", "EventType", 0, ReferenceContext.Type);
    readonly TypeReference _framework = new("Cratis.Chronicle.Events", "EventType", 0, ReferenceContext.Type);
    readonly TypeReference _context = new("Cratis.Chronicle.Events", "EventContext", 0, ReferenceContext.Type);
    ImportResolution _result = null!;

    void Establish() => _generated = [.. _generated, new("Shop.Common", "EventType")];

    void Because() => _result = ImportResolver.Resolve(Request(_model, _framework, _context), _catalog);

    [Fact] void should_keep_the_proven_model_spelling() => _result.Spell(_model).ShouldEqual("EventType");
    [Fact] void should_keep_the_non_global_framework_collision_aliased() => _result.Spell(_framework).ShouldEqual("StageImport0::EventType");
    [Fact] void should_declare_the_used_alias() => Assert.Equal(["StageImport0 = global::Cratis.Chronicle.Events"], _result.Aliases);
    [Fact] void should_keep_the_other_framework_reference_short() => _result.Spell(_context).ShouldEqual("EventContext");
}
