// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_CratisEsmSupport;

public class when_comparing_with_the_audited_gate : Specification
{
    static readonly HashSet<(LanguageVersion Language, SemanticVersion Semantics)> ExpectedAudited =
    [
        (new(1, 0), new(1, 0)),
        (new(2, 0), new(2, 0)),
        (new(3, 0), new(3, 0)),
        (new(4, 0), new(4, 0)),
        (new(5, 0), new(5, 0)),
        (new(6, 0), new(6, 0)),
        (new(7, 0), new(7, 0)),
        (new(8, 0), new(8, 0)),
        (new(9, 0), new(9, 0)),
        (new(10, 0), new(10, 0)),
        (new(11, 0), new(11, 0)),
        (new(12, 0), new(12, 0)),
        (new(13, 0), new(13, 0)),
        (new(14, 0), new(14, 0)),
        (new(15, 0), new(15, 0)),
        (new(16, 0), new(16, 0)),
        (new(17, 0), new(17, 0)),
        (new(18, 0), new(18, 0)),
        (new(19, 0), new(19, 0)),
        (new(20, 0), new(20, 0)),
        (new(21, 0), new(21, 0)),
        (new(22, 0), new(22, 0)),
        (new(23, 0), new(23, 0)),
        (new(24, 0), new(24, 0)),
        (new(25, 0), new(25, 0)),
        (new(26, 0), new(26, 0)),
        (new(27, 0), new(27, 0)),
        (new(28, 0), new(28, 0)),
        (new(29, 0), new(29, 0)),
        (new(30, 0), new(30, 0)),
        (new(32, 0), new(32, 0)),
        (new(33, 0), new(33, 0)),
        (new(34, 0), new(34, 0))
    ];

    readonly List<string> _descriptorMismatches = [];
    readonly List<string> _gateMismatches = [];
    ExecutableSemanticModel _model = null!;
    int _compared;

    void Establish() => _model = SemanticModelSerializer.Deserialize(ProtectedCommandReadsCorpus.V25.EsmBytes.AsSpan());

    void Because()
    {
        var languageField = typeof(ExecutableSemanticModel).GetField("<LanguageVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        var semanticField = typeof(ExecutableSemanticModel).GetField("<SemanticVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (var language in Versions())
        {
            languageField.SetValue(_model, new LanguageVersion(language.Major, language.Minor));
            foreach (var semantic in Versions())
            {
                semanticField.SetValue(_model, semantic);
                CheckPair();
            }
        }
    }

    [Fact] void should_pin_the_descriptor_to_the_explicit_audited_set() => _descriptorMismatches.ShouldBeEmpty();
    [Fact] void should_pin_the_real_gate_to_the_explicit_audited_set() => _gateMismatches.ShouldBeEmpty();
    [Fact] void should_examine_the_full_cartesian_product() => _compared.ShouldEqual(14400);
    [Fact] void should_not_add_unexpected_audits_to_the_descriptor() => CratisEsmSupport.Descriptor.Audited.ToHashSet().SetEquals(ExpectedAudited).ShouldBeTrue();
    [Fact] void should_preserve_the_v32_audit() => CratisEsmSupport.Descriptor.Classify(LanguageVersion.V32, SemanticVersion.V32).ShouldEqual(EsmVersionStanding.Audited);
    [Fact] void should_leave_v31_unaudited() => CratisEsmSupport.Descriptor.Classify(LanguageVersion.V31, SemanticVersion.V31).ShouldEqual(EsmVersionStanding.Unadmitted);
    [Fact]
    void should_accept_32_minors_only() => Versions().Where(version => version.Minor > 0).ShouldEachConformTo(version =>
        CratisEsmSupport.Descriptor.Classify(new(version.Major, version.Minor), version) == EsmVersionStanding.NewerWithinRange == (version.Major == 32));
    [Fact] void should_audit_the_exact_v33_pair() => CratisEsmSupport.Descriptor.Classify(LanguageVersion.V33, SemanticVersion.V33).ShouldEqual(EsmVersionStanding.Audited);
    [Fact] void should_audit_the_exact_v34_pair() => CratisEsmSupport.Descriptor.Classify(LanguageVersion.V34, SemanticVersion.V34).ShouldEqual(EsmVersionStanding.Audited);
    [Fact]
    void should_refuse_v33_and_v34_minors_and_v35_as_future_majors() => new SemanticVersion[] { new(33, 1), new(34, 1), new(35, 0) }.ShouldEachConformTo(version =>
        CratisEsmSupport.Descriptor.Classify(new(version.Major, version.Minor), version) == EsmVersionStanding.FutureMajor);
    [Fact] void should_describe_the_range() => CratisEsmSupport.Descriptor.Accepts.ToString().ShouldEqual(">=32.0 <33");

    static IEnumerable<SemanticVersion> Versions() => Enumerable.Range(1, 40)
        .SelectMany(major => Enumerable.Range(0, 3).Select(minor => new SemanticVersion((uint)major, (uint)minor)));

    void CheckPair()
    {
        var expected = ExpectedAudited.Contains((_model.LanguageVersion, _model.SemanticVersion));
        var audited = CratisEsmSupport.Descriptor.Classify(_model.LanguageVersion, _model.SemanticVersion) == EsmVersionStanding.Audited;
        if (audited != expected) _descriptorMismatches.Add($"{_model.LanguageVersion}/{_model.SemanticVersion}");
        if (SemanticVersionFeatures.Supports(_model) != expected) _gateMismatches.Add($"{_model.LanguageVersion}/{_model.SemanticVersion}");
        _compared++;
    }
}
