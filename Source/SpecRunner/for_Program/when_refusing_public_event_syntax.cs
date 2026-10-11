// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.SpecRunner.for_Program;

[Collection("SpecRunner application boundary culture")]
public class when_refusing_public_event_syntax : Specification
{
    readonly string _folder = Path.Combine(Path.GetTempPath(), $"stage public events runner {Guid.NewGuid():N}");

    void Establish() => Directory.CreateDirectory(_folder);

    // Source-events captures require both a direction and an origin to compile; their isolated refusal is
    // covered by for_PublicEventSyntaxAdmission rather than a compound file-boundary case here.
    [Theory]
    [InlineData("public", "STAGE-ESM-032")]
    [InlineData("foreign", "STAGE-ESM-033")]
    [InlineData("direction", "STAGE-ESM-024")]
    public async Task should_exit_without_writing_results(string form, string code)
    {
        var file = Path.Combine(_folder, "PublicEvents.play");
        var result = Path.Combine(_folder, "results.json");
        var slice = form switch
        {
            "public" => "    slice StateView Publication\n      public event Published",
            "foreign" => "    slice StateView Publication\n      event Published from \"producer\"",
            _ => "    slice Translate Publication\n      direction inbound\n      event Received"
        };
        await File.WriteAllTextAsync(file, "module Integration\n  feature Exchange\n" + slice);
        await using var error = new StringWriter();
        var exitCode = await Program.Run(["--model", file, "--output", result], TextWriter.Null, error);
        exitCode.ShouldEqual(1);
        Assert.True(error.ToString().Contains(code + ":", StringComparison.Ordinal), error.ToString());
        File.Exists(result).ShouldBeFalse();
    }

    void Destroy() => Directory.Delete(_folder, true);
}
#endif
