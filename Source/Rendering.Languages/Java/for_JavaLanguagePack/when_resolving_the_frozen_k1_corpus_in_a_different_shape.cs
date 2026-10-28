// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

public class when_resolving_the_frozen_k1_corpus_in_a_different_shape : Specification
{
    ImmutableArray<DeclarativeLanguageFile> _files;
    JavaLanguagePack _pack = null!;
    IReadOnlyDictionary<string, string> _baseline = null!;

    void Establish()
    {
        _files = K1Fixture.Files();
        _pack = new(K1Fixture.Facts());
        _baseline = Digests(_files);
    }

    [Fact]
    void should_resolve_each_file_the_same_when_the_files_are_reordered() => Digests([.. _files.Reverse()]).OrderBy(item => item.Key, StringComparer.Ordinal).ShouldEqual(_baseline.OrderBy(item => item.Key, StringComparer.Ordinal));

    [Fact]
    void should_resolve_each_file_the_same_when_an_unrelated_file_is_added()
    {
        DeclarativeLanguageFile unrelated = new(
            "unrelated/extra/Extra.java",
            [new DeclarativeSymbolSegment(Cratis.Stage.Rendering.Templates.TemplateSymbolKind.Imports, null), new DeclarativeTextSegment("public class "), new DeclarativeSymbolSegment(Cratis.Stage.Rendering.Templates.TemplateSymbolKind.Declaration, new("unrelated.extra", "Extra")), new DeclarativeTextSegment(" {}\n")]);
        var added = Digests([.. _files, unrelated]);

        added.Where(item => item.Key != unrelated.Path).OrderBy(item => item.Key, StringComparer.Ordinal).ShouldEqual(_baseline.OrderBy(item => item.Key, StringComparer.Ordinal));
    }

    Dictionary<string, string> Digests(ImmutableArray<DeclarativeLanguageFile> files) =>
        K1Resolution.ByPath(files, _pack.Resolve(files)).ToDictionary(item => item.Key, item => K1Resolution.Digest(K1Resolution.Text(item.Value)), StringComparer.Ordinal);
}
