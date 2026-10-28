// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

public class when_resolving_the_frozen_k1_corpus : Specification
{
    ImmutableArray<DeclarativeLanguageFile> _files;
    ImmutableArray<DeclarativeFileResolution> _result;
    IReadOnlyDictionary<string, string> _expected = null!;

    void Establish()
    {
        _files = K1Fixture.Files();
        _expected = JsonSerializer.Deserialize<Dictionary<string, string>>(K1Fixture.Read("digests.json"))!;
    }

    void Because() => _result = new JavaLanguagePack(K1Fixture.Facts()).Resolve(_files);

    [Fact] void should_cover_all_289_files() => _result.Length.ShouldEqual(289);
    [Fact] void should_resolve_every_file_without_a_refusal() => _result.Count(resolution => resolution.Refusal is not null).ShouldEqual(0);
    [Fact] void should_pin_one_digest_per_file() => _expected.Count.ShouldEqual(289);

    [Fact]
    void should_pin_the_resolved_digest_of_every_file()
    {
        var actual = K1Resolution.ByPath(_files, _result).ToDictionary(item => item.Key, item => K1Resolution.Digest(K1Resolution.Text(item.Value)), StringComparer.Ordinal);

        actual.OrderBy(item => item.Key, StringComparer.Ordinal).ShouldEqual(_expected.OrderBy(item => item.Key, StringComparer.Ordinal));
    }

    [Fact] void should_leave_no_unresolved_placeholder() => _result.Any(resolution => K1Resolution.Text(resolution).Contains("/*__stage_", StringComparison.Ordinal)).ShouldBeFalse();

    [Fact]
    void should_leave_no_private_use_character()
    {
        foreach (var resolution in _result)
        {
            K1Resolution.Text(resolution).Any(character => character is >= '\uE000' and <= '\uF8FF').ShouldBeFalse();
        }
    }
}
