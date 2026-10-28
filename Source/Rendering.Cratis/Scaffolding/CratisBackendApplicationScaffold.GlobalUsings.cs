// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

public sealed partial class CratisBackendApplicationScaffold
{
    // Fixed per scaffold version and never derived from the model. The resolver's framework name catalog records the same
    // namespaces with source compile:GlobalUsings.cs; a spec keeps the two in step. Ordinal order, like every generated using scope.
    static string GlobalUsings() =>
        """
        global using Cratis.Arc.MongoDB;
        global using Cratis.Chronicle.Keys;
        global using MongoDB.Driver;
        global using System.Reactive.Subjects;
        """;
}
