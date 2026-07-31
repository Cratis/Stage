// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;

namespace Cratis.Stage.Runtime.for_ProducedEventPayloads.given;

public class a_command_payload : Specification
{
    protected static readonly DateTimeOffset _occurred = new(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);

    protected IReadOnlyDictionary<string, JsonElement> _command = null!;
    protected IReadOnlyDictionary<string, string> _identity = null!;

    void Establish()
    {
        _command = Payload(
            """
            {
                "invoiceId": "8a4d1f7e-3c2b-4a5d-9e6f-0b1c2d3e4f50",
                "currency": "USD",
                "isProForma": true,
                "quantity": 5,
                "invoiceNumber": "INV-000001"
            }
            """);

        _identity = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["id"] = "someone", ["name"] = "Some One" };
    }

    protected static IReadOnlyDictionary<string, JsonElement> Payload(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
}
