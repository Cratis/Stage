// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Emission;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_LocalFileSystemOutput;

public class when_writing_through_a_symlinked_file_outside_the_root : Specification
{
    const string ExistingContent = "User-owned content";
    DirectoryInfo _temporary = null!;
    DirectoryInfo _target = null!;
    string _outside = null!;
    bool _symlinksSupported = true;
    Exception? _error;

    void Establish()
    {
        _temporary = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"stage-output-{Guid.NewGuid():N}"));
        _target = _temporary.CreateSubdirectory("out");
        _outside = Path.Combine(_temporary.FullName, "outside.cs");
        File.WriteAllText(_outside, ExistingContent);
        try
        {
            File.CreateSymbolicLink(Path.Combine(_target.FullName, "link.cs"), _outside);
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
            _error = await Catch.Exception(() => new LocalFileSystemOutput().Write(new RenderedFile("link.cs", "replacement"), _target, TextWriter.Null));
        }
    }

    void Destroy() => _temporary.Delete(recursive: true);

    [Fact] void should_refuse_the_path() => (!_symlinksSupported || _error is GeneratedPathOutsideOutputRoot).ShouldBeTrue();
    [Fact] void should_not_overwrite_the_outside_file() => File.ReadAllText(_outside).ShouldEqual(ExistingContent);
}
