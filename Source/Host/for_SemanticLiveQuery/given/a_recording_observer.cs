// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;

namespace Cratis.Stage.Host.for_SemanticLiveQuery.given;

internal sealed class a_recording_observer : IObserver<string>
{
    static readonly TimeSpan _deadline = TimeSpan.FromSeconds(10);
    readonly Channel<string> _values = Channel.CreateUnbounded<string>();
    readonly TaskCompletionSource<Exception?> _ended = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal Task<string> Next() => _values.Reader.ReadAsync().AsTask().WaitAsync(_deadline);

    internal Task<Exception?> Ended() => _ended.Task.WaitAsync(_deadline);

    public void OnNext(string value) => _values.Writer.TryWrite(value);

    public void OnError(Exception error) => _ended.TrySetResult(error);

    public void OnCompleted() => _ended.TrySetResult(null);
}
