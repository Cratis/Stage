// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Headers;
using System.Text;
using Cratis.Specifications;
using Microsoft.AspNetCore.Http;

namespace Cratis.Stage.Host.Workbench.for_WorkbenchTransformer.given;

/// <summary>
/// A response coming back from the kernel's Workbench, run through a <see cref="WorkbenchTransformer"/> the way
/// the forwarder would.
/// </summary>
public class a_workbench_response : Specification
{
    protected const string RoutePrefix = "/workbench";
    protected const string BasePathMetaPattern = "name=\"base-path\" content=\"\"";

    /// <summary>
    /// The Workbench's index.html as the kernel serves it - root-absolute references and an unset base path.
    /// </summary>
    protected const string Index = """
        <!doctype html>
        <html>
            <head>
                <meta name="base-path" content="" />
                <link rel="icon" href="/favicon.svg" />
                <script type="module" src="/index-a1b2c3.js"></script>
            </head>
            <body><div id="root"></div></body>
        </html>
        """;

    protected async Task<(string Body, bool Copied)> Forward(string publicPrefix, string content, string mediaType)
    {
        var transformer = new WorkbenchTransformer(RoutePrefix, publicPrefix, BasePathMetaPattern);

        var context = new DefaultHttpContext();
        var body = new MemoryStream();
        context.Response.Body = body;

        using var response = new HttpResponseMessage
        {
            Content = new StringContent(content, Encoding.UTF8)
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType) { CharSet = "utf-8" };

        var copied = await transformer.TransformResponseAsync(context, response, CancellationToken.None);

        return (Encoding.UTF8.GetString(body.ToArray()), copied);
    }
}
