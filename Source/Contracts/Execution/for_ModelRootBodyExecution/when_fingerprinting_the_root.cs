// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_ModelRootBodyExecution;

public class when_fingerprinting_the_root : given.a_model_root
{
    [Theory]
    [InlineData("Model.play")]
    [InlineData("More.play")]
    [InlineData("screenplay.json")]
    [InlineData(".screenplay/identities.json")]
    async Task should_detect_creation_updates_and_removal(string relative)
    {
        var path = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var original = ModelRootBodyExecution.Fingerprint(root);
        await File.WriteAllTextAsync(path, "first");
        var added = ModelRootBodyExecution.Fingerprint(root);
        added.ShouldNotEqual(original);
        await File.WriteAllTextAsync(path, "second revision");
        ModelRootBodyExecution.Fingerprint(root).ShouldNotEqual(added);
        File.Delete(path);
        ModelRootBodyExecution.Fingerprint(root).ShouldEqual(original);
    }

    [Theory]
    [InlineData(".git/config")]
    [InlineData(".ai-work/notes")]
    [InlineData("bin/generated")]
    [InlineData("obj/generated")]
    [InlineData("node_modules/package")]
    [InlineData(".screenplay/notes.json")]
    [InlineData(".screenplay/release-baseline.json")]
    async Task should_ignore_non_compilation_files(string relative)
    {
        var original = ModelRootBodyExecution.Fingerprint(root);
        var path = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        await File.WriteAllTextAsync(path, "ignored");
        ModelRootBodyExecution.Fingerprint(root).ShouldEqual(original);
    }
}
