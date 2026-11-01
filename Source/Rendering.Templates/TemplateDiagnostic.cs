// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Templates;

/// <summary>
/// A stable profile refusal and its one-based source location (zero for non-source refusals).
/// </summary>
public sealed record TemplateDiagnostic(string Code, string Message, string Path, int Line, int Column);
