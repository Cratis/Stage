// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;

// The columns the matrix compares; the table uses these names as its keys. All four use partial compilation,
// so slice-local plan issues refuse only the affected artifacts on each surface.
public enum conformance_surface
{
    Loader,
    Executor,
    Host,
    Renderer
}
#endif
