// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Cratis.Stage.Host.for_WarmStageHandoff.given;

[Collection(nameof(runtime_engine_environment))]
public class a_runtime_engine_environment : a_warm_stage_handoff
{
    string? _previousEngine;

    void Establish()
    {
        _previousEngine = Environment.GetEnvironmentVariable("Stage__Runtime__Engine");
        Environment.SetEnvironmentVariable("Stage__Runtime__Engine", null);
    }

    void Destroy() => Environment.SetEnvironmentVariable("Stage__Runtime__Engine", _previousEngine);
}
