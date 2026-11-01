// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Environments;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Stage.Contracts.Semantics;

/// <summary>
/// Represents an identity provider the application signs people in with.
/// </summary>
/// <param name="Kind">The provider kind - <c>EntraId</c>, <c>GitHub</c>, <c>Google</c>, <c>Apple</c> or <c>OpenId</c>.</param>
/// <param name="Identity">What the provider is known by: its alias when it has one, otherwise its kind.</param>
public sealed record RealizedProvider(string Kind, string Identity);

/// <summary>
/// Represents one claim a persona requires of a caller.
/// </summary>
/// <param name="Type">The claim type.</param>
/// <param name="Value">The claim value.</param>
public sealed record RealizedClaim(string Type, string Value);

/// <summary>
/// Represents a persona, the policies it holds and the smallest set of roles and claims that satisfies all of them.
/// </summary>
/// <param name="Name">The name of the persona.</param>
/// <param name="Policies">The names of the policies the persona holds.</param>
/// <param name="Roles">The roles a caller holds when it is this persona.</param>
/// <param name="Claims">The claims a caller holds when it is this persona.</param>
/// <param name="Mappable">Whether the policies could be reduced to roles and claims. A persona that cannot is never matched.</param>
public sealed record RealizedPersona(string Name, ImmutableArray<string> Policies, ImmutableArray<string> Roles, ImmutableArray<RealizedClaim> Claims, bool Mappable);

/// <summary>
/// Represents one kind of invitation.
/// </summary>
/// <param name="Kind">Either <c>tenant</c> or <c>member</c>.</param>
/// <param name="By">The personas allowed to send it; empty for a tenant invitation, which the operator sends.</param>
/// <param name="Grant">The personas the invitee is given.</param>
public sealed record RealizedInvitation(string Kind, ImmutableArray<string> By, ImmutableArray<string> Grant);

/// <summary>
/// Represents the email template file of one kind of onboarding email.
/// </summary>
/// <param name="Kind">The kind - <c>invitation</c>, <c>welcome</c> or <c>signupDeclined</c>.</param>
/// <param name="Path">The model-relative path of the template file.</param>
public sealed record RealizedEmail(string Kind, string Path);

/// <summary>
/// Represents the onboarding the application offers.
/// </summary>
/// <param name="Signup">What a self-signup does - <c>CreatesTenant</c> or <c>JoinsTenant</c> - or empty when sign-up is off.</param>
/// <param name="Invitations">The invitation kinds the application supports.</param>
/// <param name="LegalDocuments">The model-relative paths of the legal documents a newcomer must accept.</param>
/// <param name="Offering">The offering a new tenant starts on, or empty.</param>
/// <param name="Emails">The email templates.</param>
/// <param name="ImagesFolder">The model-relative folder holding the images the templates use, or empty.</param>
public sealed record RealizedOnboarding(
    string Signup,
    ImmutableArray<RealizedInvitation> Invitations,
    ImmutableArray<string> LegalDocuments,
    string Offering,
    ImmutableArray<RealizedEmail> Emails,
    string ImagesFolder);

/// <summary>
/// Represents an environment value the application needs, by key.
/// </summary>
/// <param name="Key">The key.</param>
/// <param name="Secret">Whether the value is a secret.</param>
public sealed record RealizedEnvironmentKey(string Key, bool Secret);

