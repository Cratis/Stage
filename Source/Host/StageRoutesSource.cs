// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Host;

/// <summary>
/// Builds <see cref="StageRoutes"/> for one loaded model, inferring list creation once rather than on every request.
/// </summary>
/// <param name="routes">The endpoints the running application registered.</param>
/// <param name="model">The loaded executable semantic model.</param>
/// <param name="creationFactory">Builds the model's list creation inference; defaults to <see cref="SemanticListCreation"/>.</param>
internal sealed class StageRoutesSource(
    StageSceneRoutes routes,
    ExecutableSemanticModel model,
    Func<ExecutableSemanticModel, SemanticListCreation>? creationFactory = null)
{
    readonly Lazy<SemanticListCreation> _creation = new(() => (creationFactory ?? (loaded => new SemanticListCreation(loaded)))(model));

    /// <summary>
    /// Builds the routes, reusing the list creation inferred for the model.
    /// </summary>
    /// <returns>The current routes.</returns>
    internal StageRoutes Build() => StageRoutes.For(routes, model, _creation.Value);
}
