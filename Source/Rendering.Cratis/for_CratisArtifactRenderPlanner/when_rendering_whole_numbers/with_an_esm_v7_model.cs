// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_whole_numbers;

public class with_an_esm_v7_model : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = invoice_model.Plan(a_whole_number_model.Compile(SemanticVersion.V7, a_whole_number_model.Source.Replace("900719925474099", "2147483647", StringComparison.Ordinal)));

    string Text(string suffix) => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith(suffix, StringComparison.Ordinal)).Bytes.AsSpan());

    [Fact] void should_admit_the_int_boundary() => _plan.Success.ShouldBeTrue();
    [Fact] void should_keep_the_concept_int32() => Text("/Quantity.cs").ShouldContain("Quantity(int Value) : global::Cratis.Concepts.ConceptAs<int>");
    [Fact] void should_keep_the_sentinel_unchanged() => Text("/Quantity.cs").ShouldContain("NotSet = new(0)");
    [Fact] void should_keep_the_command_property_int32() => Text("/Record.cs").ShouldContain("record RecordStock(global::Invoices.Common.StockId Id, global::Invoices.Common.Quantity Quantity, int Count)");
    [Fact] void should_keep_the_event_property_int32() => Text("/Record.cs").ShouldContain("record StockRecorded(global::Invoices.Common.Quantity Quantity, int Count)");
    [Fact] void should_keep_the_read_model_property_int32() => Text("/Stocks.cs").ShouldContain("int Count");
    [Fact] void should_keep_the_literal_unchanged() => Text("/when_recording_positive.cs").ShouldNotContain("2147483647L");
}
