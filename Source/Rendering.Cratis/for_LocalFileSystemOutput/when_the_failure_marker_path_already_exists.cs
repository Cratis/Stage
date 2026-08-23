// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Emission;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_LocalFileSystemOutput;

public class when_the_failure_marker_path_already_exists : Specification
{
    const string ExistingContent = "User-owned content";
    DirectoryInfo _target = null!;
    bool _written;

    void Establish()
    {
        _target = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"stage-marker-{Guid.NewGuid():N}"));
        File.WriteAllText(Path.Combine(_target.FullName, RenderFailureMarker.RelativePath), ExistingContent);
    }

    async Task Because() =>
        _written = await new LocalFileSystemOutput().TryWriteFailureMarker(_target, TextWriter.Null);

    void Destroy() => _target.Delete(recursive: true);

    [Fact] void should_not_claim_to_have_created_the_marker() => _written.ShouldBeFalse();
    [Fact] void should_not_overwrite_the_existing_file() =>
        File.ReadAllText(Path.Combine(_target.FullName, RenderFailureMarker.RelativePath)).ShouldEqual(ExistingContent);
}
