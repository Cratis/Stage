// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisPlanOptions.when_writing_file_headers;

public class with_a_header_and_a_performer_gap : for_CratisRendering.given.a_source_plan
{
    const string Header = "// Ærlig \U0001F600\n\n";
    ArtifactRenderPlan _plain = null!;
    ArtifactRenderPlan _headed = null!;
    ArtifactRenderPlan _again = null!;

    void Establish() => File.WriteAllText(Path.Combine(_root, "performers.play"), """
        module Projects
          feature Registration
            slice StateView ChangeRequests
              readmodel ChangeRequest
                talkId Uuid key
                requestedAt DateTime
              query AllChangeRequests => observable ChangeRequest[]
                performer
                  ```csharp
                  return readModels.OrderBy(request => request.RequestedAt);
                  ```
        """);

    async Task Because()
    {
        var loaded = await SemanticModelLoader.LoadAsync(_root, [], null, _planOptions.ApplicationName);
        Assert.True(loaded.Success, string.Join("; ", loaded.Diagnostics));
        _plain = CratisRendering.PlanFrom(loaded.Loaded!, _featureSelection, _planOptions).Plan!;
        _headed = CratisRendering.PlanFrom(loaded.Loaded!, _featureSelection, _planOptions with { FileHeader = ["Ærlig \U0001F600"] }).Plan!;
        var request = _request with { Profile = ArtifactFileHeader.With(_request.Profile, ["Ærlig \U0001F600"]) };
        _again = ArtifactRenderPlan.Create(request, _headed.Artifacts, _headed.Diagnostics, _headed.ImplementationGaps, _headed.Refusals, _headed.RequiredRemovals);
    }

    [Fact] void should_have_a_real_performer_gap()
    {
        _headed.Outcome.ShouldEqual(ArtifactRenderOutcome.Partial);
        _headed.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-IMPL-006");
        _headed.ImplementationGaps.Single().Role.ShouldEqual(SemanticImplementationRole.QueryPerformer);
    }

    [Fact] void should_shift_the_locator_by_the_utf16_header_length() => Stub(_headed).BodyLocator.Start.ShouldEqual(Stub(_plain).BodyLocator.Start + Header.Length);
    [Fact] void should_preserve_the_stub_signature() => Stub(_headed).Signature.ShouldEqual(Stub(_plain).Signature);
    [Fact] void should_locate_the_identical_stub_body() => Body(_headed).ShouldEqual(Body(_plain));
    [Fact] void should_point_to_the_throwing_body() => Body(_headed).ShouldContain("throw new NotImplementedException(");
    [Fact] void should_prepend_the_header_to_the_actual_slice() => Text(_headed).ShouldEqual(Header + Text(_plain));
    [Fact] void should_preserve_the_locator_on_reconstruction() => Stub(_again).BodyLocator.ShouldEqual(Stub(_headed).BodyLocator);
    [Fact] void should_preserve_the_digest_on_reconstruction() => _again.Digest.ShouldEqual(_headed.Digest);

    static ArtifactImplementationStub Stub(ArtifactRenderPlan plan) => plan.ImplementationGaps.Single().Stubs.Single();
    static string Text(ArtifactRenderPlan plan) => Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath == Stub(plan).RelativePath).Bytes.AsSpan());
    static string Body(ArtifactRenderPlan plan) => Text(plan).Substring(Stub(plan).BodyLocator.Start, Stub(plan).BodyLocator.Length);
}
