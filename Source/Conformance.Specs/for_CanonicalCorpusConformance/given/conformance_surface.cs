// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;

// The columns the matrix compares; the table uses these names as its keys. The loader column is the strict model load; the other
// three run on the partial plan, so a loader refusal does not hide what each surface does with the model.
public enum conformance_surface
{
    Loader,
    Executor,
    Host,
    Renderer
}
#endif
