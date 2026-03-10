using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using RestSharp;
using YAWN.Objects;
using Settings = YAWN.Properties.Settings;

namespace YAWN.Helpers;

public static class ValApi
{
    private static readonly RestClient Client;
    private static readonly RestClient MediaClient;

    private static Urls _mapsInfo;
    private static Urls _agentsInfo;
    private static Urls _ranksInfo;
    private static Urls _versionInfo;
    private static Urls _skinsInfo;
    private static Urls _cardsInfo;
    private static Urls _spraysInfo;
    private static Urls _gamemodeInfo;
    private static Urls _buddiesInfo;
    private static Urls _titlesInfo;
    private static Urls _contentTiersInfo;
    private static List<Urls> _allInfo;

    private static readonly Dictionary<string, string> ValApiLanguages =
        new()
        {
            { "ar", "ar-AE" },
            { "de", "de-DE" },
            { "en", "en-US" },
            { "es", "es-ES" },
            { "fr", "fr-FR" },
            { "id", "id-ID" },
            { "it", "it-IT" },
            { "ja", "ja-JP" },
            { "ko", "ko-KR" },
            { "pl", "pl-PL" },
            { "pt", "pt-BR" },
            { "ru", "ru-RU" },
            { "th", "th-TH" },
            { "tr", "tr-TR" },
            { "vi", "vi-VN" },
            { "zh", "zh-CN" }
        };

    static ValApi()
    {
        Client = new RestClient("https://valorant-api.com/v1");
        MediaClient = new RestClient();
    }

    private static async Task<RestResponse<T>> Fetch<T>(string url)
    {
        var request = new RestRequest(url);
        return await Client.ExecuteGetAsync<T>(request).ConfigureAwait(false);
    }

    private static async Task<string> GetValApiVersionAsync()
    {
        var response = await Fetch<VapiVersionResponse>("/version");
        return !response.IsSuccessful ? null : response.Data.Data.BuildDate;
    }

    private static async Task<string> GetLocalValApiVersionAsync()
    {
        if (!File.Exists(Constants.LocalAppDataPath + "\\ValAPI\\version.json"))
            return null;
        try
        {
            var lines = await File.ReadAllLinesAsync(
                    Constants.LocalAppDataPath + "\\ValAPI\\version.json"
                )
                .ConfigureAwait(false);
            return lines[1];
        }
        catch
        {
            return "";
        }
    }

    private static Task GetUrlsAsync()
    {
        var language = ValApiLanguages.GetValueOrDefault(Settings.Default.Language, "en-US");
        _mapsInfo = new Urls
        {
            Name = "Maps",
            Filepath = Constants.LocalAppDataPath + "\\ValAPI\\maps.json",
            Url = $"/maps?language={language}"
        };
        _agentsInfo = new Urls
        {
            Name = "Agents",
            Filepath = Constants.LocalAppDataPath + "\\ValAPI\\agents.json",
            Url = $"/agents?language={language}"
        };
        _skinsInfo = new Urls
        {
            Name = "Skins",
            Filepath = Constants.LocalAppDataPath + "\\ValAPI\\skinchromas.json",
            Url = $"/weapons/skinchromas?language={language}"
        };
        _cardsInfo = new Urls
        {
            Name = "Cards",
            Filepath = Constants.LocalAppDataPath + "\\ValAPI\\cards.json",
            Url = $"/playercards?language={language}"
        };
        _spraysInfo = new Urls
        {
            Name = "Sprays",
            Filepath = Constants.LocalAppDataPath + "\\ValAPI\\sprays.json",
            Url = $"/sprays?language={language}"
        };
        _ranksInfo = new Urls
        {
            Name = "Ranks",
            Filepath = Constants.LocalAppDataPath + "\\ValAPI\\competitivetiers.json",
            Url = $"/competitivetiers?language={language}"
        };
        _versionInfo = new Urls
        {
            Name = "Version",
            Filepath = Constants.LocalAppDataPath + "\\ValAPI\\version.json",
            Url = "/version"
        };
        _gamemodeInfo = new Urls
        {
            Name = "Gamemode",
            Filepath = Constants.LocalAppDataPath + "\\ValAPI\\gamemode.json",
            Url = $"/gamemodes?language={language}"
        };
        _buddiesInfo = new Urls
        {
            Name = "Buddies",
            Filepath = Constants.LocalAppDataPath + "\\ValAPI\\buddies.json",
            Url = $"/buddies/levels?language={language}"
        };
        _titlesInfo = new Urls
        {
            Name = "Titles",
            Filepath = Constants.LocalAppDataPath + "\\ValAPI\\titles.json",
            Url = $"/playertitles?language={language}"
        };
        _contentTiersInfo = new Urls
        {
            Name = "ContentTiers",
            Filepath = Constants.LocalAppDataPath + "\\ValAPI\\contenttiers.json",
            Url = $"/contenttiers?language={language}"
        };
        _allInfo = new List<Urls>
        {
            _mapsInfo,
            _agentsInfo,
            _ranksInfo,
            _versionInfo,
            _skinsInfo,
            _cardsInfo,
            _spraysInfo,
            _gamemodeInfo,
            _buddiesInfo,
            _titlesInfo,
            _contentTiersInfo
        };
        return Task.CompletedTask;
    }

