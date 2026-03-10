using System;
using System.Text.Json.Serialization;

namespace YAWN.Objects;

public class PreMatchLoadoutsExtendedResponse
{
    [JsonPropertyName("Loadouts")]
    public PreMatchPlayerLoadout[] Loadouts { get; set; }

    [JsonPropertyName("LoadoutsValid")]
    public bool LoadoutsValid { get; set; }
}

public class PreMatchPlayerLoadout
{
    [JsonPropertyName("CharacterID")]
    public Guid CharacterId { get; set; }

    [JsonPropertyName("Loadout")]
    public PreMatchLoadoutData Loadout { get; set; }
}

public class PreMatchLoadoutData
{
    [JsonPropertyName("Sprays")]
    public PreMatchSprays Sprays { get; set; }

    [JsonPropertyName("Items")]
    public System.Collections.Generic.Dictionary<string, PreMatchItemValue> Items { get; set; }

    [JsonPropertyName("Identity")]
    public PreMatchIdentity Identity { get; set; }
}

public class PreMatchSprays
{
    [JsonPropertyName("SpraySelections")]
    public PreMatchSpraySelection[] SpraySelections { get; set; }
}

public class PreMatchSpraySelection
{
    [JsonPropertyName("SocketID")]
    public Guid SocketId { get; set; }

    [JsonPropertyName("SprayID")]
    public Guid SprayId { get; set; }

    [JsonPropertyName("LevelID")]
    public Guid LevelId { get; set; }
}

public class PreMatchItemValue
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("TypeID")]
    public Guid TypeId { get; set; }

    [JsonPropertyName("Sockets")]
    public System.Collections.Generic.Dictionary<string, PreMatchSocket> Sockets { get; set; }
}

public class PreMatchSocket
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Item")]
    public PreMatchSocketItem Item { get; set; }
}

public class PreMatchSocketItem
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("TypeID")]
    public Guid TypeId { get; set; }
}

public class PreMatchIdentity
{
    [JsonPropertyName("PlayerCardID")]
    public Guid PlayerCardId { get; set; }

    [JsonPropertyName("PlayerTitleID")]
    public Guid PlayerTitleId { get; set; }

    [JsonPropertyName("AccountLevel")]
    public int AccountLevel { get; set; }

    [JsonPropertyName("PreferredLevelBorderID")]
    public Guid PreferredLevelBorderId { get; set; }

    [JsonPropertyName("HideAccountLevel")]
    public bool HideAccountLevel { get; set; }
}
