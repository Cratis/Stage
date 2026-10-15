// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_FrameworkNameCatalog;

/// <summary>
/// The exception that is thrown when restoring, building or evaluating the catalog scaffold fails.
/// </summary>
/// <param name="command">The failing command.</param>
/// <param name="output">The command output.</param>
sealed class CatalogScaffoldFailed(string command, string output) : Exception($"'{command}' failed:{Environment.NewLine}{output}");
