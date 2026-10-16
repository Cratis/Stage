// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticGeneratedValues;

public class when_generating_patterns_and_random_values : Specification
{
    [Theory]
    [InlineData("pattern \"REF-?{16}\"")]
    [InlineData("pattern \"{yyyy}-{MM}-{dd}-?{16}\"")]
    [InlineData("pattern \"#{16}\" checksum luhn")]
    [InlineData("pattern \"#{16}\" checksum mod11")]
    [InlineData("pattern \"#{16}\" checksum mod97")]
    public void should_sample_the_strategy_at_the_captured_utc_date(string strategy)
    {
        var plan = compiled_plan.From($"concept Reference : String\n  generate {strategy}\nmodule Work\n  feature Issue\n    slice StateChange Start\n      command Create\n        value Reference generated\n        returns value");
        var concept = plan.Model.Application.Concepts.Single();
        var instant = new DateTimeOffset(2024, 3, 1, 0, 30, 0, TimeSpan.FromHours(2));
        var samples = Enumerable.Range(0, 256).Select(_ => SemanticGeneratedValues.New(concept, true, new(instant, GenerationContext.DefaultTenant))).ToArray();
        samples.All(value => SemanticGenerationShapes.Matches(concept, SemanticValue.Text(value), instant)).ShouldBeTrue();
        samples.Distinct(StringComparer.Ordinal).Count().ShouldBeGreaterThan(1);
        if (concept.Generation!.Checksum is { } algorithm)
        {
            var checksum = algorithm switch { "luhn" => Checksum.Luhn, "mod11" => Checksum.Mod11, _ => Checksum.Mod97 };
            samples.All(value => GenerateValue.HasValidChecksum(value, checksum)).ShouldBeTrue();
        }
    }

    [Theory]
    [InlineData("-9223372036854775808..9223372036854775807")]
    [InlineData("-2147483649..2147483648")]
    [InlineData("-2..2")]
    [InlineData("7..7")]
    public void should_generate_numeric_fixtures_without_string_conversion_or_int32_narrowing(string range)
    {
        var plan = compiled_plan.From($"concept Ticket : Int\n  generate random {range}\nmodule Work\n  feature Issue\n    slice StateChange Start\n      command Create\n        value Ticket generated\n        returns value");
        var command = plan.Commands.Values.Single();
        var concept = plan.Model.Application.Concepts.Single();
        var samples = Enumerable.Range(0, 256).Select(_ => SemanticGeneratedValues.For(command, plan.Model, new(2026, 1, 2, 0, 0, 0, TimeSpan.Zero)).Single().Value).ToArray();
        samples.All(value => SemanticGenerationShapes.Matches(concept, value)).ShouldBeTrue();
        samples.All(value => value is SemanticNumberValue).ShouldBeTrue();
    }
}