    public static async Task UpdateFilesAsync()
    {
        Constants.Log.Information("UpdateFilesAsync: Starting file update");
        try
        {
            await GetUrlsAsync().ConfigureAwait(false);
            if (!Directory.Exists(Constants.LocalAppDataPath + "\\ValAPI"))
                Directory.CreateDirectory(Constants.LocalAppDataPath + "\\ValAPI");

            async Task UpdateVersion()
            {
                var versionRequest = new RestRequest(_versionInfo.Url);
                var versionResponse = await Client
                    .ExecuteGetAsync<VapiVersionResponse>(versionRequest)
                    .ConfigureAwait(false);
                if (!versionResponse.IsSuccessful)
                {
                    Constants.Log.Error(
                        "updateVersion Failed, Response:{error}",
                        versionResponse.ErrorException
                    );
                    return;
                }
                string[] lines =
                {
                    versionResponse.Data?.Data.RiotClientVersion,
                    versionResponse.Data?.Data.BuildDate
                };
                await File.WriteAllLinesAsync(_versionInfo.Filepath, lines).ConfigureAwait(false);
            }

            async Task UpdateMapsDictionary()
            {
                var mapsResponse = await Fetch<ValApiMapsResponse>(_mapsInfo.Url);
                if (!mapsResponse.IsSuccessful)
                {
                    Constants.Log.Error(
                        "updateMapsDictionary Failed, Response:{error}",
                        mapsResponse.ErrorException
                    );
                    return;
                }
                Dictionary<string, ValMap> mapsDictionary = new();
                if (!Directory.Exists(Constants.LocalAppDataPath + "\\ValAPI\\mapsimg"))
                    Directory.CreateDirectory(Constants.LocalAppDataPath + "\\ValAPI\\mapsimg");
                if (mapsResponse.Data?.Data != null)
                    foreach (var map in mapsResponse.Data.Data)
                    {
                        mapsDictionary.TryAdd(
                            map.MapUrl,
                            new ValMap { Name = map.DisplayName, UUID = map.Uuid }
                        );
                        var fileName =
                            Constants.LocalAppDataPath + $"\\ValAPI\\mapsimg\\{map.Uuid}.png";
                        var request = new RestRequest(map.ListViewIcon);
                        var response = await MediaClient
                            .DownloadDataAsync(request)
                            .ConfigureAwait(false);
                        if (response != null)
                        {
                            try
                            {
                                await File.WriteAllBytesAsync(fileName, response).ConfigureAwait(false);
                            }
                            catch (IOException)
                            {
                                Constants.Log.Warning("Map image in use, skipping update: {Path}", fileName);
                            }
                        }
                    }

                await File.WriteAllTextAsync(
                        _mapsInfo.Filepath,
                        JsonSerializer.Serialize(mapsDictionary)
                    )
                    .ConfigureAwait(false);
            }

            async Task UpdateAgentsDictionary()
            {
                var agentsResponse = await Fetch<ValApiAgentsResponse>(_agentsInfo.Url);
                if (!agentsResponse.IsSuccessful)
                {
                    Constants.Log.Error(
                        "updateAgentsDictionary Failed, Response:{error}",
                        agentsResponse.ErrorException
                    );
                    return;
                }
                Dictionary<Guid, string> agentsDictionary = new();
                if (!Directory.Exists(Constants.LocalAppDataPath + "\\ValAPI\\agentsimg"))
                    Directory.CreateDirectory(Constants.LocalAppDataPath + "\\ValAPI\\agentsimg");
                if (agentsResponse.Data != null)
                    foreach (var agent in agentsResponse.Data.Data)
                    {
                        agentsDictionary.TryAdd(agent.Uuid, agent.DisplayName);

                        var fileName =
                            Constants.LocalAppDataPath + $"\\ValAPI\\agentsimg\\{agent.Uuid}.png";
                        var request = new RestRequest(agent.DisplayIcon);
                        var response = await MediaClient
                            .DownloadDataAsync(request)
                            .ConfigureAwait(false);
                        if (response != null)
                        {
                            try
                            {
                                await File.WriteAllBytesAsync(fileName, response).ConfigureAwait(false);
                            }
                            catch (IOException)
                            {
                                Constants.Log.Warning("Agent image in use, skipping update: {Path}", fileName);
                            }
                        }
                    }

                await File.WriteAllTextAsync(
                        _agentsInfo.Filepath,
                        JsonSerializer.Serialize(agentsDictionary)
                    )
                    .ConfigureAwait(false);
            }

            async Task UpdateSkinsDictionary()
            {
                var skinsResponse = await Fetch<ValApiSkinsResponse>(_skinsInfo.Url);
                if (!skinsResponse.IsSuccessful)
                {
                    Constants.Log.Error(
                        "updateSkinsDictionary Failed, Response:{error}",
                        skinsResponse.ErrorException
                    );
                    return;
                }
                Dictionary<Guid, ValNameImage> skinsDictionary = new();
                if (skinsResponse.Data != null)
                    foreach (var skin in skinsResponse.Data.Data)
                        skinsDictionary.TryAdd(
                            skin.Uuid,
                            new ValNameImage { Name = skin.DisplayName, Image = skin.FullRender }
                        );
                await File.WriteAllTextAsync(
                        _skinsInfo.Filepath,
                        JsonSerializer.Serialize(skinsDictionary)
                    )
                    .ConfigureAwait(false);
            }

            async Task UpdateCardsDictionary()
            {
                var cardsResponse = await Fetch<ValApiCardsResponse>(_cardsInfo.Url);
                if (!cardsResponse.IsSuccessful)
                {
                    Constants.Log.Error(
                        "updateCardsDictionary Failed, Response:{error}",
                        cardsResponse.ErrorException
                    );
                    return;
                }
                Dictionary<Guid, ValCard> cardsDictionary = new();
                if (cardsResponse.Data != null)
                    foreach (var card in cardsResponse.Data.Data)
                        cardsDictionary.TryAdd(
                            card.Uuid,
                            new ValCard
                            {
                                Name = card.DisplayName,
                                Image = card.DisplayIcon,
                                FullImage = card.LargeArt
                            }
                        );
                await File.WriteAllTextAsync(
                        _cardsInfo.Filepath,
                        JsonSerializer.Serialize(cardsDictionary)
                    )
                    .ConfigureAwait(false);
            }

            async Task UpdateSpraysDictionary()
            {
                var spraysResponse = await Fetch<ValApiSpraysResponse>(_spraysInfo.Url);
                if (!spraysResponse.IsSuccessful)
                {
                    Constants.Log.Error(
                        "updateSpraysDictionary Failed, Response:{error}",
                        spraysResponse.ErrorException
                    );
                    return;
                }
                Dictionary<Guid, ValNameImage> spraysDictionary = new();
                if (spraysResponse.Data != null)
                    foreach (var spray in spraysResponse.Data.Data)
                    {
                        var image = spray.FullTransparentIcon ?? spray.DisplayIcon ?? (spray.Levels.Length > 0 ? spray.Levels[0].DisplayIcon : null);
                        
                        if (image == null)
                        {
                            Constants.Log.Warning(
                                "Spray image is NULL - UUID: {Uuid}, Name: {Name}, FullTransparentIcon: {FullIcon}, DisplayIcon: {DisplayIcon}, LevelsCount: {LevelsCount}",
                                spray.Uuid,
                                spray.DisplayName,
                                spray.FullTransparentIcon?.ToString() ?? "null",
                                spray.DisplayIcon?.ToString() ?? "null",
                                spray.Levels.Length
                            );
                        }
                        
                        spraysDictionary.TryAdd(
                            spray.Uuid,
                            new ValNameImage
                            {
                                Name = spray.DisplayName,
                                Image = image
                            }
                        );
                    }
                await File.WriteAllTextAsync(
                        _spraysInfo.Filepath,
                        JsonSerializer.Serialize(spraysDictionary)
                    )
                    .ConfigureAwait(false);
            }

            async Task UpdateRanksDictionary()
            {
                var ranksRequest = new RestRequest(_ranksInfo.Url);
                var ranksResponse = await Client
                    .ExecuteGetAsync<ValApiRanksResponse>(ranksRequest)
                    .ConfigureAwait(false);
                if (!ranksResponse.IsSuccessful)
                {
                    Constants.Log.Error(
                        "updateRanksDictionary Failed, Response:{error}",
                        ranksResponse.ErrorException
                    );
                    return;
                }
                Dictionary<int, string> ranksDictionary = new();
                if (!Directory.Exists(Constants.LocalAppDataPath + "\\ValAPI\\ranksimg"))
                    Directory.CreateDirectory(Constants.LocalAppDataPath + "\\ValAPI\\ranksimg");
                if (ranksResponse.Data != null)
                    foreach (var rank in ranksResponse.Data.Data.Last().Tiers)
                    {
                        var tier = rank.TierTier;
                        ranksDictionary.TryAdd(tier, rank.TierName);

                        switch (tier)
                        {
                            case 1
                            or 2:
                                continue;
                            case 0:
                            {
                                const string imagePath = "pack://application:,,,/Assets/0.png";
                                var imageInfo = Application.GetResourceStream(new Uri(imagePath));
                                using var ms = new MemoryStream();
                                if (imageInfo != null)
                                {
                                    await imageInfo.Stream.CopyToAsync(ms);
                                    var imageBytes = ms.ToArray();
                                    
                                    var filePath = Constants.LocalAppDataPath + "\\ValAPI\\ranksimg\\0.png";
                                    
                                    try
                                    {
                                        if (File.Exists(filePath))
                                        {
                                            File.Delete(filePath);
                                        }
                                        await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                                        await fs.WriteAsync(imageBytes);
                                    }
                                    catch (IOException)
                                    {
                                        Constants.Log.Warning("0.png file in use, skipping update");
                                    }
                                }

                                continue;
                            }
                        }

                        var fileName =
                            Constants.LocalAppDataPath + $"\\ValAPI\\ranksimg\\{tier}.png";

                        var request = new RestRequest(rank.LargeIcon);
                        var response = await MediaClient
                            .DownloadDataAsync(request)
                            .ConfigureAwait(false);

                        if (response != null)
                        {
                            try
                            {
                                if (File.Exists(fileName))
                                {
                                    File.Delete(fileName);
                                }
                                await using var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
                                await fs.WriteAsync(response);
                            }
                            catch (IOException)
                            {
                                Constants.Log.Warning("Rank file in use, skipping: {FileName}", fileName);
                            }
                        }
                    }

                await File.WriteAllTextAsync(
                        _ranksInfo.Filepath,
                        JsonSerializer.Serialize(ranksDictionary)
                    )
                    .ConfigureAwait(false);
            }

            async Task UpdateGamemodeDictionary()
            {
                var gameModeResponse = await Fetch<ValApiGamemodeResponse>(_gamemodeInfo.Url);
                if (!gameModeResponse.IsSuccessful)
                {
                    Constants.Log.Error(
                        "updateGamemodeDictionary Failed, Response:{error}",
                        gameModeResponse.ErrorException
                    );
                    return;
                }
                Dictionary<string, ValGamemodeInfo> gamemodeDictionary = new();
                if (!Directory.Exists(Constants.LocalAppDataPath + "\\ValAPI\\gamemodeimg"))
                    Directory.CreateDirectory(Constants.LocalAppDataPath + "\\ValAPI\\gamemodeimg");
                if (gameModeResponse.Data != null)
                    foreach (var gamemode in gameModeResponse.Data.Data)
                    {
                        if (gamemode.DisplayIcon == null)
                            continue;
                        
                        // Use AssetPath as key if available, otherwise Uuid
                        var key = !string.IsNullOrEmpty(gamemode.AssetPath) ? gamemode.AssetPath : gamemode.Uuid.ToString();
                        
                        var info = new ValGamemodeInfo 
                        { 
                            Uuid = gamemode.Uuid, 
                            Name = gamemode.DisplayName,
                            AssetPath = gamemode.AssetPath
                        };
                        
                        gamemodeDictionary.TryAdd(key, info);
                        // Also add UUID as key for fallback lookups
                        gamemodeDictionary.TryAdd(gamemode.Uuid.ToString(), info);

                        var fileName =
                            Constants.LocalAppDataPath
                            + $"\\ValAPI\\gamemodeimg\\{gamemode.Uuid}.png";
                        var request = new RestRequest(gamemode.DisplayIcon);
                        var response = await MediaClient
                            .DownloadDataAsync(request)
                            .ConfigureAwait(false);
                        
                        if (response != null)
                        {
                            try 
                            {
                                await File.WriteAllBytesAsync(fileName, response).ConfigureAwait(false);
                            }
                            catch (IOException)
                            {
                                // File is likely in use, skip updating this specific image
                                Constants.Log.Warning("Gamemode image in use, skipping update: {Path}", fileName);
                            }
                        }
                    }

                await File.WriteAllTextAsync(
                        _gamemodeInfo.Filepath,
                        JsonSerializer.Serialize(gamemodeDictionary)
                    )
                    .ConfigureAwait(false);
            }

            async Task UpdateBuddiesDictionary()
            {
                var buddiesResponse = await Fetch<ValApiBuddiesResponse>(_buddiesInfo.Url);
                if (!buddiesResponse.IsSuccessful)
                {
                    Constants.Log.Error(
                        "updateBuddiesDictionary Failed, Response:{error}",
                        buddiesResponse.ErrorException
                    );
                    return;
                }
                Dictionary<Guid, ValNameImage> buddiesDictionary = new();
                if (buddiesResponse.Data?.Data != null)
                    foreach (var buddy in buddiesResponse.Data.Data)
                        buddiesDictionary.TryAdd(
                            buddy.Uuid,
                            new ValNameImage { Name = buddy.DisplayName, Image = buddy.DisplayIcon }
                        );
                await File.WriteAllTextAsync(
                        _buddiesInfo.Filepath,
                        JsonSerializer.Serialize(buddiesDictionary)
                    )
                    .ConfigureAwait(false);
            }

            async Task UpdateTitlesDictionary()
            {
                var titlesResponse = await Fetch<ValApiPlayerTitlesResponse>(_titlesInfo.Url);
                if (!titlesResponse.IsSuccessful)
                {
                    Constants.Log.Error(
                        "updateTitlesDictionary Failed, Response:{error}",
                        titlesResponse.ErrorException
                    );
                    return;
                }
                Dictionary<Guid, string> titlesDictionary = new();
                if (titlesResponse.Data?.Data != null)
                    foreach (var title in titlesResponse.Data.Data)
                        titlesDictionary.TryAdd(title.Uuid, title.TitleText ?? title.DisplayName);
                await File.WriteAllTextAsync(
                        _titlesInfo.Filepath,
                        JsonSerializer.Serialize(titlesDictionary)
                    )
                    .ConfigureAwait(false);
            }

            async Task UpdateContentTiersDictionary()
            {
                var contentTiersResponse = await Fetch<ValApiContentTiersResponse>(_contentTiersInfo.Url);
                if (!contentTiersResponse.IsSuccessful)
                {
                    Constants.Log.Error(
                        "updateContentTiersDictionary Failed, Response:{error}",
                        contentTiersResponse.ErrorException
                    );
                    return;
                }
                Dictionary<Guid, ValContentTier> contentTiersDictionary = new();
                if (contentTiersResponse.Data?.Data != null)
                    foreach (var tier in contentTiersResponse.Data.Data)
                        contentTiersDictionary.TryAdd(
                            tier.Uuid,
                            new ValContentTier
                            {
                                Name = tier.DisplayName,
                                DevName = tier.DevName,
                                Rank = tier.Rank,
                                HighlightColor = tier.HighlightColor,
                                Icon = tier.DisplayIcon
                            }
                        );
                await File.WriteAllTextAsync(
                        _contentTiersInfo.Filepath,
                        JsonSerializer.Serialize(contentTiersDictionary)
                    )
                    .ConfigureAwait(false);
            }

            try
            {
                await Task.WhenAll(
                        UpdateVersion(),
                        UpdateRanksDictionary(),
                        UpdateAgentsDictionary(),
                        UpdateMapsDictionary(),
                        UpdateSkinsDictionary(),
                        UpdateCardsDictionary(),
                        UpdateSpraysDictionary(),
                        UpdateGamemodeDictionary(),
                        UpdateBuddiesDictionary(),
                        UpdateTitlesDictionary(),
                        UpdateContentTiersDictionary()
                    )
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Constants.Log.Error(
                    "updateGamemodeDictionary Parralel Tasks Failed, Response:{error}",
                    e
                );
            }
        }
        catch (Exception e)
        {
            Constants.Log.Error("UpdateFilesAsync Failed, Response:{error}", e);
        }
        
        Constants.Log.Information("UpdateFilesAsync: File update completed");
    }

