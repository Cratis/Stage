// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerOwner;

public class when_reading_incomplete_labels : Specification
{
    GeneratedApplicationContainerOwner _owner = null!;
    bool _missingRun;
    bool _missingFamily;
    bool _badProcess;
    bool _badStarted;
    bool _badProcessStarted;
    bool _emptyHost;

    void Establish() => _owner = new() { Host = "host-a", ProcessId = 4242, ProcessStartedUnixSeconds = 1_000_000, RunToken = "0123abcd", StartedUnixSeconds = 2_000_000 };

    void Because()
    {
        _missingRun = Reads(labels => labels.Remove(GeneratedApplicationContainerOwner.RunKey));
        _missingFamily = Reads(labels => labels.Remove(GeneratedApplicationContainerOwner.FamilyKey));
        _badProcess = Reads(labels => labels[GeneratedApplicationContainerOwner.ProcessKey] = "not-a-number");
        _badStarted = Reads(labels => labels[GeneratedApplicationContainerOwner.StartedKey] = "yesterday");
        _badProcessStarted = Reads(labels => labels[GeneratedApplicationContainerOwner.ProcessStartedKey] = "-5");
        _emptyHost = Reads(labels => labels[GeneratedApplicationContainerOwner.HostKey] = " ");
    }

    [Fact] void should_reject_a_missing_run_token() => _missingRun.ShouldBeFalse();
    [Fact] void should_reject_a_missing_family_label() => _missingFamily.ShouldBeFalse();
    [Fact] void should_reject_a_non_integer_process_id() => _badProcess.ShouldBeFalse();
    [Fact] void should_reject_a_non_integer_start_time() => _badStarted.ShouldBeFalse();
    [Fact] void should_reject_a_malformed_process_start_time() => _badProcessStarted.ShouldBeFalse();
    [Fact] void should_reject_a_blank_host() => _emptyHost.ShouldBeFalse();

    bool Reads(Action<Dictionary<string, string>> change)
    {
        var labels = when_reading_its_own_labels.Labels(_owner);
        change(labels);
        return GeneratedApplicationContainerOwner.TryRead(labels, out _);
    }
}
