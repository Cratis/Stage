// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts;

/// <summary>
/// Refuses protected command reads at the boundary of engines that execute commands without protecting decision reads.
/// </summary>
public static class ProtectedCommandReadsAdmission
{
    /// <summary>
    /// Ensures the engine may execute the model.
    /// </summary>
    /// <param name="model">The loaded event model.</param>
    /// <param name="path">The model path reported in the refusal.</param>
    /// <param name="engine">The name of the executing engine.</param>
    /// <exception cref="InvalidEventModel">Thrown when the model declares protected command reads.</exception>
    public static void EnsureExecutableBy(EventModel model, string path, string engine)
    {
        if (model.ProtectedCommandReads)
        {
            throw new InvalidEventModel(
                path,
                [$"STAGE-ESM-044: Application declares command reads protected; the {engine} engine does not evaluate or protect decision reads. Use the semantic engine."]);
        }
    }
}
