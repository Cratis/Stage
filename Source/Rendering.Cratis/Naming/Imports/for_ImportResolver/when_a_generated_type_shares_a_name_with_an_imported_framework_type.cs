// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_a_generated_type_shares_a_name_with_an_imported_framework_type : given.a_framework_catalog
{
    readonly TypeReference _generatedType = new("Shop.Common", "EventType", 0, ReferenceContext.Type);
    readonly TypeReference _frameworkType = new("Cratis.Chronicle.Events", "EventType", 0, ReferenceContext.Type);
    readonly TypeReference _context = new("Cratis.Chronicle.Events", "EventContext", 0, ReferenceContext.Type);
    ImportResolution _result = null!;

    void Establish() => _generated = [.. _generated, new("Shop.Common", "EventType")];

    void Because() => _result = ImportResolver.Resolve(Request(_frameworkType, _context, _generatedType), _catalog);

    [Fact] void should_spell_the_generated_type_short() => _result.Spell(_generatedType).ShouldEqual("EventType");
    [Fact] void should_qualify_only_the_colliding_framework_name() => _result.Spell(_frameworkType).ShouldEqual("global::Cratis.Chronicle.Events.EventType");
    [Fact] void should_spell_the_other_framework_type_short() => _result.Spell(_context).ShouldEqual("EventContext");
    [Fact] void should_import_the_generated_namespace_inside_the_namespace() => string.Join(' ', _result.Usings).ShouldEqual("Shop.Common");
    [Fact] void should_import_the_framework_namespace_above_the_namespace() => string.Join(' ', _result.TopLevelUsings).ShouldEqual("Cratis.Chronicle.Events");
}
