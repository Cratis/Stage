// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.SpecRunner.for_Program;

[Collection("SpecRunner application boundary culture")]
public class when_refusing_reaction_identity_syntax : Specification
{
    readonly string _folder = Path.Combine(Path.GetTempPath(), $"stage reaction identity runner {Guid.NewGuid():N}");

    void Establish() => Directory.CreateDirectory(_folder);

    [Fact]
    public async Task should_exit_without_writing_results()
    {
        var file = Path.Combine(_folder, "ReactionIdentity.play");
        var result = Path.Combine(_folder, "results.json");
        await File.WriteAllTextAsync(file, "module Work\n  feature Completion\n    slice Automation Complete\n      event Started\n      reaction Follow\n        runs as system\n        when Started");
        await using var error = new StringWriter();
        var exitCode = await Program.Run(["--model", file, "--output", result], TextWriter.Null, error);
        exitCode.ShouldEqual(1);
        Assert.True(error.ToString().Contains("STAGE-ESM-034:", StringComparison.Ordinal), error.ToString());
        File.Exists(result).ShouldBeFalse();
    }

    void Destroy() => Directory.Delete(_folder, true);
}
#endif
