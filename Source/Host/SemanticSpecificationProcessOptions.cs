// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host;

internal sealed record SemanticSpecificationProcessOptions(string RunnerPath, TimeSpan Timeout)
{
    internal static SemanticSpecificationProcessOptions FromConfiguration(IConfiguration configuration) => new(
        configuration["Stage:Specifications:RunnerPath"] ?? Path.Combine(AppContext.BaseDirectory, "specrunner", "Cratis.Stage.SpecRunner.dll"),
        TimeSpan.FromSeconds(Math.Clamp(configuration.GetValue("Stage:Specifications:TimeoutSeconds", 120), 1, 600)));
}
