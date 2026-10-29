// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_BodyWorkerCacheDirectory;

public class when_preparing_a_cache : Specification
{
    string _directory = null!;

    void Establish() => _directory = Directory.CreateTempSubdirectory("stage-cache-spec-").FullName;
    void Destroy() => Directory.Delete(_directory, true);

    [Fact]
    void should_use_the_same_per_user_default_for_runner_arguments()
    {
        var expected = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Cratis", "Stage", "Bodies");
        BodyWorkerCacheDirectory.Default.ShouldEqual(expected);
        BodyWorkerOptions.FromArguments(["--body-worker-path", "worker.dll"])!.CacheDirectory.ShouldEqual(expected);
        Path.IsPathFullyQualified(expected).ShouldBeTrue();
    }

    [Fact]
    void should_normalize_a_configured_relative_directory_and_create_it_privately()
    {
        var path = Path.Combine(_directory, "units");
        var actual = BodyWorkerCacheDirectory.Prepare(Path.GetRelativePath(Environment.CurrentDirectory, path));
        actual.ShouldEqual(path);
        if (!OperatingSystem.IsWindows())
        {
            File.GetUnixFileMode(actual).ShouldEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    [Fact]
    void should_reject_an_invalid_configured_path() => Assert.Throws<ArgumentException>(() => BodyWorkerCacheDirectory.Prepare("\0"));

    [Theory]
    [InlineData(UnixFileMode.GroupWrite)]
    [InlineData(UnixFileMode.OtherWrite)]
    void should_refuse_a_cache_writable_by_another_user(UnixFileMode write)
    {
        if (OperatingSystem.IsWindows()) return;
        File.SetUnixFileMode(_directory, File.GetUnixFileMode(_directory) | write);
        Assert.Throws<UnauthorizedAccessException>(() => BodyWorkerCacheDirectory.Prepare(_directory));
    }

    [Fact]
    void should_refuse_a_replaceable_cache_under_an_untrusted_parent()
    {
        if (OperatingSystem.IsWindows()) return;
        var path = BodyWorkerCacheDirectory.Prepare(Path.Combine(_directory, "units"));
        File.SetUnixFileMode(_directory, File.GetUnixFileMode(_directory) | UnixFileMode.OtherWrite);
        Assert.Throws<UnauthorizedAccessException>(() => BodyWorkerCacheDirectory.Prepare(path));
    }

    [Fact]
    void should_refuse_a_foreign_owned_cache()
    {
        if (OperatingSystem.IsWindows() || Environment.UserName == "root") return;
        Assert.Throws<UnauthorizedAccessException>(() => BodyWorkerCacheDirectory.Prepare("/"));
    }

    [Fact]
    void should_refuse_a_cache_symbolic_link()
    {
        if (OperatingSystem.IsWindows()) return;
        var target = BodyWorkerCacheDirectory.Prepare(Path.Combine(_directory, "units"));
        var link = Path.Combine(_directory, "link");
        Directory.CreateSymbolicLink(link, target);
        Assert.Throws<UnauthorizedAccessException>(() => BodyWorkerCacheDirectory.Prepare(link));
    }
}
