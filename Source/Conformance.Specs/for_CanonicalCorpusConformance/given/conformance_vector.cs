// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;

// One canonical vector: the model its ESM bytes describe, and the corpus expectations when the package carries them.
public record conformance_vector(string Name, string Source, ExecutableSemanticModel Model, CanonicalCorpusVector? Corpus);
#endif
