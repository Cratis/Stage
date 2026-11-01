// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.Execution;

namespace Cratis.Stage.BodyWorker;

/// <summary>Isolated validation-body process; stdout is reserved for the versioned pipe protocol.</summary>
public static class Program
{
    /// <summary>Runs until stdin closes, shutdown is requested, or the supervisor kills the process.</summary>
    /// <param name="args">The compile or execute role arguments.</param>
    /// <returns>The worker exit code.</returns>
    public static Task<int> Main(string[] args) => BodyWorkerLoop.Run(args, Console.In, Console.Out, Console.Error);
}
