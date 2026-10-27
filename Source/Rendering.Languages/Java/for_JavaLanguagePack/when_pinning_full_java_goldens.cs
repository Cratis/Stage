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

    void Because() => _files = JavaLanguagePack.Resolve(K1Fixture.Load()).Files.Concat(JavaLanguagePack.Resolve(K1GoldenCases.Plan()).Files)
        .ToDictionary(file => file.Id, file => file.Text, StringComparer.Ordinal);

    [Fact] void should_pin_eleven_complete_files() => _goldens.Count.ShouldEqual(11);

    [Fact]
    void should_preserve_exact_bytes_for_corpus_collisions_shadowing_and_unknown_scope()
    {
        foreach (var golden in _goldens)
        {
            Encoding.UTF8.GetBytes(_files[golden.Key]).ShouldEqual(Encoding.UTF8.GetBytes(K1Fixture.Read(golden.Value)));
        }
    }
}
