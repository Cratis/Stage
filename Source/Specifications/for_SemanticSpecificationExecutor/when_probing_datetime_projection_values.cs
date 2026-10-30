// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Specifications.Types;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_probing_datetime_projection_values : Specification
{
    [Theory]
    [InlineData("2026-01-01T00:00:00.0000000Z", "2026-01-01T00:00:00.0000000+00:00")]
    [InlineData("2026-01-01T01:00:00.1234567+01:00", "2026-01-01T01:00:00.1234567+01:00")]
    [InlineData("2026-01-01T00:00:00.1234567+00:00", "2026-01-01T00:00:00.1234567+00:00")]
    public void should_expose_why_datetimeoffset_cannot_preserve_every_authored_spelling(string authored, string expected)
    {
        var value = (DateTimeOffset)SemanticRuntimeTypes.ConvertValue(SemanticValue.Text(authored), typeof(DateTimeOffset))!;
        using var scenario = new ReadModelScenario<Current>(null, new Defaults(new SemanticClientArtifactsProvider([typeof(DateTimeProjectionRecorded)])))
            .WithProjection(builder =>
            {
                builder.NoAutoMap();
                builder.From<DateTimeProjectionRecorded>(from => from.Set(model => model.At).To(evt => evt.RecordedAt));
            });
        scenario.CollectEventsFor(new EventSourceId("one"), [new DateTimeProjectionRecorded(value)]);
        var projected = Assert.Single(scenario.Instances).Value.At.ToString("O", CultureInfo.InvariantCulture);
        projected.ShouldEqual(expected);
    }

    [EventType]
    public record DateTimeProjectionRecorded(DateTimeOffset RecordedAt);

    public class Current
    {
        public DateTimeOffset At { get; set; }
    }
}
