// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Semantics;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticUnsupportedResponses;

public class when_rewriting_a_redacted_arc_result : Specification
{
    DefaultHttpContext _context = null!;
    string _body = string.Empty;

    void Establish()
    {
        _context = new();
        _context.Request.Path = "/api/projects/registration/register-project";
        _context.Response.Body = new MemoryStream();
        _context.Items[SemanticRuntimeMarkers.UnsupportedMessage] = "Unsupported(IdentityAllocation) command-1: Allocate a destination.";
    }

    async Task Because()
    {
        await SemanticUnsupportedResponses.Rewrite(_context, async () =>
        {
            _context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await _context.Response.WriteAsync("""{"exceptionMessages":["An internal error occurred."]}""");
        });
        _context.Response.Body.Position = 0;
        _body = await new StreamReader(_context.Response.Body, Encoding.UTF8).ReadToEndAsync();
    }

    [Fact] void should_return_501() => _context.Response.StatusCode.ShouldEqual(StatusCodes.Status501NotImplemented);
    [Fact] void should_preserve_the_typed_unsupported_message() => _body.Contains("Unsupported(IdentityAllocation) command-1", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_leak_the_redacted_error() => _body.Contains("An internal error occurred.", StringComparison.Ordinal).ShouldBeFalse();
}
