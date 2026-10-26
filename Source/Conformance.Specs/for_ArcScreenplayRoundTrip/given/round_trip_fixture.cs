// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;

// One fixture of the Arc round trip: the model the renderer is given, and the plan it rendered for it.
internal sealed record round_trip_fixture(string Name, ExecutableSemanticModel Model, ArtifactRenderPlan Plan);
#endif
