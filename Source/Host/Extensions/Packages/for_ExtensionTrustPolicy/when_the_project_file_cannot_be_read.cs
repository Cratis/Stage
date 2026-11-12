// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_ExtensionTrustPolicy;

public class when_the_project_file_cannot_be_read : Specification
{
    string _folder = null!;
    ProjectExtensionPolicy _policy = null!;
    PackageTrust _trust = null!;

    void Because()
    {
        _folder = Path.Combine(Path.GetTempPath(), "stage-policy-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_folder, ".cratis", "x"));
        Directory.CreateDirectory(Path.Combine(_folder, "model"));
        File.WriteAllText(Path.Combine(_folder, ".cratis", "extensions.json"), "{ not json");
        _policy = ProjectExtensionPolicy.Read(Path.Combine(_folder, "model"));
        var options = new ExtensionPackageOptions();
        options.Allow.Publishers = [PackageFixtures.Publisher];
        _trust = new ExtensionTrustPolicy(options, _policy).Evaluate(PackageFixtures.Entry([1], [2]));
    }

    void Destroy() => Directory.Delete(_folder, recursive: true);

    [Fact] void should_find_the_file_above_the_model() => _policy.Source.EndsWith("extensions.json", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_say_why_it_could_not_be_read() => _policy.Error.ShouldContain("could not be read");
    [Fact] void should_allow_nothing_even_when_the_deployment_does() => _trust.Allowed.ShouldBeFalse();
}
