// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Host;

/// <summary>
/// A person a development session can act as, declared with <c language="shell">user</c> in the model.
/// </summary>
/// <param name="Name">The name the model gives the user, which is the identity the session presents.</param>
/// <param name="Personas">The names of the personas the user plays.</param>
/// <param name="Principal">The principal Lens sends for the user, carrying the roles and claims of its personas.</param>
internal sealed record StageUser(string Name, IReadOnlyList<string> Personas, ClientPrincipal Principal);

/// <summary>
/// The users the model declares, built once at startup and offered to development tooling such as Lens.
/// </summary>
/// <remarks>
/// Empty unless <see cref="SandboxUsersOptions.Enabled"/>: a host that did not ask for users offers none, whatever its model declares.
/// </remarks>
/// <param name="all">The users, in the order the model declares them.</param>
internal sealed class SandboxUsers(IReadOnlyList<StageUser> all)
{
    internal const string IdentityProvider = "stage-user";

    /// <summary>
    /// Gets no users.
    /// </summary>
    internal static SandboxUsers None { get; } = new([]);

    /// <summary>
    /// Gets the users, in the order the model declares them.
    /// </summary>
    internal IReadOnlyList<StageUser> All => all;

    /// <summary>
    /// Reads the users from the model's source.
    /// </summary>
    /// <param name="modelPath">The model file or folder.</param>
    /// <param name="logger">The logger for what the model says that cannot be honored.</param>
    /// <returns>The users the model declares, or none when it does not compile.</returns>
    internal static SandboxUsers Load(string modelPath, ILogger logger)
    {
        var compiler = new PlayFileCompiler();
        var result = Directory.Exists(modelPath) ? compiler.CompileFolder(modelPath).Result : compiler.CompileApplication(modelPath).Result;
        var users = result.Success ? From(result.Value!, logger) : None;
        if (users.All.Count > 0)
        {
            StageLog.SandboxUsersOffered(logger, string.Join(", ", users.All.Select(user => $"{user.Name} [{string.Join(", ", user.Principal.UserRoles)}]")));
        }

        return users;
    }

    /// <summary>
    /// Builds the users an application declares.
    /// </summary>
    /// <param name="application">The compiled application.</param>
    /// <param name="logger">The logger for what the model says that cannot be honored.</param>
    /// <returns>The users.</returns>
    internal static SandboxUsers From(ApplicationSyntax application, ILogger logger)
    {
        var personas = (application.Personas ?? []).ToDictionary(persona => persona.Name, StringComparer.Ordinal);
        return new([.. application.Users.Select(user => Create(user, personas, application, logger))]);
    }

    static StageUser Create(UserSyntax user, Dictionary<string, PersonaSyntax> personas, ApplicationSyntax application, ILogger logger)
    {
        var roles = new List<string>();
        var claims = new List<ClientPrincipalClaim>();
        foreach (var name in user.Personas.Where(personas.ContainsKey))
        {
            // Skips the policies a persona cannot be synthesized for and says so, as the single-persona sandbox does.
            var caller = SandboxPersona.CallerFor(personas[name], application, logger);
            roles.AddRange((caller?.Roles ?? []).Where(role => !roles.Contains(role)));
            claims.AddRange((caller?.Claims ?? [])
                .Where(claim => !claims.Exists(existing => existing.typ == claim.Type && existing.val == claim.Value))
                .Select(claim => new ClientPrincipalClaim { typ = claim.Type, val = claim.Value }));
        }

        return new(user.Name, [.. user.Personas], new()
        {
            IdentityProvider = IdentityProvider,
            UserId = user.Name,
            UserDetails = user.DisplayName ?? user.Name,
            UserRoles = roles,
            Claims = claims
        });
    }
}

/// <summary>
/// Offers the sandbox's modeled users to development tooling, through Arc's <c language="shell">/.cratis/users</c>.
/// </summary>
/// <param name="users">The users.</param>
internal sealed class SandboxUsersProvider(SandboxUsers users) : ICanProvideUsers
{
    /// <inheritdoc/>
    public Task<IEnumerable<User>> Provide() => Task.FromResult(users.All.Select(user => new User(user.Principal, new { user.Personas })));
}

/// <summary>
/// Whether the sandbox offers its modeled users, which Lens then sends as the caller.
/// </summary>
/// <param name="Enabled">Whether the users are on.</param>
internal sealed record SandboxUsersOptions(bool Enabled)
{
    internal const string Key = "Stage:Sandbox:Users";

    /// <summary>
    /// Reads the switch. It is explicit: it is off unless set, and it is refused rather than ignored where it cannot apply.
    /// </summary>
    /// <param name="configuration">The configuration the host runs with.</param>
    /// <param name="development">Whether Stage is in development, as <see cref="StageDevelopment"/> decided.</param>
    /// <returns>The options.</returns>
    /// <exception cref="InvalidOperationException">Users were asked for where Stage is not in development.</exception>
    internal static SandboxUsersOptions Read(IConfiguration configuration, bool development)
    {
        var requested = configuration.GetValue<bool>(Key);
        return requested && !development
            ? throw new InvalidOperationException($"{Key} is on, but Stage is not in development ({StageDevelopment.Key} is off, or this is a published application). Modeled users are for development and test only.")
            : new(requested);
    }
}
