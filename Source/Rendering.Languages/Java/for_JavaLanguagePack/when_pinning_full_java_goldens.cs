// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

public class when_pinning_full_java_goldens : Specification
{
    IReadOnlyDictionary<string, string> _files = null!;
    IReadOnlyDictionary<string, string> _goldens = null!;

    void Establish() => _goldens = JsonSerializer.Deserialize<Dictionary<string, string>>(K1Fixture.Read("goldens.json"))!;

    void Because()
    {
        var corpus = K1Fixture.Files();
        var cases = K1GoldenCases.Files();
        _files = K1Resolution.ByPath(corpus, new JavaLanguagePack(K1Fixture.Facts()).Resolve(corpus))
            .Concat(K1Resolution.ByPath(cases, new JavaLanguagePack(K1GoldenCases.Facts()).Resolve(cases)))
            .ToDictionary(item => item.Key, item => K1Resolution.Text(item.Value), StringComparer.Ordinal);
    }

    [Fact] void should_pin_nine_complete_files() => _goldens.Count.ShouldEqual(9);

    [Fact]
    void should_preserve_exact_bytes_for_corpus_collisions_inheritance_nesting_and_obscuring()
    {
        foreach (var golden in _goldens)
        {
            Encoding.UTF8.GetBytes(_files[golden.Key]).ShouldEqual(Encoding.UTF8.GetBytes(K1Fixture.Read(golden.Value)));
        }
    }
}
