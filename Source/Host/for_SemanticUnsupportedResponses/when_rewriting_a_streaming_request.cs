// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticUnsupportedResponses;

public class when_rewriting_a_streaming_request : Specification
{
    static readonly TimeSpan _deadline = TimeSpan.FromSeconds(10);
    readonly TaskCompletionSource _disconnect = new(TaskCreationOptions.RunContinuationsAsynchronously);
    SignallingStream _body = null!;
    Task _request = null!;
    bool _completedBeforeFrame;
    string _frame = null!;

    async Task Because()
    {
        _body = new();
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/projects/live";
        context.Request.Headers.Accept = "text/event-stream";
        context.Response.Body = _body;

        // Arc's SSE handler writes each frame and then holds the request open until the client disconnects.
        _request = SemanticUnsupportedResponses.Rewrite(context, async () =>
        {
            await context.Response.WriteAsync("data: []\n\n");
            await _disconnect.Task;
        });

        await _body.Written.WaitAsync(_deadline);
        _completedBeforeFrame = _request.IsCompleted;
        _frame = _body.Text;
        _disconnect.SetResult();
        await _request.WaitAsync(_deadline);
    }

    [Fact] void should_deliver_the_frame_to_the_client_while_the_request_is_open() => _frame.ShouldContain("data:");
    [Fact] void should_not_have_completed_the_request_when_the_frame_arrived() => _completedBeforeFrame.ShouldBeFalse();

    sealed class SignallingStream : MemoryStream
    {
        readonly TaskCompletionSource _written = new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal Task Written => _written.Task;

        internal string Text => Encoding.UTF8.GetString(ToArray());

        public override void Write(byte[] buffer, int offset, int count)
        {
            base.Write(buffer, offset, count);
            _written.TrySetResult();
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await base.WriteAsync(buffer, cancellationToken);
            _written.TrySetResult();
        }
    }
}
