// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.given;

/// <summary>
/// A generated-application build or test process a spec or scaffold starts.
/// </summary>
/// <param name="Caller">The spec or scaffold starting the process.</param>
/// <param name="FileName">The tool being started.</param>
/// <param name="Arguments">The tool's arguments.</param>
public record GeneratedApplicationProcess(string Caller, string FileName, string[] Arguments)
{
    const string ConfigurationProperty = "-p:Configuration=";

    /// <summary>
    /// Gets the tool and its verb, such as "dotnet build".
    /// </summary>
    public string Verb => $"{FileName} {Arguments.FirstOrDefault()}";

    /// <summary>
    /// Gets the configuration the arguments select, or "-" when they select none.
    /// </summary>
    public string Configuration
    {
        get
        {
            var configuration = "-";
            for (var index = 0; index < Arguments.Length; index++)
            {
                if (IsConfigurationOption(index)) configuration = Arguments[++index];
                else if (Arguments[index].StartsWith(ConfigurationProperty, StringComparison.OrdinalIgnoreCase)) configuration = Arguments[index][ConfigurationProperty.Length..];
            }

            return configuration;
        }
    }

    bool IsConfigurationOption(int index)
    {
        var hasValue = index + 1 < Arguments.Length;
        var isOption = string.Equals(Arguments[index], "-c", StringComparison.Ordinal) || string.Equals(Arguments[index], "--configuration", StringComparison.Ordinal);
        return isOption && hasValue;
    }
}
