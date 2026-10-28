// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

public class when_reporting_reference_sites_over_the_frozen_k1_corpus : Specification
{
    ImmutableArray<JavaResolvedFile> _files;

    void Because() => _files = new JavaLanguagePack(K1Fixture.Facts()).ResolveWithSites(K1Fixture.Files());

    [Fact] void should_report_every_frozen_reference() => _files.Sum(file => file.Sites.Length).ShouldEqual(2816);
    [Fact] void should_spell_each_intended_type_or_its_suffix() => _files.All(file => file.Sites.All(site =>
    {
        var slice = given.a_java_pack.Text(file.Resolution)[site.Start..site.End];
        return slice == site.Intended || site.Intended.EndsWith("." + slice, StringComparison.Ordinal);
    })).ShouldBeTrue();
    [Fact] void should_place_every_site_inside_a_generated_piece() => _files.All(file => file.Sites.All(site => InsideGenerated(file, site))).ShouldBeTrue();

    static bool InsideGenerated(JavaResolvedFile file, JavaReferenceSite site)
    {
        var offset = 0;
        foreach (var piece in file.Resolution.Pieces)
        {
            var length = piece switch
            {
                DeclarativeGeneratedPiece generated => generated.Text.Length,
                DeclarativeAuthoredPiece authored => authored.Text.Length,
                _ => 0
            };
            if (piece is DeclarativeGeneratedPiece && site.Start >= offset && site.End <= offset + length) return true;
            offset += length;
        }

        return false;
    }
}
