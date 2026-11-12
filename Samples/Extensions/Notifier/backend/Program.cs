// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// The backend of the Notifier extension: the Notify ability the contract declares, which posts a notice and returns its identity.
// Stage starts it when a screen that places the NoticeBoard component is reached, or when the first invocation needs it.
using System.Net;
using System.Text.Json;
using Cratis.Stage.Extensions;

var extension = Extension.Create("notifier", "1.0.0")
    .Setting("channel")
    .Ability("Notify", EffectClass.Once, Notify, ["Muted"])
    .Build();

await ExtensionRunner.Run(extension, new IPEndPoint(IPAddress.Any, 50051));

static async Task<AbilityOutcome> Notify(AbilityContext call)
{
    var channel = await call.Host.Setting("channel", call.CancellationToken) ?? "general";
    if (channel == "muted")
    {
        return AbilityOutcome.Returned(outcome: "Muted");
    }

    var message = JsonElement.Parse(call.Input).GetProperty("message").GetString();
    return AbilityOutcome.Returned(JsonSerializer.Serialize(new { noticeId = $"{channel}:{message}" }));
}
