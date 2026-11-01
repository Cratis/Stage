// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Export;

/// <summary>
/// The exception that is thrown when a model cannot be exported as a publishable package.
/// </summary>
public sealed class StageExportRefused : Exception
{
    internal StageExportRefused(IReadOnlyList<string> reasons)
        : base($"The model cannot be exported:{Environment.NewLine}{string.Join(Environment.NewLine, reasons.Select(reason => $"  - {reason}"))}")
    {
        Reasons = reasons;
    }

    /// <summary>
    /// Gets everything that stops the export.
    /// </summary>
    public IReadOnlyList<string> Reasons { get; }
}
