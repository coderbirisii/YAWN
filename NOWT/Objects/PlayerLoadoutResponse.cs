using System;
using System.Text.Json.Serialization;

namespace NOWT.Objects;

public class PlayerLoadoutResponse
{
    [JsonPropertyName("Subject")]
    public Guid Subject { get; set; }

    [JsonPropertyName("Version")]
    public int Version { get; set; }

    [JsonPropertyName("Guns")]
    public Gun[] Guns { get; set; }

    [JsonPropertyName("Sprays")]
    public SprayEquip[] Sprays { get; set; }

    [JsonPropertyName("Identity")]
    public LoadoutIdentity Identity { get; set; }

    [JsonPropertyName("Incognito")]
    public bool Incognito { get; set; }
}

public class Gun
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("SkinID")]
    public Guid SkinId { get; set; }

    [JsonPropertyName("SkinLevelID")]
    public Guid SkinLevelId { get; set; }

    [JsonPropertyName("ChromaID")]
    public Guid ChromaId { get; set; }

    [JsonPropertyName("CharmInstanceID")]
    public Guid? CharmInstanceId { get; set; }

    [JsonPropertyName("CharmID")]
    public Guid? CharmId { get; set; }

    [JsonPropertyName("CharmLevelID")]
    public Guid? CharmLevelId { get; set; }

    [JsonPropertyName("Attachments")]
    public object[] Attachments { get; set; }
}

public class SprayEquip
{
    [JsonPropertyName("EquipSlotID")]
    public Guid EquipSlotId { get; set; }

    [JsonPropertyName("SprayID")]
    public Guid SprayId { get; set; }

    [JsonPropertyName("SprayLevelID")]
    public Guid? SprayLevelId { get; set; }
}

public class LoadoutIdentity
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
