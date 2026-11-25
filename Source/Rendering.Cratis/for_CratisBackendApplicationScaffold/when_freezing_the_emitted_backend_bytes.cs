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

public class when_freezing_the_emitted_backend_bytes : a_version_3_scaffold
{
    // Re-frozen when specifications moved into the application project, compiled in Debug only.
    // Re-frozen for #424: the scaffold places using directives outside the namespace.
    // Re-frozen when GenerateProgramFile became unconditional, so a Release build after a Debug restore keeps one entry point.
    // Re-frozen for #660: only Directory.Build.props gains CratisReadProtection On.
    // Re-frozen when Debug specifications gained the Chronicle.Testing telemetry dependency.
    // Re-frozen for #120: authentication inputs, host wiring, sign-in instructions and Identity.Web references.
    // Re-frozen for #937: Directory.Packages.props pins Pkcs to 10.0.14 and documents Xml runtime servicing.
    // Re-frozen for Cratis/Stage#1116: the scaffold registers the provided value resolver beside the policies.
    // Re-frozen for Cratis/Stage#970: Program.cs omits event source ids from trace spans; every earlier digest re-frozen with it.
    const string FrozenDigest = "f13261c972aa9f0775b5be894e1647f17eeb8d51e61b18a6888abc9b5f892987";

    // Frozen when the license header became the caller's choice: generated C# carries none by default.
    const string SeparateSpecificationsDigest = "4f331010c6532a21141b2fd9a7b011111e8201c3dd643e945c82a17e164b8364";

    // Frozen for #437: only the Matching package pin and host reference are added.
    const string PreviousFrozenDigest = "fa49981d941b20b225262757f9279ea742abf2af715d1143924f48330febd8a2";

    // Re-frozen for #424: the scaffold places using directives outside the namespace.
    // Re-frozen for #660: only Directory.Build.props gains CratisReadProtection On.
    // Re-frozen for Cratis/Stage#926: the application hosts an Orleans silo; every earlier digest re-frozen with it.
    // Re-frozen for Choreography 2.2.0 with Chronicle 19.124.0 and Arc 22.165.2; every earlier digest re-frozen with it.
    const string FrozenRepositoryRootDigest = "4c0f7a0c66129f6a931eec78fe31e57ea21f18274dd6c7c7ea511a13d3f35198";

    string _digest = null!;

    void Because() => _digest = Digest(_first);

    [Fact] void should_change_only_using_placement_from_the_previous_backend() => Digest(SeparateSpecificationsLayout.Restore(_first, "MyApp"), beforePlacement: true).ShouldEqual("5afaddd18d38f2f7222faa6bb2cc27333b7b2cb244fed9de81f9d1de7c696ad3");
    [Fact] void should_emit_the_frozen_backend_bytes() => _digest.ShouldEqual(FrozenDigest);
    [Fact] void should_change_only_the_specifications_layout_from_the_previous_frozen_backend() => Digest(SeparateSpecificationsLayout.Restore(_first, "MyApp"), legacyPlacement: true).ShouldEqual(SeparateSpecificationsDigest);
    [Fact] void should_change_only_the_license_header_from_the_previous_frozen_backend() => Digest(SeparateSpecificationsLayout.Restore(_first, "MyApp"), legacyHeader: true, legacyPlacement: true).ShouldEqual(PreviousFrozenDigest);
    [Fact] void should_emit_the_frozen_repository_root_bytes() => Digest(new Scaffolding.CratisBackendApplicationScaffold().Create(_request.WithRepositoryRoot())).ShouldEqual(FrozenRepositoryRootDigest);
    [Fact] void should_change_only_the_runtime_pins_from_the_previous_frozen_backend() => Digest(SeparateSpecificationsLayout.Restore(_first, "MyApp"), previousPins: true).ShouldEqual("8de58cb132b7e24fefcaf4423e84fcc62f6e2e3afd838fae76ef49db8611f940");

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

            // Before #660 the scaffold left Arc read protection at its default.
            text = ShouldRestoreLegacyReadProtection(beforePlacement, previousPins, legacyHeader, legacyPlacement) ? LegacyReadProtection.Restore(input.Name, text) : text;
            hash.AppendData(Encoding.UTF8.GetBytes(FrozenScaffoldPins.Normalize(input.Name, text, previousPins)));
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    static bool ShouldRestoreLegacyReadProtection(bool beforePlacement, bool previousPins, bool legacyHeader, bool legacyPlacement) =>
        beforePlacement || previousPins || legacyHeader || legacyPlacement;
}