    public static string GetMapName(string mapUrl)
    {
        try
        {
            var maps = JsonSerializer.Deserialize<Dictionary<string, ValMap>>(
                File.ReadAllText(Constants.LocalAppDataPath + "\\ValAPI\\maps.json")
            );
            if (maps.TryGetValue(mapUrl, out var map))
                return map.Name;
        }
        catch { }
        
        return mapUrl switch
        {
            "/Game/Maps/Ascent/Ascent" => "ASCENT",
            "/Game/Maps/Bonsai/Bonsai" => "SPLIT",
            "/Game/Maps/Canyon/Canyon" => "FRACTURE",
            "/Game/Maps/Duality/Duality" => "BIND",
            "/Game/Maps/Foxtrot/Foxtrot" => "BREEZE",
            "/Game/Maps/Jam/Jam" => "LOTUS",
            "/Game/Maps/Infinity/Infinity" => "ABYSS",
            "/Game/Maps/Juliett/Juliett" => "SUNSET",
            "/Game/Maps/Pitt/Pitt" => "PEARL",
            "/Game/Maps/Port/Port" => "ICEBOX",
            "/Game/Maps/Triad/Triad" => "HAVEN",
            "/Game/Maps/HURM/HURM_Yard/HURM_Yard" => "PIAZZA",
            "/Game/Maps/HURM/HURM_Alley/HURM_Alley" => "DISTRICT",
            "/Game/Maps/HURM/HURM_Bowl/HURM_Bowl" => "KASBAH",
            "/Game/Maps/HURM/HURM_Helix/HURM_Helix" => "DRIFT",
            _ => "UNKNOWN MAP"
        };
    }

