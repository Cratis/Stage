// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.SpecRunner.for_Program;

[Collection("SpecRunner application boundary culture")]
public class when_refusing_unimplemented_legacy_behavior : Specification
{
    readonly string _folder = Path.Combine(Path.GetTempPath(), $"stage legacy behavior runner {Guid.NewGuid():N}");

    void Establish() => Directory.CreateDirectory(_folder);

    [Theory]
    [InlineData("reaction batches atomic\n", "", "STAGE-ESM-043")]
    [InlineData("command reads protected\n", "", "STAGE-ESM-044")]
    [InlineData("", "        handler\n          implementation\n            hint \"Calculate the fee\"\n", "STAGE-IMPL-002")]
    public async Task should_exit_without_writing_results(string boundary, string handler, string code)
    {
        var file = Path.Combine(_folder, "Fees.play");
        var result = Path.Combine(_folder, "results.json");
        await File.WriteAllTextAsync(file, boundary + "module Billing\n  feature Invoices\n    slice StateChange ApplyFee\n      command ApplyFee\n" + handler + "        produces FeeApplied\n      event FeeApplied", TestContext.Current.CancellationToken);
        await using var error = new StringWriter();
        var exitCode = await Program.Run(["--model", file, "--output", result, "--engine", "structural"], TextWriter.Null, error);
        exitCode.ShouldEqual(1);
        error.ToString().ShouldContain(code + ":");
        File.Exists(result).ShouldBeFalse();
    }

    void Destroy() => Directory.Delete(_folder, true);
}
#endif
