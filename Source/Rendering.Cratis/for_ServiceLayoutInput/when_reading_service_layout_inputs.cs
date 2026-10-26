// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ServiceLayoutInput;

public class when_reading_service_layout_inputs : Specification
{
    ArtifactRenderProfile _plain = null!;
    ArtifactRenderProfile _layout = null!;
    ArtifactRenderProfile _duplicate = null!;
    ArtifactRenderProfile _wrongVersion = null!;
    ArtifactRenderProfile _wrongBytes = null!;
    bool _plainLayout;
    bool _layoutLayout;

    void Because()
    {
        _plain = CratisRendering.CreateProfile("Projects", new("Projects", "Projects"));
        _layout = CratisRendering.CreateProfile("Projects", new("Projects", "Projects") { ServiceLayout = true });
        _duplicate = With(_layout, ArtifactRenderInput.Create(ServiceLayoutInput.Name, "2", [.. "service"u8.ToArray()]));
        _wrongVersion = With(_plain, ArtifactRenderInput.Create(ServiceLayoutInput.Name, "2", [.. "service"u8.ToArray()]));
        _wrongBytes = With(_plain, ArtifactRenderInput.Create(ServiceLayoutInput.Name, ServiceLayoutInput.Version, [.. "other"u8.ToArray()]));
        ServiceLayoutInput.TryRead(_plain, out _plainLayout);
        ServiceLayoutInput.TryRead(_layout, out _layoutLayout);
    }

    [Fact] void should_accept_an_absent_input_as_the_default() => (ServiceLayoutInput.TryRead(_plain, out _) && !_plainLayout).ShouldBeTrue();
    [Fact] void should_not_carry_the_input_by_default() => _plain.Inputs.Any(input => input.Name == ServiceLayoutInput.Name).ShouldBeFalse();
    [Fact] void should_read_a_valid_input() => (ServiceLayoutInput.TryRead(_layout, out _) && _layoutLayout).ShouldBeTrue();
    [Fact] void should_refuse_a_duplicate_input() => ServiceLayoutInput.TryRead(_duplicate, out _).ShouldBeFalse();
    [Fact] void should_refuse_a_wrong_version() => ServiceLayoutInput.TryRead(_wrongVersion, out _).ShouldBeFalse();
    [Fact] void should_refuse_wrong_bytes() => ServiceLayoutInput.TryRead(_wrongBytes, out _).ShouldBeFalse();

    static ArtifactRenderProfile With(ArtifactRenderProfile profile, ArtifactRenderInput input) =>
        ArtifactRenderProfile.Create(profile.Target, profile.TargetVersion, profile.Renderer, profile.RendererVersion, [.. profile.Inputs, input]);
}
