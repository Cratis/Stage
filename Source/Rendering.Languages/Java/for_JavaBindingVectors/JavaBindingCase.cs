// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Rendering.Declarative;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaBindingVectors;

public sealed record JavaBindingCase(
    string Name,
    ImmutableArray<DeclarativeLanguageFile> Files,
    string FactsJson,
    ImmutableArray<(string Path, string Text)> HostSources,
    ImmutableArray<string> ExcludedPaths);
