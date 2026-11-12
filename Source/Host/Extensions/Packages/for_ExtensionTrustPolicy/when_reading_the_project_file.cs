// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_ExtensionTrustPolicy;

public class when_reading_the_project_file : Specification
{
    string _folder = null!;
    ProjectExtensionPolicy _policy = null!;

    void Because()
    {
        _folder = Path.Combine(Path.GetTempPath(), "stage-policy-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_folder, ".cratis"));
        Directory.CreateDirectory(Path.Combine(_folder, "Source", "model"));
        File.WriteAllText(Path.Combine(_folder, ".cratis", "extensions.json"), """
            { "allow": { "publishers": ["https://github.com/acme"] }, "publishers": { "https://github.com/acme": { "keys": ["abc"] } } }
            """);
        _policy = ProjectExtensionPolicy.Read(Path.Combine(_folder, "Source", "model"));
    }

    void Destroy() => Directory.Delete(_folder, recursive: true);

    [Fact] void should_read_the_allowed_publishers() => _policy.Allow.Publishers.ShouldContainOnly("https://github.com/acme");
    [Fact] void should_read_the_keys() => _policy.Publishers["https://github.com/acme"].Keys.ShouldContainOnly("abc");
    [Fact] void should_have_no_error() => _policy.Error.ShouldBeEmpty();
}
