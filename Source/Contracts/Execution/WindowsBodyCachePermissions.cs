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
        if (!Trusted((SecurityIdentifier)security.GetOwner(typeof(SecurityIdentifier))!, user)) throw new UnauthorizedAccessException("Body cache parent has an untrusted owner.");
        if (security.GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>().Any(rule => UntrustedWrite(rule, user, cache)))
        {
            throw new UnauthorizedAccessException("Body cache must be protected from other users, including its parent directories.");
        }
    }

    static bool UntrustedWrite(FileSystemAccessRule rule, SecurityIdentifier user, bool cache)
    {
        var owner = (SecurityIdentifier)rule.IdentityReference;
        var trusted = Trusted(owner, user);
        var mutation = FileSystemRights.DeleteSubdirectoriesAndFiles | FileSystemRights.ChangePermissions | FileSystemRights.TakeOwnership;
        if (cache) mutation |= FileSystemRights.Write | FileSystemRights.Delete;
        return !trusted && rule.AccessControlType == AccessControlType.Allow && (rule.FileSystemRights & mutation) != default;
    }

    static bool Trusted(SecurityIdentifier owner, SecurityIdentifier user) =>
        owner.Equals(user) || owner.IsWellKnown(WellKnownSidType.LocalSystemSid) || owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid);
}
