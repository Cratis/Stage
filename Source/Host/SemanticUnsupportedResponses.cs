// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Host;

public sealed class UnsupportedSemanticResponse() : Exception("The Arc response for an unsupported semantic operation is not JSON.");

internal static class SemanticUnsupportedResponses
{
    internal static async Task Rewrite(HttpContext context, Func<Task> next)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await next();
            return;
        }

        var original = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;
        try
        {
            await next();
            context.Response.Body = original;
            buffer.Position = 0;
            if (context.Response.StatusCode == StatusCodes.Status500InternalServerError &&
                context.Items[SemanticRuntimeMarkers.UnsupportedMessage] is string message)
            {
                var body = await JsonNode.ParseAsync(buffer) as JsonObject ?? throw new UnsupportedSemanticResponse();

                var messages = body.Select(property => property.Key).FirstOrDefault(key => key.Equals("exceptionMessages", StringComparison.OrdinalIgnoreCase)) ?? "exceptionMessages";
                body[messages] = JsonSerializer.SerializeToNode(new[] { message });
                var bytes = JsonSerializer.SerializeToUtf8Bytes(body);
                context.Response.StatusCode = StatusCodes.Status501NotImplemented;
                context.Response.ContentLength = bytes.Length;
                await original.WriteAsync(bytes, context.RequestAborted);
                return;
            }

            await buffer.CopyToAsync(original, context.RequestAborted);
        }
        finally
        {
            context.Response.Body = original;
        }
    }
}
