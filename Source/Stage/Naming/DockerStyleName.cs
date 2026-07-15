// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Naming;

/// <summary>
/// Generates docker-style <c>&lt;adjective&gt;-&lt;noun&gt;</c> names, used as the ephemeral event store name for an engine run.
/// </summary>
public static class DockerStyleName
{
    static readonly string[] _adjectives =
    [
        "admiring", "amazing", "bold", "brave", "busy", "clever", "cool", "crisp", "dazzling", "eager",
        "elegant", "fervent", "gallant", "gentle", "happy", "jolly", "keen", "lucid", "merry", "modest",
        "nifty", "peaceful", "quirky", "serene", "sharp", "silly", "stoic", "sweet", "vibrant", "zen",
    ];

    static readonly string[] _nouns =
    [
        "albatross", "badger", "comet", "dolphin", "ember", "falcon", "glacier", "harbor", "ibex", "jaguar",
        "kestrel", "lantern", "meadow", "nebula", "otter", "pioneer", "quartz", "raven", "summit", "tundra",
        "umbra", "vortex", "willow", "xenon", "yonder", "zephyr", "aurora", "basalt", "cedar", "delta",
    ];

    /// <summary>
    /// Generates a new docker-style name.
    /// </summary>
    /// <returns>A name in the form <c>&lt;adjective&gt;-&lt;noun&gt;</c>.</returns>
    public static string Generate()
    {
        var adjective = _adjectives[Random.Shared.Next(_adjectives.Length)];
        var noun = _nouns[Random.Shared.Next(_nouns.Length)];

        return $"{adjective}-{noun}";
    }
}