    public static string GetMapIdFromUrl(string mapUrl)
    {
        try
        {
            var mapsText = File.ReadAllText(Constants.LocalAppDataPath + "\\ValAPI\\maps.json");
            var maps = JsonSerializer.Deserialize<Dictionary<string, ValMap>>(mapsText);
            if (maps != null && maps.TryGetValue(mapUrl, out var map))
                return map.UUID.ToString().ToLower();
        }
        catch (Exception ex)
        {
            Constants.Log.Error("GetMapIdFromUrl failed for {MapUrl}: {Error}", mapUrl, ex.Message);
        }
        return null;
    }

    public static string GetQueueName(string queueId)
    {
        try
        {
            var jsonContent = File.ReadAllText(Constants.LocalAppDataPath + "\\ValAPI\\gamemode.json");
            
            // Check if JSON content is in old format (Dictionary<string, string>)
            if (jsonContent.Trim().StartsWith("{\""))
            {
                // Simple heuristic check: see if values are strings or objects
                // But since we control the serialization, we can try to deserialize as new format first
                // If it fails, fallback to old format handling or just catch exception
                
                try 
                {
                    var queues = JsonSerializer.Deserialize<Dictionary<string, ValGamemodeInfo>>(jsonContent);
                    if (queues != null)
                    {
                        // Try to find by key (AssetPath or Uuid)
                        if (queues.TryGetValue(queueId, out var info))
                            return info.Name;
                            
                        // Fallback: try to find by Uuid if queueId is a Guid string
                        if (Guid.TryParse(queueId, out var guid))
                        {
                            var match = queues.Values.FirstOrDefault(q => q.Uuid == guid);
                            if (match != null) return match.Name;
                        }
                    }
                }
                catch (JsonException)
                {
                    // Fallback for backward compatibility if file hasn't been updated yet
                    var queuesOld = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
                    if (queuesOld != null && queuesOld.TryGetValue(queueId, out var name))
                        return name;
                }
            }
        }
        catch { }
        
        return queueId switch
        {
            "unrated" => "STANDART",
            "competitive" => "REKABET\u00C7\u0130",
            "swiftplay" => "TAM GAZ",
            "spikerush" => "SPIKE\u0027A H\u00DCCUM",
            "deathmatch" => "\u00D6L\u00DCM KALIM SAVA\u015EI",
            "ggteam" => "TIRMANI\u015E",
            "onefortheall" => "KOPYA",
            "snowball" => "KARTOPU \u00C7ATI\u015EMASI",
            "hurm" => "TAKIMLI \u00D6L\u00DCM KALIM SAVA\u015EI",
            _ => queueId?.ToUpper() ?? "CUSTOM"
        };
    }

