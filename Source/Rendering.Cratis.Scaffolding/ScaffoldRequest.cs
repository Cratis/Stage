// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

/// <summary>
/// What one scaffold produces and where it reports progress.
/// </summary>
/// <param name="TargetDirectory">The directory the application is scaffolded into.</param>
/// <param name="ProjectName">The name of the scaffolded project.</param>
/// <param name="Output">The writer progress is reported to.</param>
sealed record ScaffoldRequest(DirectoryInfo TargetDirectory, string ProjectName, TextWriter Output);
