// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_whole_numbers;

public class with_an_esm_v8_model : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = invoice_model.Plan(a_whole_number_model.Compile(SemanticVersion.V8));

    string Text(string suffix) => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith(suffix, StringComparison.Ordinal)).Bytes.AsSpan());

    [Fact] void should_admit_the_model() => _plan.Success.ShouldBeTrue();
    [Fact] void should_widen_the_concept() => Text("/Quantity.cs").ShouldContain("Quantity(long Value) : global::Cratis.Concepts.ConceptAs<long>");
    [Fact] void should_use_a_long_sentinel() => Text("/Quantity.cs").ShouldContain("NotSet = new(0L)");
    [Fact] void should_widen_the_command_property() => Text("/Record.cs").ShouldContain("record RecordStock(global::Invoices.Common.StockId Id, global::Invoices.Common.Quantity Quantity, long Count)");
    [Fact] void should_widen_the_event_property() => Text("/Record.cs").ShouldContain("record StockRecorded(global::Invoices.Common.Quantity Quantity, long Count)");
    [Fact] void should_widen_the_read_model_property() => Text("/Stocks.cs").ShouldContain("long Count");
    [Fact] void should_preserve_the_positive_literal() => Assert.True(Text("/when_recording_positive.cs").Contains("900719925474099L", StringComparison.Ordinal), Text("/when_recording_positive.cs"));
    [Fact] void should_preserve_the_negative_literal() => Assert.True(Text("/when_recording_negative.cs").Contains("-900719925474099L", StringComparison.Ordinal), Text("/when_recording_negative.cs"));
}