/// <summary>
/// The parts of a Screenplay application that are realization metadata: they never enter the executable semantic model,
/// yet a rendered application needs them - the identity providers it signs people in with, the personas it holds, the
/// onboarding it offers and the files that onboarding refers to.
/// </summary>
/// <param name="Providers">The identity providers.</param>
/// <param name="Personas">The personas.</param>
/// <param name="Onboarding">The onboarding block, when the model has one.</param>
/// <param name="Environment">The environment values the model needs.</param>
public sealed record ApplicationRealization(
    ImmutableArray<RealizedProvider> Providers,
    ImmutableArray<RealizedPersona> Personas,
    RealizedOnboarding? Onboarding,
    ImmutableArray<RealizedEnvironmentKey> Environment)
{
    static readonly HashSet<string> _imageExtensions = new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".gif" };

    /// <summary>
    /// Gets a realization of an application with no authentication and no onboarding.
    /// </summary>
    public static ApplicationRealization None { get; } = new([], [], null, []);

    /// <summary>
    /// Gets the model-relative files (legal documents and email templates) the realization refers to, as UTF-8 text.
    /// </summary>
    public ImmutableSortedDictionary<string, string> Texts { get; init; } = ImmutableSortedDictionary.Create<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Gets the images of the onboarding emails, keyed by file name.
    /// </summary>
    public ImmutableSortedDictionary<string, ImmutableArray<byte>> Images { get; init; } = ImmutableSortedDictionary.Create<string, ImmutableArray<byte>>(StringComparer.Ordinal);

    /// <summary>
    /// Gets whether the model asks for nothing a rendered application has to realize.
    /// </summary>
    public bool IsEmpty => Providers.IsDefaultOrEmpty && Onboarding is null;

    /// <summary>
    /// Derives the realization of a semantic compilation.
    /// </summary>
    /// <remarks>
    /// Providers, personas and onboarding are not part of the executable model, so they are read from the syntax of the very
    /// documents the model was compiled from.
    /// </remarks>
    /// <param name="compilation">The <see cref="SemanticCompilation"/>.</param>
    /// <param name="modelRoot">The physical root of the model, which the images of the onboarding emails are read from; omitted, there are none.</param>
    /// <returns>The <see cref="ApplicationRealization"/>.</returns>
    public static ApplicationRealization From(SemanticCompilation compilation, string? modelRoot = null)
    {
        if (compilation.Environment.Keys.IsEmpty)
        {
            return None;
        }

        var parsed = new PlayFileCompiler().CompileFolder(compilation.Documents.Documents.Select(document => new PlayFileContent(document.DisplayPath, document.Text)));

        return parsed.Result.Success
            ? From(parsed.Result.Value!, compilation.Environment, compilation.Documents.AttachmentContents, modelRoot)
            : None;
    }

    /// <summary>
    /// Derives the realization of a compiled application.
    /// </summary>
    /// <param name="application">The compiled <see cref="ApplicationSyntax"/>.</param>
    /// <param name="environment">The environment values the model needs.</param>
    /// <param name="texts">The attachment files by model-relative path.</param>
    /// <param name="imagesRoot">The physical model root images are read from, when there is one.</param>
    /// <returns>The <see cref="ApplicationRealization"/>.</returns>
    public static ApplicationRealization From(
        ApplicationSyntax application,
        EnvironmentRequirements environment,
        IReadOnlyDictionary<string, string> texts,
        string? imagesRoot)
    {
        var providers = (application.Authentication?.Providers ?? [])
            .Select(provider => new RealizedProvider(provider.Name, provider.Identity))
            .ToImmutableArray();
        var onboarding = application.Onboarding is { } block ? RealizeOnboarding(block) : null;
        if (providers.IsEmpty && onboarding is null)
        {
            return None;
        }

        var personas = (application.Personas ?? [])
            .Select(persona => Persona(persona, application))
            .ToImmutableArray();
        var files = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var path in (onboarding?.LegalDocuments ?? []).Concat((onboarding?.Emails ?? []).Select(email => email.Path)))
        {
            if (TryFind(texts, path, out var text))
            {
                files[path] = text;
            }
        }

        return new(
            providers,
            personas,
            onboarding,
            [.. environment.Keys.Select(key => new RealizedEnvironmentKey(key.Key, key.IsSecret))])
        {
            Texts = files.ToImmutableSortedDictionary(StringComparer.Ordinal),
            Images = ReadImages(imagesRoot, onboarding?.ImagesFolder)
        };
    }

    static bool TryFind(IReadOnlyDictionary<string, string> texts, string path, out string text)
    {
        var normalized = path.Replace('\\', '/').TrimStart('.', '/');
        foreach (var (key, value) in texts)
        {
            if (string.Equals(key.Replace('\\', '/').TrimStart('.', '/'), normalized, StringComparison.Ordinal))
            {
                text = value;
                return true;
            }
        }

        text = string.Empty;
        return false;
    }

    static RealizedPersona Persona(PersonaSyntax persona, ApplicationSyntax application)
    {
        var result = PersonaCallers.Synthesize(persona, application);
        var policies = persona.Policies.ToImmutableArray();
        if (result.Caller is not { } caller || result.Refusal is not null)
        {
            return new(persona.Name, policies, [], [], false);
        }

        return new(
            persona.Name,
            policies,
            [.. caller.Roles.Order(StringComparer.Ordinal)],
            [.. caller.Claims.Select(claim => new RealizedClaim(claim.Type, claim.Value))],
            true);
    }

    static RealizedOnboarding RealizeOnboarding(OnboardingSyntax onboarding) => new(
        onboarding.Signup is { } signup ? signup.Outcome.ToString() : string.Empty,
        [.. onboarding.Invitations.Select(invitation => new RealizedInvitation(
            invitation.Kind == OnboardingInvitationKind.Tenant ? "tenant" : "member",
            [.. invitation.By],
            [.. invitation.Grant]))],
        [.. (onboarding.LegalTerms?.Documents ?? []).Select(document => document.Path)],
        onboarding.Offering?.Name ?? string.Empty,
        [.. (onboarding.Emails?.Templates ?? []).Select(email => new RealizedEmail(email.Kind, email.File.Path))],
        onboarding.Emails?.Images?.Path ?? string.Empty);

    static ImmutableSortedDictionary<string, ImmutableArray<byte>> ReadImages(string? root, string? folder)
    {
        var images = ImmutableSortedDictionary.CreateBuilder<string, ImmutableArray<byte>>(StringComparer.Ordinal);
        if (root is null || string.IsNullOrWhiteSpace(folder))
        {
            return images.ToImmutable();
        }

        var directory = new DirectoryInfo(Path.GetFullPath(folder, root));
        var relative = Path.GetRelativePath(root, directory.FullName);
        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative) || !directory.Exists || directory.LinkTarget is not null)
        {
            return images.ToImmutable();
        }

        foreach (var file in directory.EnumerateFiles().OrderBy(file => file.Name, StringComparer.Ordinal))
        {
            if (file.LinkTarget is not null || file.Length > 2 * 1024 * 1024 || !IsImage(file.Name))
            {
                continue;
            }

            images[file.Name] = [.. File.ReadAllBytes(file.FullName)];
        }

        return images.ToImmutable();
    }

    static bool IsImage(string name) =>
        _imageExtensions.Contains(Path.GetExtension(name)) && Encoding.UTF8.GetByteCount(name) == name.Length;
}
