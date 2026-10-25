// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

public sealed partial class CratisBackendApplicationScaffold
{
    static string GlobalUsings() =>
        """
        global using Cratis.Chronicle.Keys;
        global using System.Collections.Immutable;
        global using System.Reactive.Subjects;
        """;
}
