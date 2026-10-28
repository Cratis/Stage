// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_reference_sites_are_reported : Specification
{
    ImmutableArray<JavaResolvedFile> _files;
    JavaLanguagePack _pack = null!;

    void Establish() => _pack = new(K1GoldenCases.Facts());
    void Because() => _files = _pack.ResolveWithSites(K1GoldenCases.Files());

    [Fact] void should_report_the_nested_reference_spellings() => Slices(2).ShouldEqual(["library.Outer", "Outer.Inner"]);
    [Fact] void should_report_the_nested_intended_types() => _files[2].Sites.Select(site => site.Intended).ShouldEqual(["library.Outer", "vectors.nesting.Outer.Inner"]);
    [Fact] void should_report_allocated_collision_names() => _files[0].Sites.Select(site => site.Intended).ShouldEqual(["vectors.collision.Collision.Order_51861112", "vectors.collision.Collision.order_79b0a366"]);
    [Fact] void should_report_no_sites_for_a_refusal() => _files[4].Sites.ShouldBeEmpty();
    [Fact] void should_preserve_every_resolution_piece() => _files.Select(file => (file.Resolution.Refusal, string.Concat(file.Resolution.Pieces))).ShouldEqual(_pack.Resolve(K1GoldenCases.Files()).Select(file => (file.Refusal, string.Concat(file.Pieces))));

    IEnumerable<string> Slices(int index)
    {
        var text = given.a_java_pack.Text(_files[index].Resolution);
        return _files[index].Sites.Select(site => text[site.Start..site.End]);
    }
}
