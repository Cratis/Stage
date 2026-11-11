// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// A minimal extension backend: one safe ability that greets, with a public setting for the greeting.
// Stage connects to it at the address its configuration points to (Stage:Extensions:Backends:Hello:Endpoint).
using System.Net;
using System.Text.Json;
using Cratis.Stage.Extensions;

var extension = Extension.Create("hello", "1.0.0")
    .Setting("greeting")
    .Ability("Greet", EffectClass.Safe, async call =>
    {
        var greeting = await call.Host.Setting("greeting", call.CancellationToken) ?? "Hello";
        var name = JsonElement.Parse(call.Input).GetProperty("name").GetString();
        return AbilityOutcome.Returned(JsonSerializer.Serialize(new { message = $"{greeting}, {name}!" }));
    })
    .Build();

await ExtensionRunner.Run(extension, new IPEndPoint(IPAddress.Any, 50051));
