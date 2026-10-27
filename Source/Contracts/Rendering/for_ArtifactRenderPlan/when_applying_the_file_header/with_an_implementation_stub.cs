// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.when_applying_the_file_header;

public class with_an_implementation_stub : for_ArtifactImplementationGap.given.a_gap
{
    ArtifactRenderPlan _plain = null!;
    ArtifactRenderPlan _headed = null!;
    ArtifactRenderPlan _again = null!;

    void Because()
    {
        _artifacts = [PlannedArtifact.CreateText(_stub.RelativePath, "start" + new string(' ', 115) + new string('x', 80) + "end", [_request.Model.Application.Id])];
        _plain = ArtifactRenderPlan.Create(_request, _artifacts, [], [_gap]);
        var request = _request with { Profile = ArtifactFileHeader.With(_request.Profile, ["Ærlig \U0001F600"]) };
        _headed = ArtifactRenderPlan.Create(request, _artifacts, [], [_gap]);
        _again = ArtifactRenderPlan.Create(request, _headed.Artifacts, [], _headed.ImplementationGaps, _headed.Refusals, _headed.RequiredRemovals);
    }

    [Fact] void should_shift_by_utf16_units() => _headed.ImplementationGaps.Single().Stubs.Single().BodyLocator.Start.ShouldEqual(_stub.BodyLocator.Start + "// Ærlig \U0001F600\n\n".Length);
    [Fact] void should_locate_the_same_body() => Body(_headed).ShouldEqual(Body(_plain));
    [Fact] void should_not_shift_again() => _again.ImplementationGaps.Single().Stubs.Single().BodyLocator.ShouldEqual(_headed.ImplementationGaps.Single().Stubs.Single().BodyLocator);
    [Fact] void should_keep_the_digest() => _again.Digest.ShouldEqual(_headed.Digest);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_clear_gaps_and_removals_on_refusal(bool malformed)
    {
        var input = malformed ? ArtifactRenderInput.Create(ArtifactFileHeader.InputName, "2", ArtifactFileHeader.CreateInput(["hello"]).Bytes) : ArtifactFileHeader.CreateInput(["\\u000a"]);
        var profile = ArtifactRenderProfile.Create(_request.Profile.Target, _request.Profile.TargetVersion, _request.Profile.Renderer, _request.Profile.RendererVersion, [.. _request.Profile.Inputs, input]);
        var request = _request with { Profile = profile };
        var removal = new ArtifactRemoval("old.cs", _artifacts[0].Sha256, _request.Model.Application.Id);
        ArtifactRenderPlan.Create(_request, _artifacts, [], [_gap], [], [removal]).RequiredRemovals.Single().ShouldEqual(removal);
        var result = ArtifactRenderPlan.Create(request, [.. _artifacts, PlannedArtifact.CreateText("Code.java", "code")], [], [_gap], [], [removal]);
        result.ImplementationGaps.ShouldBeEmpty();
        result.RequiredRemovals.ShouldBeEmpty();
        result.Artifacts.ShouldBeEmpty();
        result.Diagnostics.Single().Code.ShouldEqual(malformed ? "STAGE-HEADER-001" : "STAGE-HEADER-002");
    }

    static string Body(ArtifactRenderPlan plan)
    {
        var stub = plan.ImplementationGaps.Single().Stubs.Single();
        return Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath == stub.RelativePath).Bytes.AsSpan()).Substring(stub.BodyLocator.Start, stub.BodyLocator.Length);
    }
}
