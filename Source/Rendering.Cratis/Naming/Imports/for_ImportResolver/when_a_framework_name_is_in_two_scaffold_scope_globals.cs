// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_a_framework_name_is_in_two_scaffold_scope_globals : Specification
{
    readonly TypeReference _chronicleTag = new("Cratis.Chronicle", "Tag", 0, ReferenceContext.Type);
    readonly TypeReference _mongoTag = new("MongoDB.Driver", "Tag", 0, ReferenceContext.Type);
    readonly TypeReference _sortDirection = new("Cratis.Arc.Queries", "SortDirection", 0, ReferenceContext.Type);
    ImportResolution _result = null!;

    void Because() => _result = ImportResolver.Resolve(
        new("Shop.Orders.Placing", GeneratedProjects.Application, [], [_chronicleTag, _mongoTag, _sortDirection]),
        FrameworkNameCatalog.Current);

    [Fact] void should_spell_the_chronicle_name_short_through_an_inner_import() => _result.Spell(_chronicleTag).ShouldEqual("Tag");
    [Fact] void should_import_the_chronicle_namespace_inside_the_namespace() => _result.Usings.ShouldContain("Cratis.Chronicle");
    [Fact] void should_qualify_the_mongo_name() => _result.Spell(_mongoTag).ShouldEqual("global::MongoDB.Driver.Tag");
    [Fact] void should_qualify_the_ambiguous_arc_name() => _result.Spell(_sortDirection).ShouldEqual("global::Cratis.Arc.Queries.SortDirection");
    [Fact] void should_not_add_an_import() => _result.TopLevelUsings.ShouldBeEmpty();
}
