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

        _digest = Digest(result.Artifacts);
        _qualifiedDigest = Digest(QualifiedCommonOutput.Restore(_loaded.Model, result.Plan!, new("InvoiceApp", "Invoices")));
    }

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

    [Fact] void should_keep_all_paths_and_bytes() => _digest.ShouldEqual("29CF0110771FDE60057302A56DA77E52320D3545FBCD8E794022B40FB13285E0");
    [Fact] void should_confine_changes_to_common_and_using_placement() => _qualifiedDigest.ShouldEqual("11D1ABE1115C209EBF40C8CA891EB17F1137C3A0E752FD3905ECF1FDA038230E");
}
#endif
