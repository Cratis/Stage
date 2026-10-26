// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Cryptography;
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_preserving_empty_domain_policy_output;

public class without_a_domain : given.a_domain_policy_plan
{
    string _digest = null!;
    string _qualifiedDigest = null!;

    void Because()
    {
        var result = Plan(string.Empty);
        result.Success.ShouldBeTrue();

        var qualified = QualifiedCommonOutput.Restore(result.Plan!, () => Plan(string.Empty).Plan!);

        // Restoring only the initializer identifier must reproduce both complete HEAD path/byte pins.
        // These assertions confine the updated pins below to that one generated declaration change.
        // Since #424 the output itself uses short names, so the HEAD pins are reproduced from the qualified restore.
        Digest(BeforeInitializerRename(LegacyLicenseHeader.Restore(qualified))).ShouldEqual("0B882F2BC3D6D8157D3116BBFDA61AC3A79DFDAA00A4C1DA4ED508DCB0DBF015");

        // Restoring the former license header must reproduce the pins from before headers became optional.
        Digest(LegacyLicenseHeader.Restore(qualified)).ShouldEqual("9D2D04A6885FBC66D2A14ED794DDEE5202D65343904974FD000F86D0E4F39A70");
        _digest = Digest(result.Artifacts);
        _qualifiedDigest = Digest(qualified);
    }

    static IEnumerable<PlannedArtifact> BeforeInitializerRename(IEnumerable<PlannedArtifact> artifacts) => artifacts.Select(artifact =>
        artifact.RelativePath.StartsWith("GeneratedPolicies/", StringComparison.Ordinal) && artifact.RelativePath.EndsWith("Policy.cs", StringComparison.Ordinal)
            ? PlannedArtifact.CreateText(
                artifact.RelativePath,
                Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Replace("internal static void InitializeGeneratedAuthorization()", "internal static void RegisterPolicy()", StringComparison.Ordinal),
                artifact.Sources)
            : artifact);

    static string Digest(IEnumerable<PlannedArtifact> artifacts)
    {
        // Pins the unchanged payload: paths, wrappers, bodies, policies and generated specs. Managed reports have separate byte goldens.
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var artifact in artifacts.Where(artifact => !string.Equals(artifact.RelativePath, ".stage/completeness.json", StringComparison.Ordinal) && !string.Equals(artifact.RelativePath, "UNSUPPORTED.md", StringComparison.Ordinal)).OrderBy(artifact => artifact.RelativePath, StringComparer.Ordinal))
        {
            hash.AppendData(Encoding.UTF8.GetBytes(artifact.RelativePath + "\n"));
            hash.AppendData(artifact.Bytes.AsSpan());
        }
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    // Re-frozen for #424: generated C# uses short type names and using directives.
    [Fact] void should_keep_all_paths_and_bytes() => _digest.ShouldEqual("2FAB07125C8F373F3BA852F53E4758E6565620719D4001F2F7153C162A86E975");
    [Fact] void should_confine_changes_to_common_and_using_placement() => _qualifiedDigest.ShouldEqual("05777F401D263E5BC39126494FB4912F7096AA91C5F3F5E22AE41AA55D20D689");
}
#endif
