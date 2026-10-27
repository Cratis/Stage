// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Rendering.Kits;
using Scriban;

namespace Cratis.Stage.Rendering.Templates;

internal sealed record PreparedTemplateKit(
    LoadedKit Kit,
    ImmutableDictionary<string, Template> Templates,
    ImmutableDictionary<string, Template> Expressions,
    ImmutableDictionary<string, OperatorTable> Operators,
    ImmutableDictionary<(string Template, string Section), Template> Overrides,
    ImmutableDictionary<string, ImmutableArray<string>> SectionNames);
