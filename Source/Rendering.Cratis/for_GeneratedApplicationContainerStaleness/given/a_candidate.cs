// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.given;
using Cratis.Stage.Rendering.Cratis.given;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerStaleness.given;

/// <summary>
/// The current run and a scripted owner process probe.
/// </summary>
public class a_candidate : Specification
{
    protected static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    protected FakeOwnerProcesses _processes = null!;
    protected GeneratedApplicationContainerOwner _current = null!;

    void Establish()
    {
        _processes = new();
        _current = new() { Host = "host-a", ProcessId = 4242, ProcessStartedUnixSeconds = 1_000, RunToken = "current0", StartedUnixSeconds = Now.ToUnixTimeSeconds() };
    }

    protected static GeneratedApplicationContainerOwner Started(string host, TimeSpan age, string token = "earlier0") => new() { Host = host, ProcessId = 99, ProcessStartedUnixSeconds = 1_000, RunToken = token, StartedUnixSeconds = (Now - age).ToUnixTimeSeconds() };

    protected StalenessDecision Decide(GeneratedApplicationContainerOwner? owner) => GeneratedApplicationContainerStaleness.Decide(owner, _current, Now, _processes);
}
