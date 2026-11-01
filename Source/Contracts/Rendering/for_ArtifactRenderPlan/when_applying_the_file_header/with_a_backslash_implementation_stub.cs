// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.when_applying_the_file_header;

public class with_a_backslash_implementation_stub : for_ArtifactImplementationGap.given.a_gap
{
    ArtifactRenderPlan _headed = null!;
    ArtifactRenderPlan _again = null!;
    string _body = string.Empty;

    void Establish()
    {
        var input = ArtifactFileHeader.CreateInput(["hello"]);
        _request = _request with { Profile = ArtifactRenderProfile.Create(_request.Profile.Target, _request.Profile.TargetVersion, _request.Profile.Renderer, _request.Profile.RendererVersion, [input]) };
        _stub = _stub with { RelativePath = @"Features\RegisterPayloads.cs" };
        _gap = _gap with { Stubs = [_stub] };
        _body = new string('x', _stub.BodyLocator.Length);
        _artifacts = [PlannedArtifact.CreateText(_stub.RelativePath, new string(' ', _stub.BodyLocator.Start) + _body)];
    }

    void Because()
    {
        _headed = ArtifactRenderPlan.Create(_request, _artifacts, [], [_gap]);
        _again = ArtifactRenderPlan.Create(_request, _headed.Artifacts, _headed.Diagnostics, _headed.ImplementationGaps, _headed.Refusals, _headed.RequiredRemovals);
    }

    [Fact] void should_normalize_the_artifact_path() => _headed.Artifacts.Single().RelativePath.ShouldEqual("Features/RegisterPayloads.cs");
    [Fact] void should_normalize_the_stub_path() => _headed.ImplementationGaps.Single().Stubs.Single().RelativePath.ShouldEqual("Features/RegisterPayloads.cs");
    [Fact] void should_shift_the_body_locator() => _headed.ImplementationGaps.Single().Stubs.Single().BodyLocator.Start.ShouldEqual(_stub.BodyLocator.Start + "// hello\n\n".Length);
    [Fact] void should_keep_the_body_length() => _headed.ImplementationGaps.Single().Stubs.Single().BodyLocator.Length.ShouldEqual(_stub.BodyLocator.Length);
    [Fact] void should_keep_the_shift_on_recreation() => _again.ImplementationGaps.Single().Stubs.Single().BodyLocator.ShouldEqual(_headed.ImplementationGaps.Single().Stubs.Single().BodyLocator);
    [Fact] void should_keep_the_digest_on_recreation() => _again.Digest.ShouldEqual(_headed.Digest);

    [Fact]
    void should_locate_the_original_body()
    {
        var locator = _headed.ImplementationGaps.Single().Stubs.Single().BodyLocator;
        Encoding.UTF8.GetString(_headed.Artifacts.Single().Bytes.AsSpan()).Substring(locator.Start, locator.Length).ShouldEqual(_body);
    }
}
