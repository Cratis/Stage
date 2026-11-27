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
    // Re-frozen for Cratis/Stage#1146 and Cratis/Arc#3975 together with main's DiagnosticSource 11 and proxy-generator runtime wiring.
    const string FrozenDigest = "4ed967be9bbde1b65efffbd5ed9424ac64a25fe8ec8e422080b32bc9ba0836e3";
    const string BeforeAvailabilityOrderDigest = "52547908f1c9728c56d57ec7d5889e1dd02ffe19b8f09faae8f1911aad99edaf";

    // Frozen when the license header became the caller's choice: generated C# carries none by default.
    const string SeparateSpecificationsDigest = "4ec8907a6aff9d1af0a4ed58e2ba35bef643933a76e223a672198d240b3e1988";

    // Frozen for #437: only the Matching package pin and host reference are added.
    const string PreviousFrozenDigest = "680f6cfee3e3738d08d5a97e9270be342d6a842192be2d9fd835c648f662f3a1";

    // Re-frozen for #424: the scaffold places using directives outside the namespace.
    // Re-frozen for #660: only Directory.Build.props gains CratisReadProtection On.
    // Re-frozen for Cratis/Stage#926: the application hosts an Orleans silo; every earlier digest re-frozen with it.
    // Re-frozen for Choreography 2.2.0 with Chronicle 19.124.0 and Arc 22.165.2; every earlier digest re-frozen with it.
    // Re-frozen for the same availability ordering and runtime wiring changes as the standalone backend.
    const string FrozenRepositoryRootDigest = "32f927380c94ce18d1b3dd26fc5c7077ff9f2a137e44d7b32a1a27d67e92f87c";
    const string BeforeAvailabilityOrderRepositoryRootDigest = "572c76a734d6c01b6e3490e86151c4f771f1db3ed879bafd7e9e970b6bb885e7";

    string _digest = null!;
    ImmutableArray<ArtifactRenderInput> _beforeAvailabilityOrder;

    void Because()
    {
        _digest = Digest(_first);
        _beforeAvailabilityOrder = AvailabilityRegistrationOrder.Restore(_first, "MyApp");
    }

    [Fact] void should_change_only_the_program_availability_registration_order_from_the_previous_frozen_backend() => Digest(_beforeAvailabilityOrder).ShouldEqual(BeforeAvailabilityOrderDigest);
    [Fact] void should_change_only_the_program_availability_registration_order_from_the_previous_frozen_repository_root() => Digest(AvailabilityRegistrationOrder.Restore(new Scaffolding.CratisBackendApplicationScaffold().Create(_request.WithRepositoryRoot()), "MyApp")).ShouldEqual(BeforeAvailabilityOrderRepositoryRootDigest);

    [Fact] void should_change_only_using_placement_from_the_previous_backend() => Digest(SeparateSpecificationsLayout.Restore(_beforeAvailabilityOrder, "MyApp"), beforePlacement: true).ShouldEqual("10c810272f0aecb2112ee8fa1e4506fa0490ea474ef18b6111f89bb34fb5190b");
    [Fact] void should_emit_the_frozen_backend_bytes() => _digest.ShouldEqual(FrozenDigest);
    [Fact] void should_change_only_the_specifications_layout_from_the_previous_frozen_backend() => Digest(SeparateSpecificationsLayout.Restore(_beforeAvailabilityOrder, "MyApp"), legacyPlacement: true).ShouldEqual(SeparateSpecificationsDigest);
    [Fact] void should_change_only_the_license_header_from_the_previous_frozen_backend() => Digest(SeparateSpecificationsLayout.Restore(_beforeAvailabilityOrder, "MyApp"), legacyHeader: true, legacyPlacement: true).ShouldEqual(PreviousFrozenDigest);
    [Fact] void should_emit_the_frozen_repository_root_bytes() => Digest(new Scaffolding.CratisBackendApplicationScaffold().Create(_request.WithRepositoryRoot())).ShouldEqual(FrozenRepositoryRootDigest);
    [Fact] void should_change_only_the_runtime_pins_from_the_previous_frozen_backend() => Digest(SeparateSpecificationsLayout.Restore(_beforeAvailabilityOrder, "MyApp"), previousPins: true).ShouldEqual("0f6ccbc85a02355ebacc20bb139816584c371eaf66762e5d21a2466ce9d8bf5f");

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
