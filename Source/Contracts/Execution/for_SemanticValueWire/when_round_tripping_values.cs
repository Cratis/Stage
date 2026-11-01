// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_SemanticValueWire;

public class when_round_tripping_values : Specification
{
    [Fact]
    void should_preserve_every_portable_kind_and_invariant_text()
    {
        SemanticValue[] values =
        [
            SemanticValue.Null, SemanticValue.Boolean(true), SemanticValue.Number(42), SemanticValue.Number(12.34m),
            SemanticValue.Text("hello"), SemanticValue.Text("2026-10-28"), SemanticValue.Text("12:34:56"),
            SemanticValue.Text("2026-10-28T12:34:56+02:00"), SemanticValue.Text("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
            SemanticValue.Text("Accepted"), SemanticValue.Array([SemanticValue.Number(1), SemanticValue.Null]),
            SemanticValue.Composite([new(SemanticId.Create(SemanticKind.Property, "property"), SemanticValue.Number(5))])
        ];
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nb-NO");
            foreach (var value in values)
            {
                var wire = SemanticValueWire.Write(value);
                SemanticValueWire.Write(SemanticValueWire.Read(wire)).GetRawText().ShouldEqual(wire.GetRawText());
            }
            SemanticValueWire.Write(SemanticValue.Number(12.34m)).GetProperty("value").GetString().ShouldEqual("12.34");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
