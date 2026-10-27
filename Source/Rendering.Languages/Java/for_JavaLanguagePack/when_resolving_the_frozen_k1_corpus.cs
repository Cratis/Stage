// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

public class when_resolving_the_frozen_k1_corpus : Specification
{
    SymbolPlan _plan = null!;
    LanguagePackResolution _result = null!;
    IReadOnlyDictionary<string, string> _expected = null!;

    void Establish()
    {
        _plan = K1Fixture.Load();
        _expected = JsonSerializer.Deserialize<Dictionary<string, string>>(K1Fixture.Read("digests.json"))!;
    }

    void Because() => _result = JavaLanguagePack.Resolve(_plan);

    [Fact] void should_cover_all_289_lexical_files() => _result.Files.Count.ShouldEqual(289);
    [Fact] void should_pin_one_digest_per_resolved_file() => Digests(_result).ShouldEqual(_expected.OrderBy(item => item.Key, StringComparer.Ordinal).Select(item => item.Key + "=" + item.Value));
    [Fact] void should_fit_the_full_serialized_fixture_below_400_kib() => (Encoding.UTF8.GetByteCount(K1Fixture.Read("plan.json")) < 400 * 1024).ShouldBeTrue();
    [Fact] void should_have_no_degradation_diagnostics() => _result.Diagnostics.Count.ShouldEqual(0);
    [Fact] void should_contain_explicit_owners_for_all_references() => _plan.Files.SelectMany(file => file.Segments.OfType<ReferenceSegment>()).All(reference => reference.LexicalOwner.Length > 0).ShouldBeTrue();
    [Fact] void should_keep_external_type_knowledge_unknown() => _plan.ExternalDeclarations.Count.ShouldEqual(0);

    [Fact]
    void should_leave_zero_raw_or_decoded_private_use_tokens()
    {
        foreach (var file in _result.Files)
        {
            PrivateUseCodePoints.Validate(file.Text);
            PrivateUseCodePoints.Validate(JavaUnicodeEscapes.Decode(file.Text));
        }
    }

    [Theory]
    [InlineData("files")]
    [InlineData("external-types")]
    [InlineData("unrelated-file")]
    void should_preserve_original_digests_under_plan_permutations(string change)
    {
        var changed = change switch
        {
            "files" => _plan with { Files = [.. _plan.Files.Reverse()] },
            "external-types" => _plan with { ExternalTypes = [.. _plan.ExternalTypes.Reverse()] },
            "unrelated-file" => _plan with { Files = [.. _plan.Files, new("unrelated", "unrelated.extra", [new TextSegment("class "), new DeclarationSegment(new("unrelated.extra", "Extra", []), "extra") { Supertypes = KnownTypes<TypeName>.None }, new TextSegment(" {}\n")])] },
            _ => throw new InvalidOperationException(change),
        };
        var result = JavaLanguagePack.Resolve(changed);
        var originals = result with { Files = [.. result.Files.Where(file => _expected.ContainsKey(file.Id))] };
        Digests(originals).ShouldEqual(Digests(_result));
    }

    static IEnumerable<string> Digests(LanguagePackResolution result) => result.Files.OrderBy(file => file.Id, StringComparer.Ordinal)
        .Select(file => file.Id + "=" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(file.Text))));
}
