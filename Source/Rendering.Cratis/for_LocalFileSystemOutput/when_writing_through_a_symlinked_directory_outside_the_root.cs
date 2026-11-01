// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Emission;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_LocalFileSystemOutput;

public class when_writing_through_a_symlinked_directory_outside_the_root : Specification
{
    DirectoryInfo _temporary = null!;
    DirectoryInfo _target = null!;
    DirectoryInfo _outside = null!;
    bool _symlinksSupported = true;
    Exception? _error;

    void Establish()
    {
        _temporary = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"stage-output-{Guid.NewGuid():N}"));
        _target = _temporary.CreateSubdirectory("out");
        _outside = _temporary.CreateSubdirectory("outside");
        try
        {
            Directory.CreateSymbolicLink(Path.Combine(_target.FullName, "link"), _outside.FullName);
        }
        catch (Exception error) when (OperatingSystem.IsWindows() && (error is UnauthorizedAccessException or IOException))
        {
            // Unprivileged Windows may not allow symbolic links; Linux CI always exercises the write.
            _symlinksSupported = false;
        }
    }

    async Task Because()
    {
        if (_symlinksSupported)
        {
            _error = await Catch.Exception(() => new LocalFileSystemOutput().Write(new RenderedFile("link/x.cs", "content"), _target, TextWriter.Null));
        }
    }

    void Destroy() => _temporary.Delete(recursive: true);

    [Fact] void should_refuse_the_path() => (!_symlinksSupported || _error is GeneratedPathOutsideOutputRoot).ShouldBeTrue();
    [Fact] void should_not_write_outside_the_root() => File.Exists(Path.Combine(_outside.FullName, "x.cs")).ShouldBeFalse();
    [Fact] void should_leave_the_outside_directory_empty() => _outside.GetFileSystemInfos().ShouldBeEmpty();
}
