// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Specs;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxRuntimeConventions;

public class when_reading_the_mounted_connection_string : Specification
{
    string _folder = null!;
    string? _mounted;
    string? _unset;
    string? _missing;

    void Because()
    {
        _folder = Directory.CreateDirectory(SpecTemporaryRoot.NewPath("stage-runtime-conventions")).FullName;
        var file = Path.Combine(_folder, "chronicle");
        File.WriteAllText(file, "chronicle://client:secret@kernel:35000\n");
        _mounted = SandboxRuntimeConventions.MountedConnectionString(name => name == SandboxRuntimeConventions.ConnectionStringFile ? file : null)?.ToString();
        _unset = SandboxRuntimeConventions.MountedConnectionString(_ => null)?.ToString();
        _missing = SandboxRuntimeConventions.MountedConnectionString(_ => Path.Combine(_folder, "absent"))?.ToString();
    }

    void Destroy() => SpecTemporaryRoot.Delete(_folder);

    [Fact] void should_use_the_connection_string_in_the_file_as_supplied() => _mounted.ShouldEqual("chronicle://client:secret@kernel:35000");
    [Fact] void should_use_nothing_when_the_runtime_mounted_none() => _unset.ShouldBeNull();
    [Fact] void should_use_nothing_when_the_file_is_not_there() => _missing.ShouldBeNull();
}
