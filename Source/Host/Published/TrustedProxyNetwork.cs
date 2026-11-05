// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;

namespace Cratis.Stage.Host.Published;

/// <summary>
/// An IP address or CIDR network a sign-in proxy may connect from.
/// </summary>
/// <param name="Prefix">The network address.</param>
/// <param name="Length">The prefix length in bits of the address family of <paramref name="Prefix"/>.</param>
internal sealed record TrustedProxyNetwork(IPAddress Prefix, int Length)
{
    internal static bool TryParse(string value, out TrustedProxyNetwork network)
    {
        network = null!;
        var parts = value.Split('/', 2);
        if (!IPAddress.TryParse(parts[0], out var address))
        {
            return false;
        }

        var maximum = address.GetAddressBytes().Length * 8;
        var length = maximum;
        if (parts.Length == 2 && (!int.TryParse(parts[1], out length) || length < 0 || length > maximum))
        {
            return false;
        }

        network = new TrustedProxyNetwork(address, length);
        return true;
    }

    internal bool Contains(IPAddress? candidate)
    {
        if (candidate is null)
        {
            return false;
        }

        var left = Normalize(Prefix);
        var right = Normalize(candidate);
        var bits = Length + (Prefix.AddressFamily == AddressFamily.InterNetwork ? 96 : 0);
        for (var index = 0; index < left.Length && bits > 0; index++)
        {
            var mask = bits >= 8 ? 0xFF : (0xFF << (8 - bits)) & 0xFF;
            if ((left[index] & mask) != (right[index] & mask))
            {
                return false;
            }

            bits -= 8;
        }

        return true;
    }

    static byte[] Normalize(IPAddress address) => (address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address).MapToIPv6().GetAddressBytes();
}
