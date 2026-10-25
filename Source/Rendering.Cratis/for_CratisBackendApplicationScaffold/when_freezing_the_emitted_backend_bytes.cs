// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

public class when_freezing_the_emitted_backend_bytes : a_current_scaffold
{
    // Re-frozen for #783: the scaffold adds GlobalUsings.cs.
    const string FrozenDigest = "425cf5d8864ac1dca3b32f31e1de1a29b67ccabcdd5b8c73e88b8047af12e842";

    // Frozen when the license header became the caller's choice: generated C# carries none by default.
    const string SeparateSpecificationsDigest = "cd81f42a9bdba49d65d3110228ed7abb05a02002932bc118be05a9eceaba4549";

    // Frozen for #437: only the Matching package pin and host reference are added.
    const string PreviousFrozenDigest = "8a2ff497b626c46a26773cb8cb46b15c710cbc4edb940bfdd6a9f83e157b1475";

    // Re-frozen for #783: the repository-root scaffold adds the same GlobalUsings.cs.
    const string FrozenRepositoryRootDigest = "5bc8391242c7f98246ad635db77111c989979b1a26a7121c2bb721419d05aa2a";

    string _digest = null!;

    void Because() => _digest = Digest(_first);

    [Fact] void should_change_only_using_placement_from_the_previous_backend() => Digest(SeparateSpecificationsLayout.Restore(_first, "MyApp"), beforePlacement: true).ShouldEqual("19ce148c0d3128edae958d34b45dbfb68dd4d19cfe45277ca24d273dd8cf2779");
    [Fact] void should_change_only_the_globals_from_the_previous_backend() => Digest([.. _first.Where(input => !input.Name.EndsWith(":GlobalUsings.cs", StringComparison.Ordinal))]).ShouldEqual("da1b98905ac6381a425b2fedaec61033cc028316e3ba9386da6200875edc9aea");
    [Fact] void should_emit_the_frozen_backend_bytes() => _digest.ShouldEqual(FrozenDigest);
    [Fact] void should_change_only_the_specifications_layout_from_the_previous_frozen_backend() => Digest(SeparateSpecificationsLayout.Restore(_first, "MyApp"), legacyPlacement: true).ShouldEqual(SeparateSpecificationsDigest);
    [Fact] void should_change_only_the_license_header_from_the_previous_frozen_backend() => Digest(SeparateSpecificationsLayout.Restore(_first, "MyApp"), legacyHeader: true, legacyPlacement: true).ShouldEqual(PreviousFrozenDigest);
    [Fact] void should_emit_the_frozen_repository_root_bytes() => Digest(new Scaffolding.CratisBackendApplicationScaffold().Create(_request.WithRepositoryRoot())).ShouldEqual(FrozenRepositoryRootDigest);
    [Fact] void should_change_only_the_runtime_pins_from_the_previous_frozen_backend() => Digest(SeparateSpecificationsLayout.Restore(_first, "MyApp"), previousPins: true).ShouldEqual("8100938a3df827b3da55c7fbf560eff871ccee2333e3c2aeca253902e9ccea5a");

    static string Digest(ImmutableArray<ArtifactRenderInput> inputs, bool previousPins = false, bool beforePlacement = false, bool legacyHeader = false, bool legacyPlacement = false)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var input in inputs)
        {
            hash.AppendData(Encoding.UTF8.GetBytes(input.Name));
            var text = beforePlacement || previousPins ? QualifiedCommonOutput.BeforePlacement(Text(input)) : Text(input);

            // Before #424 the scaffold placed the generated imports inside the namespace.
            text = legacyPlacement ? text.Replace(QualifiedCommonOutput.Placement, QualifiedCommonOutput.Placement.Replace("outside_namespace", "inside_namespace", StringComparison.Ordinal), StringComparison.Ordinal) : text;
            text = beforePlacement || previousPins || legacyHeader ? LegacyLicenseHeader.Restore(input.Name, text) : text;
            hash.AppendData(Encoding.UTF8.GetBytes(FrozenScaffoldPins.Normalize(input.Name, text, previousPins)));
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }
}
