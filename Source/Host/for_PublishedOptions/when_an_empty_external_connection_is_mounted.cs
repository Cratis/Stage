// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.Published.for_PublishedOptions;

public class when_an_empty_external_connection_is_mounted : a_published_configuration
{
    string _file = null!;
    PublishedOptions _options = null!;

    void Establish()
    {
        _file = Path.GetTempFileName();
        File.WriteAllText(_file, " \n");
        _environment[SandboxRuntimeConventions.ConnectionStringFile] = _file;
        _values.Remove("Stage:Published:Storage:ConnectionString");
    }

    void Because() => _options = Options();

    void Destroy() => File.Delete(_file);

    [Fact] void should_not_use_an_external_kernel() => _options.UsesExternalKernel.ShouldBeFalse();
    [Fact] void should_still_require_storage() => _options.Problems().Any(problem => problem.Contains("No persistent storage is configured")).ShouldBeTrue();
}
