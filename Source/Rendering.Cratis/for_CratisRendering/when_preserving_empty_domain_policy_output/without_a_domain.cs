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

        var qualified = QualifiedCommonOutput.Restore(_loaded.Model, result.Plan!, new("InvoiceApp", "Invoices"));

        // Restoring only the initializer identifier must reproduce both complete HEAD path/byte pins.
        // These assertions confine the updated pins below to that one generated declaration change.
        Digest(BeforeInitializerRename(result.Artifacts)).ShouldEqual("8C7B38ABBD21DED65A68BAAEF4E69595AFBF11C21D573E5C1BAB31EA8A1A1292");
        Digest(BeforeInitializerRename(qualified)).ShouldEqual("0B882F2BC3D6D8157D3116BBFDA61AC3A79DFDAA00A4C1DA4ED508DCB0DBF015");
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
        // Pins the complete output, including paths, wrappers, bodies, policies and generated specs.
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var artifact in artifacts.OrderBy(artifact => artifact.RelativePath, StringComparer.Ordinal))
        {
            hash.AppendData(Encoding.UTF8.GetBytes(artifact.RelativePath + "\n"));
            hash.AppendData(artifact.Bytes.AsSpan());
        }
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    [Fact] void should_keep_all_paths_and_bytes() => _digest.ShouldEqual("F72648610254219C75E6EFCA82EEBB9030253CFFACE8DE14F2A4B344D3DD6BD2");
    [Fact] void should_confine_changes_to_common_and_using_placement() => _qualifiedDigest.ShouldEqual("9D2D04A6885FBC66D2A14ED794DDEE5202D65343904974FD000F86D0E4F39A70");
}
#endif
