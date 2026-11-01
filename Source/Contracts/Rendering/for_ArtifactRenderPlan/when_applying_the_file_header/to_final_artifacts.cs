// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.when_applying_the_file_header;

public class to_final_artifacts : given.an_artifact_render_request
{
    [Fact] void should_preserve_the_unheaded_digest()
    {
        var artifacts = new[] { PlannedArtifact.CreateText("b.cs", "b", [_request.Model.Application.Id]), PlannedArtifact.CreateText("a.cs", "a") };
        ArtifactRenderPlan.Create(_request, [.. artifacts], []).Digest.ShouldEqual(ArtifactRenderPlan.ComputeDigest(_request.Profile.Target, _request.Profile.TargetVersion, _request.Profile.Renderer, _request.Profile.RendererVersion, _request.Model.Application.Name, [.. artifacts], []));
    }

    [Fact] void should_preserve_content_sources_and_binary_bytes()
    {
        var json = PlannedArtifact.CreateText("data.json", "{}");
        var binary = PlannedArtifact.CreateBinary("blob.cs", [1, 2]);
        var plain = ArtifactRenderPlan.Create(_request, [PlannedArtifact.CreateText("Code.cs", "code", [_request.Model.Application.Id]), json, binary], []);
        var request = _request with { Profile = ArtifactFileHeader.With(_request.Profile, ["hello"]) };
        var headed = ArtifactRenderPlan.Create(request, plain.Artifacts, []);
        headed.Artifacts.Single(artifact => artifact.RelativePath == "Code.cs").Sources.ShouldEqual(new[] { _request.Model.Application.Id });
        Text(headed.Artifacts.Single(artifact => artifact.RelativePath == "Code.cs")).ShouldEqual("// hello\n\ncode");
        headed.Artifacts.Single(artifact => artifact.RelativePath == "data.json").Sha256.ShouldEqual(json.Sha256);
        headed.Artifacts.Single(artifact => artifact.RelativePath == "blob.cs").Sha256.ShouldEqual(binary.Sha256);
        headed.Digest.ShouldNotEqual(plain.Digest);
        ArtifactRenderPlan.Create(request, plain.Artifacts, []).Digest.ShouldEqual(headed.Digest);
        ArtifactRenderPlan.Create(request with { Profile = ArtifactFileHeader.With(_request.Profile, ["changed"]) }, plain.Artifacts, []).Digest.ShouldNotEqual(headed.Digest);
    }

    [Fact] void should_preserve_the_marker_through_removal_overloads()
    {
        var request = _request with { Profile = ArtifactFileHeader.With(_request.Profile, ["hello"]) };
        var first = ArtifactRenderPlan.Create(request, [PlannedArtifact.CreateText("Code.cs", "// hello\n\ncode")], [], [], [], []);
        var second = ArtifactRenderPlan.Create(request, first.Artifacts, first.Diagnostics, first.ImplementationGaps, first.Refusals, first.RequiredRemovals);
        second.Digest.ShouldEqual(first.Digest);
        second.Artifacts.Single().Bytes.ShouldEqual(first.Artifacts.Single().Bytes);
        second.Artifacts.Single().AppliedFileHeaderSha256.ShouldEqual(first.Artifacts.Single().AppliedFileHeaderSha256);
        Text(second.Artifacts.Single()).ShouldEqual("// hello\n\n// hello\n\ncode");
    }

    [Fact] void should_allow_custom_comment_syntax()
    {
        var request = _request with { Profile = ArtifactFileHeader.With(_request.Profile, ["hello"]), CommentSyntaxes = ArtifactCommentSyntaxes.Default.With(".py", new("#", [])) };
        Text(ArtifactRenderPlan.Create(request, [PlannedArtifact.CreateText("code.py", "code")], []).Artifacts.Single()).ShouldEqual("# hello\n\ncode");
    }

    [Fact] void should_refuse_java_before_publishing()
    {
        var request = _request with { Profile = ArtifactFileHeader.With(_request.Profile, ["\\uuuu0041"]) };
        var result = ArtifactRenderPlan.Create(request, [PlannedArtifact.CreateText("Code.java", "code")], []);
        result.Outcome.ShouldEqual(ArtifactRenderOutcome.Refused);
        result.Artifacts.ShouldBeEmpty();
        result.Diagnostics.Single().Code.ShouldEqual("STAGE-HEADER-002");
    }

    static string Text(PlannedArtifact artifact) => Encoding.UTF8.GetString(artifact.Bytes.AsSpan());
}
