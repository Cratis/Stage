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
    const string FrozenDigest = "5a3f52d7d1e00167f2b418956bff9c1db6f778628d05e6f0c89ff961e6806c93";
    const string BeforeAvailabilityOrderDigest = "e4c31b59796a05b213b9491e7861baa8740b9d821d906ea68b75d1e6c03162a1";

    // Frozen when the license header became the caller's choice: generated C# carries none by default.
    const string SeparateSpecificationsDigest = "f2b70a7f5447cc83ee4b5e6b22827f03dd65bb9e08c9d47295847b04fc113f9e";

    // Frozen for #437: only the Matching package pin and host reference are added.
    const string PreviousFrozenDigest = "8b59ce8302b265795138c2377b92b4512a4ee1e156c401176dcdd61315b96ea3";

    // Re-frozen for #424: the scaffold places using directives outside the namespace.
    // Re-frozen for #660: only Directory.Build.props gains CratisReadProtection On.
    // Re-frozen for Cratis/Stage#926: the application hosts an Orleans silo; every earlier digest re-frozen with it.
    // Re-frozen for Choreography 2.2.0 with Chronicle 19.124.0 and Arc 22.165.2; every earlier digest re-frozen with it.
    // Re-frozen for the same availability ordering and runtime wiring changes as the standalone backend.
    const string FrozenRepositoryRootDigest = "4f1e49dc52a5ed656585967f87638e2e866b12157564b307698b09e3bdc33f7f";
    const string BeforeAvailabilityOrderRepositoryRootDigest = "b40048c5193d0ebdf8e68dcd805db45835549622bfabda5706ee87d57f204cfa";

    string _digest = null!;
    ImmutableArray<ArtifactRenderInput> _beforeAvailabilityOrder;

    void Because()
    {
        _digest = Digest(_first);
        _beforeAvailabilityOrder = AvailabilityRegistrationOrder.Restore(_first, "MyApp");
    }

    [Fact] void should_change_only_the_program_availability_registration_order_from_the_previous_frozen_backend() => Digest(_beforeAvailabilityOrder).ShouldEqual(BeforeAvailabilityOrderDigest);
    [Fact] void should_change_only_the_program_availability_registration_order_from_the_previous_frozen_repository_root() => Digest(AvailabilityRegistrationOrder.Restore(new Scaffolding.CratisBackendApplicationScaffold().Create(_request.WithRepositoryRoot()), "MyApp")).ShouldEqual(BeforeAvailabilityOrderRepositoryRootDigest);

    [Fact] void should_change_only_using_placement_from_the_previous_backend() => Digest(SeparateSpecificationsLayout.Restore(_beforeAvailabilityOrder, "MyApp"), beforePlacement: true).ShouldEqual("9a7f13b3c99d6124229f152f1e3203b53a9f326d1e672617baa135357b170c62");
    [Fact] void should_emit_the_frozen_backend_bytes() => _digest.ShouldEqual(FrozenDigest);
    [Fact] void should_change_only_the_specifications_layout_from_the_previous_frozen_backend() => Digest(SeparateSpecificationsLayout.Restore(_beforeAvailabilityOrder, "MyApp"), legacyPlacement: true).ShouldEqual(SeparateSpecificationsDigest);
    [Fact] void should_change_only_the_license_header_from_the_previous_frozen_backend() => Digest(SeparateSpecificationsLayout.Restore(_beforeAvailabilityOrder, "MyApp"), legacyHeader: true, legacyPlacement: true).ShouldEqual(PreviousFrozenDigest);
    [Fact] void should_emit_the_frozen_repository_root_bytes() => Digest(new Scaffolding.CratisBackendApplicationScaffold().Create(_request.WithRepositoryRoot())).ShouldEqual(FrozenRepositoryRootDigest);
    [Fact] void should_change_only_the_runtime_pins_from_the_previous_frozen_backend() => Digest(SeparateSpecificationsLayout.Restore(_beforeAvailabilityOrder, "MyApp"), previousPins: true).ShouldEqual("5f2320acdb9556d4f86f089efe4ea7354ca2c45521e663b89c4b11502a17b4a7");

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
