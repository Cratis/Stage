// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Stage.Rendering.Templates;

internal sealed class PlanMeter(TemplateBudget limits)
{
    readonly Stopwatch _clock = Stopwatch.StartNew();
    long _steps;
    long _bytes;
    int _artifacts;
    int _fanOut;

    internal static TemplateRefusal Exceeded(string code, string message) => new(new(code, message, string.Empty, 0, 0));

    internal void Step()
    {
        CheckDeadline();
        if (++_steps > limits.MaxSteps) throw Exceeded(TemplateCodes.Steps, "Plan step budget exceeded.");
    }

    internal void CheckDeadline()
    {
        if (_clock.Elapsed >= limits.Deadline) throw Exceeded(TemplateCodes.Deadline, "Plan deadline exceeded.");
    }

    internal void Bytes(long count)
    {
        if (count > limits.MaxOutputBytes - _bytes) throw Exceeded(TemplateCodes.Output, "Plan UTF-8 output budget exceeded.");
        _bytes += count;
    }

    internal void Artifact()
    {
        CheckDeadline();
        if (++_artifacts > limits.MaxArtifacts) throw Exceeded(TemplateCodes.Artifacts, "Plan artifact budget exceeded.");
    }

    internal void FanOut(int count)
    {
        if (count > limits.MaxFanOut - _fanOut) throw Exceeded(TemplateCodes.FanOut, "Plan fan-out budget exceeded.");
        _fanOut += count;
    }
}
