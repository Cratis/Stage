// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

public sealed partial class CratisBackendApplicationScaffold
{
    static string GlobalUsings() =>
        """
        // Copyright (c) Cratis. All rights reserved.
        // Licensed under the MIT license. See LICENSE file in the project root for full license information.

        global using Cratis.Arc.Chronicle.ReadModels;
        global using Cratis.Chronicle.Keys;
        global using System.Collections.Immutable;
        global using System.Reactive.Subjects;
        """;

    static string SpecificationsGlobalUsings() =>
        """
        // Copyright (c) Cratis. All rights reserved.
        // Licensed under the MIT license. See LICENSE file in the project root for full license information.

        global using Cratis.Specifications;
        global using NSubstitute;
        global using Xunit;
        """;
}
