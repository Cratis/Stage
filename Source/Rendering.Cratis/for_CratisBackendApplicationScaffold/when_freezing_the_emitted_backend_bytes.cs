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
    const string FrozenDigest = "a0cfdc4fc47cea6b514005b319a8cd72b100e3b7f5e1ab5110fa96b59041646f";

    // Frozen when the license header became the caller's choice: generated C# carries none by default.
    const string SeparateSpecificationsDigest = "2683ed58d56eb103b2d1764d6b60c4d23cbea872fdf032513104e99e63d280dd";

    // Frozen for #437: only the Matching package pin and host reference are added.
    const string PreviousFrozenDigest = "4c72cdd160702dd61146d5d91d87bb563e500ab5a11ed0cc15398eb9a3995cb5";

    // Re-frozen for #424: the scaffold places using directives outside the namespace.
    // Re-frozen for #660: only Directory.Build.props gains CratisReadProtection On.
    // Re-frozen for Cratis/Stage#926: the application hosts an Orleans silo; every earlier digest re-frozen with it.
    // Re-frozen for Choreography 2.2.0 with Chronicle 19.124.0 and Arc 22.165.2; every earlier digest re-frozen with it.
    const string FrozenRepositoryRootDigest = "4d95a3acb7161ec082f98f5786f4965aa1f12c81a09e10234b377b1ac687a2e3";

    string _digest = null!;

    void Because() => _digest = Digest(_first);

    [Fact] void should_change_only_using_placement_from_the_previous_backend() => Digest(SeparateSpecificationsLayout.Restore(_first, "MyApp"), beforePlacement: true).ShouldEqual("bc00331ce1fbfcdddfbfcfdfbdba61098110c4c4860f771534f015d3286cf4a3");
    [Fact] void should_emit_the_frozen_backend_bytes() => _digest.ShouldEqual(FrozenDigest);
    [Fact] void should_change_only_the_specifications_layout_from_the_previous_frozen_backend() => Digest(SeparateSpecificationsLayout.Restore(_first, "MyApp"), legacyPlacement: true).ShouldEqual(SeparateSpecificationsDigest);
    [Fact] void should_change_only_the_license_header_from_the_previous_frozen_backend() => Digest(SeparateSpecificationsLayout.Restore(_first, "MyApp"), legacyHeader: true, legacyPlacement: true).ShouldEqual(PreviousFrozenDigest);
    [Fact] void should_emit_the_frozen_repository_root_bytes() => Digest(new Scaffolding.CratisBackendApplicationScaffold().Create(_request.WithRepositoryRoot())).ShouldEqual(FrozenRepositoryRootDigest);
    [Fact] void should_change_only_the_runtime_pins_from_the_previous_frozen_backend() => Digest(SeparateSpecificationsLayout.Restore(_first, "MyApp"), previousPins: true).ShouldEqual("44a0fd876b53ef1a8fa6d19b21f8a13f4ce014c5304d1ab4734d2deb586faa38");

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
