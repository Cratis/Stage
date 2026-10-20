// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Stage.Host.for_SandboxIdentity.when_stripping_before_authentication.given;

public class AuthenticationRecording
{
    public ConcurrentQueue<string[]> Invocations { get; } = new();
}
