// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Emission;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_LocalFileSystemOutput;

public class when_writing_a_failure_marker : Specification
{
    DirectoryInfo _target = null!;
    StringWriter _output = null!;
    bool _written;

    void Establish()
    {
        _target = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"stage-marker-{Guid.NewGuid():N}"));
        _output = new StringWriter();
    }

    async Task Because() => _written = await new LocalFileSystemOutput().TryWriteFailureMarker(_target, _output);

    void Destroy() => _target.Delete(recursive: true);

    [Fact] void should_create_the_marker() => _written.ShouldBeTrue();
    [Fact] void should_use_the_deterministic_path() => File.Exists(Path.Combine(_target.FullName, RenderFailureMarker.RelativePath)).ShouldBeTrue();
    [Fact] void should_state_that_the_marker_does_not_make_stale_output_safe() =>
        File.ReadAllText(Path.Combine(_target.FullName, RenderFailureMarker.RelativePath)).ShouldEqual(RenderFailureMarker.Content);
}
