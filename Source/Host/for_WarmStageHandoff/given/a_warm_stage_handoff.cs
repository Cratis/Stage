// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;

namespace Cratis.Stage.Host.for_WarmStageHandoff.given;

public class a_warm_stage_handoff : Specification
{
    protected string _directory = null!;
    protected WarmStageHandoff _handoff = null!;
    protected Guid _handoffId;
    protected int _resetCount;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _handoff = new WarmStageHandoff(_directory);
        _handoffId = Guid.NewGuid();
    }

    void Destroy()
    {
        _handoff.Dispose();
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }

    protected Task Reset(CancellationToken cancellationToken)
    {
        _resetCount++;
        return Task.CompletedTask;
    }
}
