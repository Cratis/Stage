// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Packages;

/// <summary>
/// The log messages of extension package loading.
/// </summary>
public static partial class ExtensionPackageLog
{
    [LoggerMessage(LogLevel.Information, "Loaded the extension package {Reference} {Version} ({Digest}) from {Channel}; signed: {Signed}")]
    internal static partial void Loaded(ILogger logger, string reference, string version, string digest, string channel, bool signed);

    [LoggerMessage(LogLevel.Error, "The extension package {Reference} was refused ({Reason}): {Details}")]
    internal static partial void Refused(ILogger logger, string reference, PackageRefusalReason reason, string details);

    [LoggerMessage(LogLevel.Information, "Downloading the extension package {Reference} {Version} by its locked digest {Digest}")]
    internal static partial void Downloading(ILogger logger, string reference, string version, string digest);

    [LoggerMessage(LogLevel.Warning, "The cached copy of {Digest} no longer verifies and is downloaded again: {Details}")]
    internal static partial void CacheRejected(ILogger logger, string digest, string details);

    [LoggerMessage(LogLevel.Warning, "Extension packages are not required to be signed in this deployment; a package is trusted by its digest and the allow-list only")]
    internal static partial void SignaturesNotRequired(ILogger logger);
}
