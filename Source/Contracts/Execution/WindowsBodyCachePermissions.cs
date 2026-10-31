// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Cratis.Stage.Contracts.Execution;

[SupportedOSPlatform("windows")]
internal static class WindowsBodyCachePermissions
{
    internal static void Validate(string path)
    {
        using var identity = WindowsIdentity.GetCurrent();
        var user = identity.User!;
        DirectoryInfo? directory = new(path);
        var security = directory.GetAccessControl();
        if (!user.Equals(security.GetOwner(typeof(SecurityIdentifier)))) throw new UnauthorizedAccessException("Body cache must be owned by the current user.");
        var cache = true;
        for (; directory is not null; directory = directory.Parent)
        {
            ValidateAccess(directory, user, cache);
            cache = false;
        }
    }

    static void ValidateAccess(DirectoryInfo directory, SecurityIdentifier user, bool cache)
    {
        var security = directory.GetAccessControl();
        if (!BodyCacheAccessPolicy.TrustedOwner(Principal((SecurityIdentifier)security.GetOwner(typeof(SecurityIdentifier))!, user))) throw new UnauthorizedAccessException("Body cache parent has an untrusted owner.");
        var rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>().Select(rule => Describe(rule, user));
        if (!BodyCacheAccessPolicy.IsProtected(rules, cache))
        {
            throw new UnauthorizedAccessException("Body cache must be protected from other users, including its parent directories.");
        }
    }

    static BodyCacheAccessRule Describe(FileSystemAccessRule rule, SecurityIdentifier user) => new(
        Principal((SecurityIdentifier)rule.IdentityReference, user),
        Rights(rule.FileSystemRights),
        rule.AccessControlType == AccessControlType.Allow,
        rule.PropagationFlags.HasFlag(PropagationFlags.InheritOnly),
        rule.IsInherited);

    static BodyCacheRights Rights(FileSystemRights native)
    {
        var rights = BodyCacheRights.None;
        if ((native & FileSystemRights.Write) != default) rights |= BodyCacheRights.Write;
        if (native.HasFlag(FileSystemRights.Delete)) rights |= BodyCacheRights.Delete;
        if (native.HasFlag(FileSystemRights.DeleteSubdirectoriesAndFiles)) rights |= BodyCacheRights.ReplaceChildren;
        if ((native & (FileSystemRights.ChangePermissions | FileSystemRights.TakeOwnership)) != default) rights |= BodyCacheRights.Control;
        return rights;
    }

    static BodyCachePrincipal Principal(SecurityIdentifier identity, SecurityIdentifier user)
    {
        if (identity.Equals(user)) return BodyCachePrincipal.CurrentUser;
        if (identity.IsWellKnown(WellKnownSidType.LocalSystemSid)) return BodyCachePrincipal.System;
        if (identity.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid)) return BodyCachePrincipal.Administrators;
        if (identity.IsWellKnown(WellKnownSidType.CreatorOwnerSid)) return BodyCachePrincipal.CreatorOwner;
        if (identity.IsWellKnown(WellKnownSidType.CreatorGroupSid)) return BodyCachePrincipal.CreatorGroup;
        return BodyCachePrincipal.Other;
    }
}
