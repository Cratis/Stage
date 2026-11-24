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
    // Re-frozen for Cratis/Stage#1146 and Cratis/Arc#3975: only Program.cs's availability registration moves before AddCratis.
    const string FrozenDigest = "035ab5074060be7be976410cba66915c51f6032c16bf515e76024ccce18723dc";
    const string BeforeAvailabilityOrderDigest = "5953421156eaade3b1acae982b7e93388eaaae1fcfc2479f4333b0e911bcd079";

    // Frozen when the license header became the caller's choice: generated C# carries none by default.
    const string SeparateSpecificationsDigest = "07618b2b6c61da148c95e4cd1e05787f862cfc63fa273c3af26200c7cc761818";

    // Frozen for #437: only the Matching package pin and host reference are added.
    const string PreviousFrozenDigest = "6603cec90dd2d749d56cdf98857388c45c4864fe1c6df171e7e8fdf25887c37d";

    // Re-frozen for #424: the scaffold places using directives outside the namespace.
    // Re-frozen for #660: only Directory.Build.props gains CratisReadProtection On.
    // Re-frozen for Cratis/Stage#926: the application hosts an Orleans silo; every earlier digest re-frozen with it.
    // Re-frozen for Choreography 2.2.0 with Chronicle 19.124.0 and Arc 22.165.2; every earlier digest re-frozen with it.
    // Re-frozen for the same Program.cs-only availability ordering change as the standalone backend.
    const string FrozenRepositoryRootDigest = "ce89e50b82f069e6ac4d24a5aa08d06e5ea1f2730b3e5163cd3cd100fc7da101";
    const string BeforeAvailabilityOrderRepositoryRootDigest = "8f167be7ddcb0305973246d6ff5eeb52db974272d82d0041a7249a970794898d";

    string _digest = null!;
    ImmutableArray<ArtifactRenderInput> _beforeAvailabilityOrder;

    void Because()
    {
        _digest = Digest(_first);
        _beforeAvailabilityOrder = AvailabilityRegistrationOrder.Restore(_first, "MyApp");
    }

    [Fact] void should_change_only_the_program_availability_registration_order_from_the_previous_frozen_backend() => Digest(_beforeAvailabilityOrder).ShouldEqual(BeforeAvailabilityOrderDigest);
    [Fact] void should_change_only_the_program_availability_registration_order_from_the_previous_frozen_repository_root() => Digest(AvailabilityRegistrationOrder.Restore(new Scaffolding.CratisBackendApplicationScaffold().Create(_request.WithRepositoryRoot()), "MyApp")).ShouldEqual(BeforeAvailabilityOrderRepositoryRootDigest);

    [Fact] void should_change_only_using_placement_from_the_previous_backend() => Digest(SeparateSpecificationsLayout.Restore(_beforeAvailabilityOrder, "MyApp"), beforePlacement: true).ShouldEqual("03c329802671c89553eab0b7f8ae1cff65e3a4c028d6ce51d79ef18871135b95");
    [Fact] void should_emit_the_frozen_backend_bytes() => _digest.ShouldEqual(FrozenDigest);
    [Fact] void should_change_only_the_specifications_layout_from_the_previous_frozen_backend() => Digest(SeparateSpecificationsLayout.Restore(_beforeAvailabilityOrder, "MyApp"), legacyPlacement: true).ShouldEqual(SeparateSpecificationsDigest);
    [Fact] void should_change_only_the_license_header_from_the_previous_frozen_backend() => Digest(SeparateSpecificationsLayout.Restore(_beforeAvailabilityOrder, "MyApp"), legacyHeader: true, legacyPlacement: true).ShouldEqual(PreviousFrozenDigest);
    [Fact] void should_emit_the_frozen_repository_root_bytes() => Digest(new Scaffolding.CratisBackendApplicationScaffold().Create(_request.WithRepositoryRoot())).ShouldEqual(FrozenRepositoryRootDigest);
    [Fact] void should_change_only_the_runtime_pins_from_the_previous_frozen_backend() => Digest(SeparateSpecificationsLayout.Restore(_beforeAvailabilityOrder, "MyApp"), previousPins: true).ShouldEqual("0b4f6fcc41ce61cdd4057e8ddfc9df162d0d49f1f1a981aaa495506bec37f698");

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
