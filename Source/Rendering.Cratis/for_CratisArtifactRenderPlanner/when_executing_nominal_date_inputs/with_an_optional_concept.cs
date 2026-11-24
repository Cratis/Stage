// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_nominal_date_inputs;

public class with_an_optional_concept : Specification
{
    Assembly _assembly;

    void Establish() => _assembly = datetime_concept_model.Load(datetime_concept_model.OptionalSource);

    [Theory]
    [InlineData("", 0)]
    [InlineData(",\"at\":null", 0)]
    [InlineData(",\"at\":\"2026-10-17T02:00:00.1234567+02:00\"", 0)]
    [InlineData(",\"at\":\"2026-02-01T02:00:00+02:00\"", 0)]
    [InlineData(",\"at\":\"2026-01-31T23:59:59.9999999Z\"", 1)]
    void should_preserve_absence_and_compare_present_instants(string assignment, int failures)
    {
        var command = datetime_concept_model.Bind(_assembly, "{\"id\":\"one\"" + assignment + "}");
        datetime_concept_model.Command(_assembly).GetProperty("At").PropertyType.Name.ShouldEqual("Moment");
        var result = datetime_concept_model.Validate(_assembly, command);
        result.Errors.Count.ShouldEqual(failures);
        if (failures > 0) result.Errors.Single().ErrorMessage.ShouldEqual("Command minimum.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(",\"at\":null")]
    void should_leave_an_absent_concept_null(string assignment)
    {
        var command = datetime_concept_model.Bind(_assembly, "{\"id\":\"one\"" + assignment + "}");
        command.GetType().GetProperty("At").GetValue(command).ShouldBeNull();
    }

    [Fact]
    void should_refuse_an_offsetless_present_optional_value()
    {
        var refusal = Assert.Throws<ValueRequiresExplicitOffset>(() => datetime_concept_model.Bind(_assembly, "{\"id\":\"one\",\"at\":\"2026-10-17T00:00:00\"}"));
        refusal.Path.ShouldEqual("$.at");
        refusal.Message.ShouldEqual("The value requires an explicit offset or Z.");
    }
}
#endif