    public static ValGamemodeInfo GetGamemodeInfo(string modeId)
    {
        try
        {
            var jsonContent = File.ReadAllText(Constants.LocalAppDataPath + "\\ValAPI\\gamemode.json");
            Dictionary<string, ValGamemodeInfo> queues = null;

            try 
            {
                queues = JsonSerializer.Deserialize<Dictionary<string, ValGamemodeInfo>>(jsonContent);
            }
            catch (JsonException)
            {
                // File might be in old format, force update or return null
                Constants.Log.Warning("GetGamemodeInfo: gamemode.json is in old format or invalid.");
                return null;
            }
            
            if (queues != null && queues.TryGetValue(modeId, out var info))
                return info;
        }
        catch (Exception ex)
        {
            Constants.Log.Error("GetGamemodeInfo failed for {ModeId}: {Error}", modeId, ex.Message);
        }
        return null;
    }

    public static async Task CheckAndUpdateJsonAsync()
    {
        try
        {
            Constants.Log.Information("CheckAndUpdateJsonAsync: Starting file check");
            await GetUrlsAsync().ConfigureAwait(false);

            var remoteVersion = await GetValApiVersionAsync().ConfigureAwait(false);
            var localVersion = await GetLocalValApiVersionAsync().ConfigureAwait(false);
            
            Constants.Log.Information("CheckAndUpdateJsonAsync: Remote version={Remote}, Local version={Local}", remoteVersion, localVersion);
            
            if (remoteVersion != localVersion || true) // Force update to get new sprays/assets
            {
                Constants.Log.Information("CheckAndUpdateJsonAsync: Updating files to ensure latest assets");
                await UpdateFilesAsync().ConfigureAwait(false);
                return;
            }

            var missingFiles = _allInfo.Where(url => !File.Exists(url.Filepath)).ToList();
            if (missingFiles.Any())
            {
                Constants.Log.Information("CheckAndUpdateJsonAsync: {Count} missing files detected, updating", missingFiles.Count);
                await UpdateFilesAsync().ConfigureAwait(false);
            }
            else
            {
                Constants.Log.Information("CheckAndUpdateJsonAsync: All files up to date");
            }
        }
        catch (Exception e)
        {
            Constants.Log.Error("CheckAndUpdateJsonAsync Failed: {e}", e);
            // ignored
        }
    }
}
