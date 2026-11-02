// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Stage.Host;

/// <summary>
/// The conventions a Cratis runtime (<c language="csharp">cratis run</c> or Stage Manager) starts a container with.
/// </summary>
/// <remarks>
/// <c language="csharp">CRATIS_ENVIRONMENT</c>, <c language="csharp">CRATIS_SECRETS_DIRECTORY</c> and <c language="csharp">CRATIS_MAIL_PICKUP_FOLDER</c> are for the services that read the values of an
/// environment or send mail - a rendered application. The sandbox runs the semantic engine, which sends no mail and needs no
/// environment value, so it reads only the Chronicle connection string.
/// </remarks>
internal static class SandboxRuntimeConventions
{
    internal const string ConnectionStringFile = "CRATIS_CHRONICLE_CONNECTION_STRING_FILE";

    /// <summary>
    /// Gets the Chronicle connection string a runtime mounted as a file, which carries a secret and so is not put in the environment.
    /// </summary>
    /// <param name="environment">Reads an environment variable.</param>
    /// <returns>The connection string as supplied, or null when the runtime mounted none.</returns>
    internal static ChronicleConnectionString? MountedConnectionString(Func<string, string?> environment) =>
        environment(ConnectionStringFile) is { Length: > 0 } file && File.Exists(file) && File.ReadAllText(file).Trim() is { Length: > 0 } text
            ? new ChronicleConnectionStringBuilder(text).ToConnectionString()
            : null;
}
