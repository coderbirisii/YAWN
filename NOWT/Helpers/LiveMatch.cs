using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using RestSharp;
using RestSharp.Serializers.Json;
using NOWT.Objects;
using NOWT.Properties;
using static NOWT.Helpers.Login;

namespace NOWT.Helpers;

public class LiveMatch
{
    public delegate void UpdateProgress(int percentage);

    public MatchDetails MatchInfo { get; } = new();
    private static Guid Matchid { get; set; }
    private static Guid Partyid { get; set; }
    private static string Stage { get; set; }
    public string QueueId { get; set; }
    public string Status { get; set; }

    private static async Task<bool> CheckAndSetLiveMatchIdAsync()
    {
        var client = new RestClient(
            $"https://glz-{Constants.Shard}-1.{Constants.Region}.a.pvp.net/core-game/v1/players/{Constants.Ppuuid}"
        );
        var request = new RestRequest();
        request.AddHeader("X-Riot-Entitlements-JWT", Constants.EntitlementToken);
        request.AddHeader("Authorization", $"Bearer {Constants.AccessToken}");
        request.AddHeader("X-Riot-ClientPlatform", Constants.Platform);
        request.AddHeader("X-Riot-ClientVersion", Constants.Version);
        var response = await client.ExecuteGetAsync<MatchIDResponse>(request).ConfigureAwait(false);
        if (response.IsSuccessful)
        {
            Matchid = response.Data.MatchId;
            Stage = "core";
            return true;
        }

        client = new RestClient(
            $"https://glz-{Constants.Shard}-1.{Constants.Region}.a.pvp.net/pregame/v1/players/{Constants.Ppuuid}"
        );
        response = await client.ExecuteGetAsync<MatchIDResponse>(request).ConfigureAwait(false);
        if (response.IsSuccessful)
        {
            Matchid = response.Data.MatchId;
            Stage = "pre";
            return true;
        }

        Constants.Log.Error(
            "CheckAndSetLiveMatchIdAsync() failed. Response: {Response}",
            response.ErrorException
        );
        return false;
    }

    public async Task<bool> CheckAndSetPartyIdAsync()
    {
        var client = new RestClient(
            $"https://glz-{Constants.Shard}-1.{Constants.Region}.a.pvp.net/parties/v1/players/{Constants.Ppuuid}"
        );
        var request = new RestRequest();
        request.AddHeader("X-Riot-Entitlements-JWT", Constants.EntitlementToken);
        request.AddHeader("Authorization", $"Bearer {Constants.AccessToken}");
        request.AddHeader("X-Riot-ClientPlatform", Constants.Platform);
        request.AddHeader("X-Riot-ClientVersion", Constants.Version);
        var response = await client.ExecuteGetAsync<PartyIdResponse>(request).ConfigureAwait(false);
        if (!response.IsSuccessful)
            return false;
        Partyid = response.Data.CurrentPartyId;
        return true;
    }

    public static async Task<bool> LiveMatchChecksAsync()
    {
        if (await Checks.CheckLoginAsync().ConfigureAwait(false))
        {
            await LocalRegionAsync().ConfigureAwait(false);
            return await CheckAndSetLiveMatchIdAsync().ConfigureAwait(false);
        }

        if (!await Checks.CheckLocalAsync().ConfigureAwait(false))
            return false;
        await LocalLoginAsync().ConfigureAwait(false);
        await Checks.CheckLoginAsync().ConfigureAwait(false);
        await LocalRegionAsync().ConfigureAwait(false);

        return await CheckAndSetLiveMatchIdAsync().ConfigureAwait(false);
    }

    private static async Task<LiveMatchResponse> GetLiveMatchDetailsAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(Constants.Shard) || string.IsNullOrEmpty(Constants.Region))
            {
                // Not logged in yet or region/shard not set
                return null;
            }

            RestClient client =
                new(
                    $"https://glz-{Constants.Shard}-1.{Constants.Region}.a.pvp.net"
                );
            RestRequest request = new($"/core-game/v1/matches/{Matchid}");
            request.AddHeader("X-Riot-Entitlements-JWT", Constants.EntitlementToken);
            request.AddHeader("Authorization", $"Bearer {Constants.AccessToken}");
            request.AddHeader("X-Riot-ClientPlatform", Constants.Platform);
            request.AddHeader("X-Riot-ClientVersion", Constants.Version);
            var response = await client
                .ExecuteGetAsync<LiveMatchResponse>(request)
                .ConfigureAwait(false);
            if (response.IsSuccessful)
                return response.Data;
            Constants.Log.Error(
                "GetLiveMatchDetailsAsync() failed. Status: {Status}, Error: {Error}",
                response.StatusCode,
                response.ErrorMessage
            );
        }
        catch (Exception ex)
        {
            Constants.Log.Error("GetLiveMatchDetailsAsync Exception: {Error}", ex.Message);
        }
        return null;
    }

    private static async Task<PreMatchResponse> GetPreMatchDetailsAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(Constants.Shard) || string.IsNullOrEmpty(Constants.Region))
            {
                // Not logged in yet or region/shard not set
                return null;
            }

            RestClient client =
                new(
                    $"https://glz-{Constants.Shard}-1.{Constants.Region}.a.pvp.net"
                );
            RestRequest request = new($"/pregame/v1/matches/{Matchid}");
            request.AddHeader("X-Riot-Entitlements-JWT", Constants.EntitlementToken);
            request.AddHeader("Authorization", $"Bearer {Constants.AccessToken}");
            request.AddHeader("X-Riot-ClientPlatform", Constants.Platform);
            request.AddHeader("X-Riot-ClientVersion", Constants.Version);
            var response = await client
                .ExecuteGetAsync<PreMatchResponse>(request)
                .ConfigureAwait(false);
            if (response.IsSuccessful)
                return response.Data;
            Constants.Log.Error(
                "GetPreMatchDetailsAsync() failed. Status: {Status}, Error: {Error}",
                response.StatusCode,
                response.ErrorMessage
            );
        }
        catch (Exception ex)
        {
            Constants.Log.Error("GetPreMatchDetailsAsync Exception: {Error}", ex.Message);
        }
        return null;
    }

    private static async Task<PartyResponse> GetPartyDetailsAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(Constants.Shard) || string.IsNullOrEmpty(Constants.Region))
            {
                // Not logged in yet or region/shard not set
                return null;
            }

            RestClient client =
                new(
                    $"https://glz-{Constants.Shard}-1.{Constants.Region}.a.pvp.net/parties/v1/parties/{Partyid}"
                );
            RestRequest request = new();
            request.AddHeader("X-Riot-Entitlements-JWT", Constants.EntitlementToken);
            request.AddHeader("Authorization", $"Bearer {Constants.AccessToken}");
            request.AddHeader("X-Riot-ClientPlatform", Constants.Platform);
            request.AddHeader("X-Riot-ClientVersion", Constants.Version);
            var response = await client.ExecuteGetAsync<PartyResponse>(request).ConfigureAwait(false);
            if (response.IsSuccessful)
                return response.Data;
            Constants.Log.Error(
                "GetPartyDetailsAsync() failed. Response: {Response}",
                response.ErrorException
            );
        }
        catch (Exception ex)
        {
            Constants.Log.Error("GetPartyDetailsAsync Exception: {Error}", ex.Message);
        }
        return null;
    }

    private async Task<Player> GetPrePlayerInfo(
        RiotPrePlayer riotPlayer,
        sbyte index,
        Guid[] seasonData,
        PresencesResponse presencesResponse
    )
    {
        Player player = new();
        try
        {
            var cardTask = GetCardAsync(riotPlayer.PlayerIdentity.PlayerCardId, index);
            var historyTask = GetPlayerHistoryAsync(riotPlayer.Subject, seasonData);
            var presenceTask = GetPresenceInfoAsync(riotPlayer.Subject, presencesResponse);
            
            Task<SkinData> skinTask;
            if (riotPlayer.Subject == Constants.Ppuuid)
            {
                skinTask = GetPartySkinInfoAsync(riotPlayer.Subject, riotPlayer.PlayerIdentity.PlayerCardId);
            }
            else
            {
                skinTask = GetPreSkinInfoAsync(index, riotPlayer.PlayerIdentity.PlayerCardId);
            }

            await Task.WhenAll(cardTask, historyTask, presenceTask, skinTask).ConfigureAwait(false);

            player.IdentityData = cardTask.Result;
            player.RankData = historyTask.Result;
            player.PlayerUiData = presenceTask.Result;
            player.SkinData = skinTask.Result;
            player.IgnData = await GetIgcUsernameAsync(
                    riotPlayer.Subject,
                    riotPlayer.PlayerIdentity.Incognito,
                    false
                )
                .ConfigureAwait(false);
            player.AccountLevel = !riotPlayer.PlayerIdentity.HideAccountLevel
                ? riotPlayer.PlayerIdentity.AccountLevel.ToString()
                : "-";
            player.TeamId = "Blue";
            player.Active = Visibility.Visible;
        }
        catch (Exception e)
        {
            Constants.Log.Error("GetPlayerInfo() (PRE) failed for player {index}: {e}", index, e);
        }

        return player;
    }

    private async Task<Player> GetLivePlayerInfo(
        RiotLivePlayer riotPlayer,
        sbyte index,
        Guid[] seasonData,
        PresencesResponse presencesResponse
    )
    {
        Player player = new();
        try
        {
            var agentTask = GetAgentInfoAsync(riotPlayer.CharacterId);
            var playerTask = GetPlayerHistoryAsync(riotPlayer.Subject, seasonData);
            var presenceTask = GetPresenceInfoAsync(riotPlayer.Subject, presencesResponse);
            
            Task<SkinData> skinTask;
            if (riotPlayer.Subject == Constants.Ppuuid)
            {
                skinTask = GetPartySkinInfoAsync(riotPlayer.Subject, riotPlayer.PlayerIdentity.PlayerCardId);
            }
            else
            {
                skinTask = GetMatchSkinInfoAsync(index, riotPlayer.PlayerIdentity.PlayerCardId);
            }

            await Task.WhenAll(agentTask, playerTask, skinTask, presenceTask).ConfigureAwait(false);

            player.IdentityData = agentTask.Result;
            player.RankData = playerTask.Result;
            player.SkinData = skinTask.Result;
            player.PlayerUiData = presenceTask.Result;
            player.IgnData = await GetIgcUsernameAsync(
                    riotPlayer.Subject,
                    riotPlayer.PlayerIdentity.Incognito,
                    false
                )
                .ConfigureAwait(false);
            player.AccountLevel = !riotPlayer.PlayerIdentity.HideAccountLevel
                ? riotPlayer.PlayerIdentity.AccountLevel.ToString()
                : "-";
            player.TeamId = riotPlayer.TeamId;
            player.Active = Visibility.Visible;
        }
        catch (Exception e)
        {
            Constants.Log.Error("GetPlayerInfo() (LIVE) failed for player {index}: {e}", index, e);
        }

        return player;
    }

    private async Task GetPrePlayers(
        List<Task<Player>> playerTasks,
        PreMatchResponse matchIdInfo,
        Guid[] seasonData,
        PresencesResponse presencesResponse
    )
    {
        Task sTask = Task.Run(
            async () => seasonData = await GetSeasonsAsync().ConfigureAwait(false)
        );
        Task pTask = Task.Run(
            async () => presencesResponse = await GetPresencesAsync().ConfigureAwait(false)
        );
        await Task.WhenAll(sTask, pTask).ConfigureAwait(false);
        sbyte index = 0;

        foreach (var riotPlayer in matchIdInfo.AllyTeam.Players)
        {
            playerTasks.Add(GetPrePlayerInfo(riotPlayer, index, seasonData, presencesResponse));
            index++;
        }
    }

    private async Task GetLivePlayers(
        List<Task<Player>> playerTasks,
        LiveMatchResponse matchIdInfo,
        Guid[] seasonData,
        PresencesResponse presencesResponse
    )
    {
        Task sTask = Task.Run(
            async () => seasonData = await GetSeasonsAsync().ConfigureAwait(false)
        );
        Task pTask = Task.Run(
            async () => presencesResponse = await GetPresencesAsync().ConfigureAwait(false)
        );
        await Task.WhenAll(sTask, pTask).ConfigureAwait(false);
        sbyte index = 0;

        foreach (var riotPlayer in matchIdInfo.Players)
        {
            if (riotPlayer.IsCoach)
                continue;

            playerTasks.Add(GetLivePlayerInfo(riotPlayer, index, seasonData, presencesResponse));

            index++;
        }
    }

    public static async Task<dynamic> GetMatchResponseAsync()
    {
        if (Stage == "pre")
        {
            return await GetPreMatchDetailsAsync().ConfigureAwait(false);
        }
        return await GetLiveMatchDetailsAsync().ConfigureAwait(false);
    }

    private async Task GetPlayers(
        UpdateProgress updateProgress,
        List<Task<Player>> playerTasks,
        Guid[] seasonData,
        PresencesResponse presencesResponse
    )
    {
        var matchIdInfo = await GetMatchResponseAsync();
        updateProgress(10);

        if (matchIdInfo == null)
            return;

        if (Stage == "pre")
        {
            await GetPrePlayers(playerTasks, matchIdInfo, seasonData, presencesResponse);
            return;
        }
        await GetLivePlayers(playerTasks, matchIdInfo, seasonData, presencesResponse);
    }

    private void AddPlayerParties(List<Player> playerList)
    {
        var playerPartyColors = new List<string>
        {
            "Red",
            "#32e2b2",
            "DarkOrange",
            "White",
            "DeepSkyBlue",
            "MediumPurple",
            "SaddleBrown"
        };
        List<string> newArray = new();
        newArray.AddRange(Enumerable.Repeat("Transparent", playerList.Count));

        for (var i = 0; i < playerList.Count; i++)
        {
            if (playerList[i].PlayerUiData is null)
                continue;

            var colourused = false;
            var id = playerList[i].PlayerUiData.PartyUuid;
            for (var j = i; j < playerList.Count; j++)
            {
                if (
                    newArray[i] != "Transparent"
                    || playerList[i] == playerList[j]
                    || playerList[j].PlayerUiData?.PartyUuid != id
                    || id == Guid.Empty
                )
                    continue;
                newArray[j] = playerPartyColors[0];
                colourused = true;
            }

            if (!colourused)
                continue;
            newArray[i] = playerPartyColors[0];
            playerPartyColors.RemoveAt(0);
        }
        for (var i = 0; i < playerList.Count; i++)
            playerList[i].PlayerUiData.PartyColour = newArray[i];
    }

    public async Task<List<Player>> LiveMatchOutputAsync(UpdateProgress updateProgress)
    {
        var playerList = new List<Player>();
        var playerTasks = new List<Task<Player>>();
        var seasonData = new Guid[4];
        var presencesResponse = new PresencesResponse();

        await GetPlayers(updateProgress, playerTasks, seasonData, presencesResponse);

        playerList.AddRange(await Task.WhenAll(playerTasks).ConfigureAwait(false));
        updateProgress(75);

        try
        {
            AddPlayerParties(playerList);
            updateProgress(100);
        }
        catch (Exception e)
        {
            Constants.Log.Error("LiveMatchOutputAsync() party colour failed: {e}", e);
        }

        return playerList;
    }

    private async Task<Player> GetPartyPlayerInfo(Member riotPlayer, sbyte index, Guid[] seasonData)
    {
        Player player = new();

        var cardTask = GetCardAsync(riotPlayer.PlayerIdentity.PlayerCardId, index);
        var historyTask = GetMatchHistoryAsync(riotPlayer.Subject);
        var playerTask = GetPlayerHistoryAsync(riotPlayer.Subject, seasonData);

        Task<SkinData> skinTask = null;
        if (riotPlayer.Subject == Constants.Ppuuid)
        {
            skinTask = GetPartySkinInfoAsync(riotPlayer.Subject, riotPlayer.PlayerIdentity.PlayerCardId);
        }

        if (skinTask != null)
        {
            await Task.WhenAll(cardTask, historyTask, playerTask, skinTask).ConfigureAwait(false);
            player.SkinData = skinTask.Result;
        }
        else
        {
            await Task.WhenAll(cardTask, historyTask, playerTask).ConfigureAwait(false);
        }

        player.IdentityData = cardTask.Result;
        player.MatchHistoryData = historyTask.Result;
        player.RankData = playerTask.Result;
        player.PlayerUiData = new PlayerUIData
        {
            BackgroundColour = "#252A40",
            PartyUuid = Partyid,
            PartyColour = "Transparent",
            Puuid = riotPlayer.PlayerIdentity.Subject
        };
        player.IgnData = await GetIgcUsernameAsync(riotPlayer.Subject, false, true)
            .ConfigureAwait(false);
        player.AccountLevel = !riotPlayer.PlayerIdentity.HideAccountLevel
            ? riotPlayer.PlayerIdentity.AccountLevel.ToString()
            : "-";
        player.TeamId = "Blue";
        player.Active = Visibility.Visible;
        return player;
    }


    private async Task GetPartyPlayers(PartyResponse partyInfo, List<Task<Player>> playerTasks)
    {
        if (partyInfo == null)
            return;

        var seasonData = await GetSeasonsAsync().ConfigureAwait(false);
        sbyte index = 0;

        foreach (var riotPlayer in partyInfo.Members)
        {
            playerTasks.Add(GetPartyPlayerInfo(riotPlayer, index, seasonData));
            index++;
        }
    }

    public async Task<List<Player>> PartyOutputAsync()
    {
        var playerList = new List<Player>();
        var playerTasks = new List<Task<Player>>();
        var partyInfo = await GetPartyDetailsAsync().ConfigureAwait(false);

        await GetPartyPlayers(partyInfo, playerTasks);

        playerList.AddRange(await Task.WhenAll(playerTasks).ConfigureAwait(false));

        return playerList;
    }

    private static async Task<IgnData> GetIgcUsernameAsync(
        Guid puuid,
        bool isIncognito,
        bool inParty
    )
    {
        IgnData ignData = new();
        ignData.TrackerEnabled = Visibility.Hidden;
        ignData.TrackerDisabled = Visibility.Visible;

        if (isIncognito && !inParty)
        {
            ignData.Username = "----";
            return ignData;
        }

        ignData.Username = await GetNameServiceGetUsernameAsync(puuid).ConfigureAwait(false);

        var trackerUri = await TrackerAsync(ignData.Username).ConfigureAwait(false);

        if (trackerUri != null)
        {
            ignData.TrackerEnabled = Visibility.Visible;
            ignData.TrackerDisabled = Visibility.Collapsed;
            ignData.TrackerUri = trackerUri;
            ignData.Username = ignData.Username + " 🔗";
        }

        return ignData;
    }

    private static async Task<IdentityData> GetAgentInfoAsync(Guid agentid)
    {
        IdentityData identityData = new();

        if (agentid == Guid.Empty)
        {
            Constants.Log.Error("GetAgentInfoAsync Failed: AgentID is empty");
            identityData.Image = null;
            identityData.Name = "";
            return identityData;
        }

        identityData.Image = new Uri(
            Constants.LocalAppDataPath + $"\\ValAPI\\agentsimg\\{agentid}.png"
        );
        var agents = JsonSerializer.Deserialize<Dictionary<Guid, string>>(
            await File.ReadAllTextAsync(Constants.LocalAppDataPath + "\\ValAPI\\agents.json")
                .ConfigureAwait(false)
        );
        agents.TryGetValue(agentid, out var agentName);
        identityData.Name = agentName;
        return identityData;
    }

    private static async Task<IdentityData> GetCardAsync(Guid cardid, sbyte index)
    {
        if (cardid != Guid.Empty)
        {
            var cards = JsonSerializer.Deserialize<Dictionary<Guid, ValNameImage>>(
                await File.ReadAllTextAsync(Constants.LocalAppDataPath + "\\ValAPI\\cards.json")
                    .ConfigureAwait(false)
            );
            cards.TryGetValue(cardid, out var card);
            return new IdentityData
            {
                Image = card.Image,
                Name = Resources.Player + " " + (index + 1)
            };
        }

        Constants.Log.Error("GetCardAsync Failed: CardID is empty");
        return new IdentityData();
    }

    private static async Task<SkinData> GetMatchSkinInfoAsync(sbyte playerno, Guid cardid)
    {
        var response = await DoCachedRequestAsync(
                Method.Get,
                $"https://glz-{Constants.Shard}-1.{Constants.Region}.a.pvp.net/core-game/v1/matches/{Matchid}/loadouts",
                true
            )
            .ConfigureAwait(false);
        
        Constants.Log.Information("GetMatchSkinInfoAsync: Player={Player}, IsSuccessful={IsSuccessful}", playerno, response.IsSuccessful);
        
        if (response.IsSuccessful)
        {
            var content = JsonSerializer.Deserialize<MatchLoadoutsResponse>(response.Content);
            var loadout = content.Loadouts[playerno].Loadout;
            var skinData = await GetSkinInfoAsync(loadout, cardid).ConfigureAwait(false);

            if (loadout.Identity != null)
            {
                skinData.PlayerTitle = await BuddyTitleHelper.GetPlayerTitleAsync(loadout.Identity.PlayerTitleId).ConfigureAwait(false);
                Constants.Log.Information("GetMatchSkinInfoAsync: Player={Player}, Title={Title}", playerno, skinData.PlayerTitle);

                var buddyTasks = new Dictionary<string, Task<ValNameImage>>
                {
                    ["29a0cfab-485b-f5d5-779a-b59f85e204a8"] = GetBuddyFromMatchLoadout(loadout.Items, "29a0cfab-485b-f5d5-779a-b59f85e204a8"),
                    ["42da8ccc-40d5-affc-beec-15aa47b42eda"] = GetBuddyFromMatchLoadout(loadout.Items, "42da8ccc-40d5-affc-beec-15aa47b42eda"),
                    ["44d4e95c-4157-0037-81b2-17841bf2e8e3"] = GetBuddyFromMatchLoadout(loadout.Items, "44d4e95c-4157-0037-81b2-17841bf2e8e3"),
                    ["1baa85b4-4c70-1284-64bb-6481dfc3bb4e"] = GetBuddyFromMatchLoadout(loadout.Items, "1baa85b4-4c70-1284-64bb-6481dfc3bb4e"),
                    ["410b2e0b-4ceb-1321-1727-20858f7f3477"] = GetBuddyFromMatchLoadout(loadout.Items, "410b2e0b-4ceb-1321-1727-20858f7f3477"),
                    ["e336c6b8-418d-9340-d77f-7a9e4cfe0702"] = GetBuddyFromMatchLoadout(loadout.Items, "e336c6b8-418d-9340-d77f-7a9e4cfe0702"),
                    ["f7e1b454-4ad4-1063-ec0a-159e56b58941"] = GetBuddyFromMatchLoadout(loadout.Items, "f7e1b454-4ad4-1063-ec0a-159e56b58941"),
                    ["462080d1-4035-2937-7c09-27aa2a5c27a7"] = GetBuddyFromMatchLoadout(loadout.Items, "462080d1-4035-2937-7c09-27aa2a5c27a7"),
                    ["910be174-449b-c412-ab22-d0873436b21b"] = GetBuddyFromMatchLoadout(loadout.Items, "910be174-449b-c412-ab22-d0873436b21b"),
                    ["ec845bf4-4f79-ddda-a3da-0db3774b2794"] = GetBuddyFromMatchLoadout(loadout.Items, "ec845bf4-4f79-ddda-a3da-0db3774b2794"),
                    ["ae3de142-4d85-2547-dd26-4e90bed35cf7"] = GetBuddyFromMatchLoadout(loadout.Items, "ae3de142-4d85-2547-dd26-4e90bed35cf7"),
                    ["4ade7faa-4cf1-8376-95ef-39884480959b"] = GetBuddyFromMatchLoadout(loadout.Items, "4ade7faa-4cf1-8376-95ef-39884480959b"),
                    ["ee8e8d15-496b-07ac-e5f6-8fae5d4c7b1a"] = GetBuddyFromMatchLoadout(loadout.Items, "ee8e8d15-496b-07ac-e5f6-8fae5d4c7b1a"),
                    ["9c82e19d-4575-0200-1a81-3eacf00cf872"] = GetBuddyFromMatchLoadout(loadout.Items, "9c82e19d-4575-0200-1a81-3eacf00cf872"),
                    ["c4883e50-4494-202c-3ec3-6b8a9284f00b"] = GetBuddyFromMatchLoadout(loadout.Items, "c4883e50-4494-202c-3ec3-6b8a9284f00b"),
                    ["5f0aaf7a-4289-3998-d5ff-eb9a5cf7ef5c"] = GetBuddyFromMatchLoadout(loadout.Items, "5f0aaf7a-4289-3998-d5ff-eb9a5cf7ef5c"),
                    ["a03b24d3-4319-996d-0f8c-94bbfba1dfc7"] = GetBuddyFromMatchLoadout(loadout.Items, "a03b24d3-4319-996d-0f8c-94bbfba1dfc7"),
                    ["55d8a0f4-4274-ca67-fe2c-06ab45efdf58"] = GetBuddyFromMatchLoadout(loadout.Items, "55d8a0f4-4274-ca67-fe2c-06ab45efdf58"),
                    ["63e6c2b6-4a8e-869c-3d4c-e38355226584"] = GetBuddyFromMatchLoadout(loadout.Items, "63e6c2b6-4a8e-869c-3d4c-e38355226584"),
                    ["2f59173c-4bed-b6c3-2191-dea9b58be9c7"] = GetBuddyFromMatchLoadout(loadout.Items, "2f59173c-4bed-b6c3-2191-dea9b58be9c7")
                };

                await Task.WhenAll(buddyTasks.Values).ConfigureAwait(false);

                skinData.ClassicBuddyImage = buddyTasks["29a0cfab-485b-f5d5-779a-b59f85e204a8"].Result.Image;
                skinData.ClassicBuddyName = buddyTasks["29a0cfab-485b-f5d5-779a-b59f85e204a8"].Result.Name;
                skinData.ShortyBuddyImage = buddyTasks["42da8ccc-40d5-affc-beec-15aa47b42eda"].Result.Image;
                skinData.ShortyBuddyName = buddyTasks["42da8ccc-40d5-affc-beec-15aa47b42eda"].Result.Name;
                skinData.FrenzyBuddyImage = buddyTasks["44d4e95c-4157-0037-81b2-17841bf2e8e3"].Result.Image;
                skinData.FrenzyBuddyName = buddyTasks["44d4e95c-4157-0037-81b2-17841bf2e8e3"].Result.Name;
                skinData.GhostBuddyImage = buddyTasks["1baa85b4-4c70-1284-64bb-6481dfc3bb4e"].Result.Image;
                skinData.GhostBuddyName = buddyTasks["1baa85b4-4c70-1284-64bb-6481dfc3bb4e"].Result.Name;
                skinData.BanditBuddyImage = buddyTasks["410b2e0b-4ceb-1321-1727-20858f7f3477"].Result.Image;
                skinData.BanditBuddyName = buddyTasks["410b2e0b-4ceb-1321-1727-20858f7f3477"].Result.Name;
                skinData.SheriffBuddyImage = buddyTasks["e336c6b8-418d-9340-d77f-7a9e4cfe0702"].Result.Image;
                skinData.SheriffBuddyName = buddyTasks["e336c6b8-418d-9340-d77f-7a9e4cfe0702"].Result.Name;
                skinData.StingerBuddyImage = buddyTasks["f7e1b454-4ad4-1063-ec0a-159e56b58941"].Result.Image;
                skinData.StingerBuddyName = buddyTasks["f7e1b454-4ad4-1063-ec0a-159e56b58941"].Result.Name;
                skinData.SpectreBuddyImage = buddyTasks["462080d1-4035-2937-7c09-27aa2a5c27a7"].Result.Image;
                skinData.SpectreBuddyName = buddyTasks["462080d1-4035-2937-7c09-27aa2a5c27a7"].Result.Name;
                skinData.BuckyBuddyImage = buddyTasks["910be174-449b-c412-ab22-d0873436b21b"].Result.Image;
                skinData.BuckyBuddyName = buddyTasks["910be174-449b-c412-ab22-d0873436b21b"].Result.Name;
                skinData.JudgeBuddyImage = buddyTasks["ec845bf4-4f79-ddda-a3da-0db3774b2794"].Result.Image;
                skinData.JudgeBuddyName = buddyTasks["ec845bf4-4f79-ddda-a3da-0db3774b2794"].Result.Name;
                skinData.BulldogBuddyImage = buddyTasks["ae3de142-4d85-2547-dd26-4e90bed35cf7"].Result.Image;
                skinData.BulldogBuddyName = buddyTasks["ae3de142-4d85-2547-dd26-4e90bed35cf7"].Result.Name;
                skinData.GuardianBuddyImage = buddyTasks["4ade7faa-4cf1-8376-95ef-39884480959b"].Result.Image;
                skinData.GuardianBuddyName = buddyTasks["4ade7faa-4cf1-8376-95ef-39884480959b"].Result.Name;
                skinData.PhantomBuddyImage = buddyTasks["ee8e8d15-496b-07ac-e5f6-8fae5d4c7b1a"].Result.Image;
                skinData.PhantomBuddyName = buddyTasks["ee8e8d15-496b-07ac-e5f6-8fae5d4c7b1a"].Result.Name;
                skinData.VandalBuddyImage = buddyTasks["9c82e19d-4575-0200-1a81-3eacf00cf872"].Result.Image;
                skinData.VandalBuddyName = buddyTasks["9c82e19d-4575-0200-1a81-3eacf00cf872"].Result.Name;
                skinData.MarshalBuddyImage = buddyTasks["c4883e50-4494-202c-3ec3-6b8a9284f00b"].Result.Image;
                skinData.MarshalBuddyName = buddyTasks["c4883e50-4494-202c-3ec3-6b8a9284f00b"].Result.Name;
                skinData.OutlawBuddyImage = buddyTasks["5f0aaf7a-4289-3998-d5ff-eb9a5cf7ef5c"].Result.Image;
                skinData.OutlawBuddyName = buddyTasks["5f0aaf7a-4289-3998-d5ff-eb9a5cf7ef5c"].Result.Name;
                skinData.OperatorBuddyImage = buddyTasks["a03b24d3-4319-996d-0f8c-94bbfba1dfc7"].Result.Image;
                skinData.OperatorBuddyName = buddyTasks["a03b24d3-4319-996d-0f8c-94bbfba1dfc7"].Result.Name;
                skinData.AresBuddyImage = buddyTasks["55d8a0f4-4274-ca67-fe2c-06ab45efdf58"].Result.Image;
                skinData.AresBuddyName = buddyTasks["55d8a0f4-4274-ca67-fe2c-06ab45efdf58"].Result.Name;
                skinData.OdinBuddyImage = buddyTasks["63e6c2b6-4a8e-869c-3d4c-e38355226584"].Result.Image;
                skinData.OdinBuddyName = buddyTasks["63e6c2b6-4a8e-869c-3d4c-e38355226584"].Result.Name;
                skinData.MeleeBuddyImage = buddyTasks["2f59173c-4bed-b6c3-2191-dea9b58be9c7"].Result.Image;
                skinData.MeleeBuddyName = buddyTasks["2f59173c-4bed-b6c3-2191-dea9b58be9c7"].Result.Name;
            }

            return skinData;
        }

        Constants.Log.Error("GetMatchSkinInfoAsync Failed: {e}", response.ErrorException);
        return new SkinData();
    }

    private static async Task<ValNameImage> GetBuddyFromMatchLoadout(Dictionary<string, ItemValue> items, string weaponId)
    {
        if (items.TryGetValue(weaponId, out var weapon))
        {
            if (weapon.Sockets != null && weapon.Sockets.TryGetValue("bcef87d6-209b-46c6-8b19-fbe40bd95abc", out var charmSocket))
            {
                return await BuddyTitleHelper.GetBuddyInfoAsync(charmSocket.Item.Id).ConfigureAwait(false);
            }
        }
        return new ValNameImage();
    }

    private static async Task<SkinData> GetPreSkinInfoAsync(sbyte playerno, Guid cardid)
    {
        var response = await DoCachedRequestAsync(
                Method.Get,
                $"https://glz-{Constants.Shard}-1.{Constants.Region}.a.pvp.net/pregame/v1/matches/{Matchid}/loadouts",
                true
            )
            .ConfigureAwait(false);
        
        Constants.Log.Information("GetPreSkinInfoAsync: Player={Player}, IsSuccessful={IsSuccessful}", playerno, response.IsSuccessful);
        
        if (response.IsSuccessful)
            try
            {
                var content = JsonSerializer.Deserialize<PreMatchLoadoutsExtendedResponse>(
                    response.Content
                );

                if (content?.Loadouts == null || playerno >= content.Loadouts.Length)
                {
                    Constants.Log.Warning("GetPreSkinInfoAsync: Invalid loadouts. Player={Player}, Count={Count}", playerno, content?.Loadouts?.Length ?? 0);
                    return new SkinData();
                }

                var playerLoadout = content.Loadouts[playerno];
                if (playerLoadout?.Loadout == null)
                {
                    Constants.Log.Warning("GetPreSkinInfoAsync: Loadout is null. Player={Player}", playerno);
                    return new SkinData();
                }

                var loadout = new LoadoutLoadout
                {
                    Sprays = new Sprays
                    {
                        SpraySelections = playerLoadout.Loadout.Sprays.SpraySelections.Select(s => new SpraySelection
                        {
                            AssetId = s.SprayId,
                            TypeId = s.SocketId
                        }).ToArray()
                    },
                    Items = playerLoadout.Loadout.Items.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new ItemValue
                        {
                            Id = kvp.Value.Id,
                            TypeId = kvp.Value.TypeId,
                            Sockets = kvp.Value.Sockets.ToDictionary(
                                s => s.Key,
                                s => new Socket
                                {
                                    Id = s.Value.Id,
                                    Item = new SocketItem
                                    {
                                        Id = s.Value.Item.Id,
                                        TypeId = s.Value.Item.TypeId
                                    }
                                }
                            )
                        }
                    )
                };

                var skinData = await GetSkinInfoAsync(loadout, cardid).ConfigureAwait(false);

                if (playerLoadout.Loadout.Identity != null)
                {
                    skinData.PlayerTitle = await BuddyTitleHelper.GetPlayerTitleAsync(playerLoadout.Loadout.Identity.PlayerTitleId).ConfigureAwait(false);

                    var buddyTasks = new Dictionary<string, Task<ValNameImage>>
                {
                    ["29a0cfab-485b-f5d5-779a-b59f85e204a8"] = GetBuddyFromLoadout(playerLoadout.Loadout.Items, "29a0cfab-485b-f5d5-779a-b59f85e204a8"),
                    ["42da8ccc-40d5-affc-beec-15aa47b42eda"] = GetBuddyFromLoadout(playerLoadout.Loadout.Items, "42da8ccc-40d5-affc-beec-15aa47b42eda"),
                    ["44d4e95c-4157-0037-81b2-17841bf2e8e3"] = GetBuddyFromLoadout(playerLoadout.Loadout.Items, "44d4e95c-4157-0037-81b2-17841bf2e8e3"),
                    ["1baa85b4-4c70-1284-64bb-6481dfc3bb4e"] = GetBuddyFromLoadout(playerLoadout.Loadout.Items, "1baa85b4-4c70-1284-64bb-6481dfc3bb4e"),
                    ["410b2e0b-4ceb-1321-1727-20858f7f3477"] = GetBuddyFromLoadout(playerLoadout.Loadout.Items, "410b2e0b-4ceb-1321-1727-20858f7f3477"),
                    ["e336c6b8-418d-9340-d77f-7a9e4cfe0702"] = GetBuddyFromLoadout(playerLoadout.Loadout.Items, "e336c6b8-418d-9340-d77f-7a9e4cfe0702"),
                    ["f7e1b454-4ad4-1063-ec0a-159e56b58941"] = GetBuddyFromLoadout(playerLoadout.Loadout.Items, "f7e1b454-4ad4-1063-ec0a-159e56b58941"),
                    ["462080d1-4035-2937-7c09-27aa2a5c27a7"] = GetBuddyFromLoadout(playerLoadout.Loadout.Items, "462080d1-4035-2937-7c09-27aa2a5c27a7"),
                    ["910be174-449b-c412-ab22-d0873436b21b"] = GetBuddyFromLoadout(playerLoadout.Loadout.Items, "910be174-449b-c412-ab22-d0873436b21b"),
                    ["ec845bf4-4f79-ddda-a3da-0db3774b2794"] = GetBuddyFromLoadout(playerLoadout.Loadout.Items, "ec845bf4-4f79-ddda-a3da-0db3774b2794"),
                    ["ae3de142-4d85-2547-dd26-4e90bed35cf7"] = GetBuddyFromLoadout(playerLoadout.Loadout.Items, "ae3de142-4d85-2547-dd26-4e90bed35cf7"),
                    ["4ade7faa-4cf1-8376-95ef-39884480959b"] = GetBuddyFromLoadout(playerLoadout.Loadout.Items, "4ade7faa-4cf1-8376-95ef-39884480959b"),
                    ["ee8e8d15-496b-07ac-e5f6-8fae5d4c7b1a"] = GetBuddyFromLoadout(playerLoadout.Loadout.Items, "ee8e8d15-496b-07ac-e5f6-8fae5d4c7b1a"),
                    ["9c82e19d-4575-0200-1a81-3eacf00cf872"] = GetBuddyFromLoadout(playerLoadout.Loadout.Items, "9c82e19d-4575-0200-1a81-3eacf00cf872"),
                    ["c4883e50-4494-202c-3ec3-6b8a9284f00b"] = GetBuddyFromLoadout(playerLoadout.Loadout.Items, "c4883e50-4494-202c-3ec3-6b8a9284f00b"),
                    ["5f0aaf7a-4289-3998-d5ff-eb9a5cf7ef5c"] = GetBuddyFromLoadout(playerLoadout.Loadout.Items, "5f0aaf7a-4289-3998-d5ff-eb9a5cf7ef5c"),
                    ["a03b24d3-4319-996d-0f8c-94bbfba1dfc7"] = GetBuddyFromLoadout(playerLoadout.Loadout.Items, "a03b24d3-4319-996d-0f8c-94bbfba1dfc7"),
                    ["55d8a0f4-4274-ca67-fe2c-06ab45efdf58"] = GetBuddyFromLoadout(playerLoadout.Loadout.Items, "55d8a0f4-4274-ca67-fe2c-06ab45efdf58"),
                    ["63e6c2b6-4a8e-869c-3d4c-e38355226584"] = GetBuddyFromLoadout(playerLoadout.Loadout.Items, "63e6c2b6-4a8e-869c-3d4c-e38355226584"),
                    ["2f59173c-4bed-b6c3-2191-dea9b58be9c7"] = GetBuddyFromLoadout(playerLoadout.Loadout.Items, "2f59173c-4bed-b6c3-2191-dea9b58be9c7")
                };

                await Task.WhenAll(buddyTasks.Values).ConfigureAwait(false);

                skinData.ClassicBuddyImage = buddyTasks["29a0cfab-485b-f5d5-779a-b59f85e204a8"].Result.Image;
                skinData.ClassicBuddyName = buddyTasks["29a0cfab-485b-f5d5-779a-b59f85e204a8"].Result.Name;
                skinData.ShortyBuddyImage = buddyTasks["42da8ccc-40d5-affc-beec-15aa47b42eda"].Result.Image;
                skinData.ShortyBuddyName = buddyTasks["42da8ccc-40d5-affc-beec-15aa47b42eda"].Result.Name;
                skinData.FrenzyBuddyImage = buddyTasks["44d4e95c-4157-0037-81b2-17841bf2e8e3"].Result.Image;
                skinData.FrenzyBuddyName = buddyTasks["44d4e95c-4157-0037-81b2-17841bf2e8e3"].Result.Name;
                skinData.GhostBuddyImage = buddyTasks["1baa85b4-4c70-1284-64bb-6481dfc3bb4e"].Result.Image;
                skinData.GhostBuddyName = buddyTasks["1baa85b4-4c70-1284-64bb-6481dfc3bb4e"].Result.Name;
                skinData.BanditBuddyImage = buddyTasks["410b2e0b-4ceb-1321-1727-20858f7f3477"].Result.Image;
                skinData.BanditBuddyName = buddyTasks["410b2e0b-4ceb-1321-1727-20858f7f3477"].Result.Name;
                skinData.SheriffBuddyImage = buddyTasks["e336c6b8-418d-9340-d77f-7a9e4cfe0702"].Result.Image;
                skinData.SheriffBuddyName = buddyTasks["e336c6b8-418d-9340-d77f-7a9e4cfe0702"].Result.Name;
                skinData.StingerBuddyImage = buddyTasks["f7e1b454-4ad4-1063-ec0a-159e56b58941"].Result.Image;
                skinData.StingerBuddyName = buddyTasks["f7e1b454-4ad4-1063-ec0a-159e56b58941"].Result.Name;
                skinData.SpectreBuddyImage = buddyTasks["462080d1-4035-2937-7c09-27aa2a5c27a7"].Result.Image;
                skinData.SpectreBuddyName = buddyTasks["462080d1-4035-2937-7c09-27aa2a5c27a7"].Result.Name;
                skinData.BuckyBuddyImage = buddyTasks["910be174-449b-c412-ab22-d0873436b21b"].Result.Image;
                skinData.BuckyBuddyName = buddyTasks["910be174-449b-c412-ab22-d0873436b21b"].Result.Name;
                skinData.JudgeBuddyImage = buddyTasks["ec845bf4-4f79-ddda-a3da-0db3774b2794"].Result.Image;
                skinData.JudgeBuddyName = buddyTasks["ec845bf4-4f79-ddda-a3da-0db3774b2794"].Result.Name;
                skinData.BulldogBuddyImage = buddyTasks["ae3de142-4d85-2547-dd26-4e90bed35cf7"].Result.Image;
                skinData.BulldogBuddyName = buddyTasks["ae3de142-4d85-2547-dd26-4e90bed35cf7"].Result.Name;
                skinData.GuardianBuddyImage = buddyTasks["4ade7faa-4cf1-8376-95ef-39884480959b"].Result.Image;
                skinData.GuardianBuddyName = buddyTasks["4ade7faa-4cf1-8376-95ef-39884480959b"].Result.Name;
                skinData.PhantomBuddyImage = buddyTasks["ee8e8d15-496b-07ac-e5f6-8fae5d4c7b1a"].Result.Image;
                skinData.PhantomBuddyName = buddyTasks["ee8e8d15-496b-07ac-e5f6-8fae5d4c7b1a"].Result.Name;
                skinData.VandalBuddyImage = buddyTasks["9c82e19d-4575-0200-1a81-3eacf00cf872"].Result.Image;
                skinData.VandalBuddyName = buddyTasks["9c82e19d-4575-0200-1a81-3eacf00cf872"].Result.Name;
                skinData.MarshalBuddyImage = buddyTasks["c4883e50-4494-202c-3ec3-6b8a9284f00b"].Result.Image;
                skinData.MarshalBuddyName = buddyTasks["c4883e50-4494-202c-3ec3-6b8a9284f00b"].Result.Name;
                skinData.OutlawBuddyImage = buddyTasks["5f0aaf7a-4289-3998-d5ff-eb9a5cf7ef5c"].Result.Image;
                skinData.OutlawBuddyName = buddyTasks["5f0aaf7a-4289-3998-d5ff-eb9a5cf7ef5c"].Result.Name;
                skinData.OperatorBuddyImage = buddyTasks["a03b24d3-4319-996d-0f8c-94bbfba1dfc7"].Result.Image;
                skinData.OperatorBuddyName = buddyTasks["a03b24d3-4319-996d-0f8c-94bbfba1dfc7"].Result.Name;
                skinData.AresBuddyImage = buddyTasks["55d8a0f4-4274-ca67-fe2c-06ab45efdf58"].Result.Image;
                skinData.AresBuddyName = buddyTasks["55d8a0f4-4274-ca67-fe2c-06ab45efdf58"].Result.Name;
                skinData.OdinBuddyImage = buddyTasks["63e6c2b6-4a8e-869c-3d4c-e38355226584"].Result.Image;
                skinData.OdinBuddyName = buddyTasks["63e6c2b6-4a8e-869c-3d4c-e38355226584"].Result.Name;
                skinData.MeleeBuddyImage = buddyTasks["2f59173c-4bed-b6c3-2191-dea9b58be9c7"].Result.Image;
                skinData.MeleeBuddyName = buddyTasks["2f59173c-4bed-b6c3-2191-dea9b58be9c7"].Result.Name;
                }

                Constants.Log.Information("GetPreSkinInfoAsync: Player={Player} completed", playerno);
                return skinData;
            }
            catch (Exception ex)
            {
                Constants.Log.Error("GetPreSkinInfoAsync Failed: Player={Player}, Exception={Exception}", playerno, ex);
                return new SkinData();
            }

        Constants.Log.Error("GetPreSkinInfoAsync Failed: Player={Player}, StatusCode={StatusCode}", playerno, response.StatusCode);
        return new SkinData();
    }

    private static async Task<ValNameImage> GetBuddyFromLoadout(Dictionary<string, PreMatchItemValue> items, string weaponId)
    {
        if (items.TryGetValue(weaponId, out var weapon))
        {
            if (weapon.Sockets != null && weapon.Sockets.TryGetValue("bcef87d6-209b-46c6-8b19-fbe40bd95abc", out var charmSocket))
            {
                return await BuddyTitleHelper.GetBuddyInfoAsync(charmSocket.Item.Id).ConfigureAwait(false);
            }
        }
        return new ValNameImage();
    }


    private static async Task<SkinData> GetPartySkinInfoAsync(Guid puuid, Guid cardid)
    {
        var response = await DoCachedRequestAsync(
                Method.Get,
                $"https://pd.{Constants.Region}.a.pvp.net/personalization/v2/players/{puuid}/playerloadout",
                true
            )
            .ConfigureAwait(false);
        
        Constants.Log.Information("GetPartySkinInfoAsync Response: IsSuccessful={IsSuccessful}, StatusCode={StatusCode}", 
            response.IsSuccessful, response.StatusCode);
        
        if (response.IsSuccessful)
        {
            var playerLoadout = JsonSerializer.Deserialize<PlayerLoadoutResponse>(response.Content);
            
            var loadout = new LoadoutLoadout
            {
                Sprays = new Sprays
                {
                    SpraySelections = playerLoadout.Sprays.Select(s => new SpraySelection
                    {
                        AssetId = s.SprayId,
                        TypeId = s.EquipSlotId
                    }).ToArray()
                },
                Items = playerLoadout.Guns.ToDictionary(
                    g => g.Id.ToString(),
                    g => new ItemValue
                    {
                        Id = g.Id,
                        TypeId = g.Id,
                        Sockets = new Dictionary<string, Socket>
                        {
                            ["3ad1b2b2-acdb-4524-852f-954a76ddae0a"] = new Socket
                            {
                                Id = Guid.Parse("3ad1b2b2-acdb-4524-852f-954a76ddae0a"),
                                Item = new SocketItem
                                {
                                    Id = g.ChromaId,
                                    TypeId = g.SkinId
                                }
                            }
                        }
                    }
                )
            };
            
            var skinData = await GetSkinInfoAsync(loadout, cardid).ConfigureAwait(false);
            
            skinData.PlayerTitle = await BuddyTitleHelper.GetPlayerTitleAsync(playerLoadout.Identity.PlayerTitleId).ConfigureAwait(false);
            
            var buddyTasks = new Dictionary<string, Task<ValNameImage>>
            {
                ["29a0cfab-485b-f5d5-779a-b59f85e204a8"] = BuddyTitleHelper.GetBuddyInfoAsync(playerLoadout.Guns.FirstOrDefault(g => g.Id.ToString() == "29a0cfab-485b-f5d5-779a-b59f85e204a8")?.CharmLevelId),
                ["42da8ccc-40d5-affc-beec-15aa47b42eda"] = BuddyTitleHelper.GetBuddyInfoAsync(playerLoadout.Guns.FirstOrDefault(g => g.Id.ToString() == "42da8ccc-40d5-affc-beec-15aa47b42eda")?.CharmLevelId),
                ["44d4e95c-4157-0037-81b2-17841bf2e8e3"] = BuddyTitleHelper.GetBuddyInfoAsync(playerLoadout.Guns.FirstOrDefault(g => g.Id.ToString() == "44d4e95c-4157-0037-81b2-17841bf2e8e3")?.CharmLevelId),
                ["1baa85b4-4c70-1284-64bb-6481dfc3bb4e"] = BuddyTitleHelper.GetBuddyInfoAsync(playerLoadout.Guns.FirstOrDefault(g => g.Id.ToString() == "1baa85b4-4c70-1284-64bb-6481dfc3bb4e")?.CharmLevelId),
                ["410b2e0b-4ceb-1321-1727-20858f7f3477"] = BuddyTitleHelper.GetBuddyInfoAsync(playerLoadout.Guns.FirstOrDefault(g => g.Id.ToString() == "410b2e0b-4ceb-1321-1727-20858f7f3477")?.CharmLevelId),
                ["e336c6b8-418d-9340-d77f-7a9e4cfe0702"] = BuddyTitleHelper.GetBuddyInfoAsync(playerLoadout.Guns.FirstOrDefault(g => g.Id.ToString() == "e336c6b8-418d-9340-d77f-7a9e4cfe0702")?.CharmLevelId),
                ["f7e1b454-4ad4-1063-ec0a-159e56b58941"] = BuddyTitleHelper.GetBuddyInfoAsync(playerLoadout.Guns.FirstOrDefault(g => g.Id.ToString() == "f7e1b454-4ad4-1063-ec0a-159e56b58941")?.CharmLevelId),
                ["462080d1-4035-2937-7c09-27aa2a5c27a7"] = BuddyTitleHelper.GetBuddyInfoAsync(playerLoadout.Guns.FirstOrDefault(g => g.Id.ToString() == "462080d1-4035-2937-7c09-27aa2a5c27a7")?.CharmLevelId),
                ["910be174-449b-c412-ab22-d0873436b21b"] = BuddyTitleHelper.GetBuddyInfoAsync(playerLoadout.Guns.FirstOrDefault(g => g.Id.ToString() == "910be174-449b-c412-ab22-d0873436b21b")?.CharmLevelId),
                ["ec845bf4-4f79-ddda-a3da-0db3774b2794"] = BuddyTitleHelper.GetBuddyInfoAsync(playerLoadout.Guns.FirstOrDefault(g => g.Id.ToString() == "ec845bf4-4f79-ddda-a3da-0db3774b2794")?.CharmLevelId),
                ["ae3de142-4d85-2547-dd26-4e90bed35cf7"] = BuddyTitleHelper.GetBuddyInfoAsync(playerLoadout.Guns.FirstOrDefault(g => g.Id.ToString() == "ae3de142-4d85-2547-dd26-4e90bed35cf7")?.CharmLevelId),
                ["4ade7faa-4cf1-8376-95ef-39884480959b"] = BuddyTitleHelper.GetBuddyInfoAsync(playerLoadout.Guns.FirstOrDefault(g => g.Id.ToString() == "4ade7faa-4cf1-8376-95ef-39884480959b")?.CharmLevelId),
                ["ee8e8d15-496b-07ac-e5f6-8fae5d4c7b1a"] = BuddyTitleHelper.GetBuddyInfoAsync(playerLoadout.Guns.FirstOrDefault(g => g.Id.ToString() == "ee8e8d15-496b-07ac-e5f6-8fae5d4c7b1a")?.CharmLevelId),
                ["9c82e19d-4575-0200-1a81-3eacf00cf872"] = BuddyTitleHelper.GetBuddyInfoAsync(playerLoadout.Guns.FirstOrDefault(g => g.Id.ToString() == "9c82e19d-4575-0200-1a81-3eacf00cf872")?.CharmLevelId),
                ["c4883e50-4494-202c-3ec3-6b8a9284f00b"] = BuddyTitleHelper.GetBuddyInfoAsync(playerLoadout.Guns.FirstOrDefault(g => g.Id.ToString() == "c4883e50-4494-202c-3ec3-6b8a9284f00b")?.CharmLevelId),
                ["5f0aaf7a-4289-3998-d5ff-eb9a5cf7ef5c"] = BuddyTitleHelper.GetBuddyInfoAsync(playerLoadout.Guns.FirstOrDefault(g => g.Id.ToString() == "5f0aaf7a-4289-3998-d5ff-eb9a5cf7ef5c")?.CharmLevelId),
                ["a03b24d3-4319-996d-0f8c-94bbfba1dfc7"] = BuddyTitleHelper.GetBuddyInfoAsync(playerLoadout.Guns.FirstOrDefault(g => g.Id.ToString() == "a03b24d3-4319-996d-0f8c-94bbfba1dfc7")?.CharmLevelId),
                ["55d8a0f4-4274-ca67-fe2c-06ab45efdf58"] = BuddyTitleHelper.GetBuddyInfoAsync(playerLoadout.Guns.FirstOrDefault(g => g.Id.ToString() == "55d8a0f4-4274-ca67-fe2c-06ab45efdf58")?.CharmLevelId),
                ["63e6c2b6-4a8e-869c-3d4c-e38355226584"] = BuddyTitleHelper.GetBuddyInfoAsync(playerLoadout.Guns.FirstOrDefault(g => g.Id.ToString() == "63e6c2b6-4a8e-869c-3d4c-e38355226584")?.CharmLevelId),
                ["2f59173c-4bed-b6c3-2191-dea9b58be9c7"] = BuddyTitleHelper.GetBuddyInfoAsync(playerLoadout.Guns.FirstOrDefault(g => g.Id.ToString() == "2f59173c-4bed-b6c3-2191-dea9b58be9c7")?.CharmLevelId)
            };
            
            await Task.WhenAll(buddyTasks.Values).ConfigureAwait(false);
            
            skinData.ClassicBuddyImage = buddyTasks["29a0cfab-485b-f5d5-779a-b59f85e204a8"].Result.Image;
            skinData.ClassicBuddyName = buddyTasks["29a0cfab-485b-f5d5-779a-b59f85e204a8"].Result.Name;
            skinData.ShortyBuddyImage = buddyTasks["42da8ccc-40d5-affc-beec-15aa47b42eda"].Result.Image;
            skinData.ShortyBuddyName = buddyTasks["42da8ccc-40d5-affc-beec-15aa47b42eda"].Result.Name;
            skinData.FrenzyBuddyImage = buddyTasks["44d4e95c-4157-0037-81b2-17841bf2e8e3"].Result.Image;
            skinData.FrenzyBuddyName = buddyTasks["44d4e95c-4157-0037-81b2-17841bf2e8e3"].Result.Name;
            skinData.GhostBuddyImage = buddyTasks["1baa85b4-4c70-1284-64bb-6481dfc3bb4e"].Result.Image;
            skinData.GhostBuddyName = buddyTasks["1baa85b4-4c70-1284-64bb-6481dfc3bb4e"].Result.Name;
            skinData.BanditBuddyImage = buddyTasks["410b2e0b-4ceb-1321-1727-20858f7f3477"].Result.Image;
            skinData.BanditBuddyName = buddyTasks["410b2e0b-4ceb-1321-1727-20858f7f3477"].Result.Name;
            skinData.SheriffBuddyImage = buddyTasks["e336c6b8-418d-9340-d77f-7a9e4cfe0702"].Result.Image;
            skinData.SheriffBuddyName = buddyTasks["e336c6b8-418d-9340-d77f-7a9e4cfe0702"].Result.Name;
            skinData.StingerBuddyImage = buddyTasks["f7e1b454-4ad4-1063-ec0a-159e56b58941"].Result.Image;
            skinData.StingerBuddyName = buddyTasks["f7e1b454-4ad4-1063-ec0a-159e56b58941"].Result.Name;
            skinData.SpectreBuddyImage = buddyTasks["462080d1-4035-2937-7c09-27aa2a5c27a7"].Result.Image;
            skinData.SpectreBuddyName = buddyTasks["462080d1-4035-2937-7c09-27aa2a5c27a7"].Result.Name;
            skinData.BuckyBuddyImage = buddyTasks["910be174-449b-c412-ab22-d0873436b21b"].Result.Image;
            skinData.BuckyBuddyName = buddyTasks["910be174-449b-c412-ab22-d0873436b21b"].Result.Name;
            skinData.JudgeBuddyImage = buddyTasks["ec845bf4-4f79-ddda-a3da-0db3774b2794"].Result.Image;
            skinData.JudgeBuddyName = buddyTasks["ec845bf4-4f79-ddda-a3da-0db3774b2794"].Result.Name;
            skinData.BulldogBuddyImage = buddyTasks["ae3de142-4d85-2547-dd26-4e90bed35cf7"].Result.Image;
            skinData.BulldogBuddyName = buddyTasks["ae3de142-4d85-2547-dd26-4e90bed35cf7"].Result.Name;
            skinData.GuardianBuddyImage = buddyTasks["4ade7faa-4cf1-8376-95ef-39884480959b"].Result.Image;
            skinData.GuardianBuddyName = buddyTasks["4ade7faa-4cf1-8376-95ef-39884480959b"].Result.Name;
            skinData.PhantomBuddyImage = buddyTasks["ee8e8d15-496b-07ac-e5f6-8fae5d4c7b1a"].Result.Image;
            skinData.PhantomBuddyName = buddyTasks["ee8e8d15-496b-07ac-e5f6-8fae5d4c7b1a"].Result.Name;
            skinData.VandalBuddyImage = buddyTasks["9c82e19d-4575-0200-1a81-3eacf00cf872"].Result.Image;
            skinData.VandalBuddyName = buddyTasks["9c82e19d-4575-0200-1a81-3eacf00cf872"].Result.Name;
            skinData.MarshalBuddyImage = buddyTasks["c4883e50-4494-202c-3ec3-6b8a9284f00b"].Result.Image;
            skinData.MarshalBuddyName = buddyTasks["c4883e50-4494-202c-3ec3-6b8a9284f00b"].Result.Name;
            skinData.OutlawBuddyImage = buddyTasks["5f0aaf7a-4289-3998-d5ff-eb9a5cf7ef5c"].Result.Image;
            skinData.OutlawBuddyName = buddyTasks["5f0aaf7a-4289-3998-d5ff-eb9a5cf7ef5c"].Result.Name;
            skinData.OperatorBuddyImage = buddyTasks["a03b24d3-4319-996d-0f8c-94bbfba1dfc7"].Result.Image;
            skinData.OperatorBuddyName = buddyTasks["a03b24d3-4319-996d-0f8c-94bbfba1dfc7"].Result.Name;
            skinData.AresBuddyImage = buddyTasks["55d8a0f4-4274-ca67-fe2c-06ab45efdf58"].Result.Image;
            skinData.AresBuddyName = buddyTasks["55d8a0f4-4274-ca67-fe2c-06ab45efdf58"].Result.Name;
            skinData.OdinBuddyImage = buddyTasks["63e6c2b6-4a8e-869c-3d4c-e38355226584"].Result.Image;
            skinData.OdinBuddyName = buddyTasks["63e6c2b6-4a8e-869c-3d4c-e38355226584"].Result.Name;
            skinData.MeleeBuddyImage = buddyTasks["2f59173c-4bed-b6c3-2191-dea9b58be9c7"].Result.Image;
            skinData.MeleeBuddyName = buddyTasks["2f59173c-4bed-b6c3-2191-dea9b58be9c7"].Result.Name;
            
            return skinData;
        }

        Constants.Log.Error("GetPartySkinInfoAsync Failed: {e}", response.ErrorException);
        return new SkinData();
    }


    private static async Task<SkinData> GetSkinInfoAsync(LoadoutLoadout loadout, Guid cardid)
    {
        Dictionary<Guid, ValCard> cards = null;
        Dictionary<Guid, ValNameImage> sprays = null;
        Dictionary<Guid, ValNameImage> skins = null;
        try
        {
            skins = JsonSerializer.Deserialize<Dictionary<Guid, ValNameImage>>(
                await File.ReadAllTextAsync(
                        Constants.LocalAppDataPath + "\\ValAPI\\skinchromas.json"
                    )
                    .ConfigureAwait(false)
            );
            cards = JsonSerializer.Deserialize<Dictionary<Guid, ValCard>>(
                await File.ReadAllTextAsync(Constants.LocalAppDataPath + "\\ValAPI\\cards.json")
                    .ConfigureAwait(false)
            );
            sprays = JsonSerializer.Deserialize<Dictionary<Guid, ValNameImage>>(
                await File.ReadAllTextAsync(Constants.LocalAppDataPath + "\\ValAPI\\sprays.json")
                    .ConfigureAwait(false)
            );
        }
        catch (Exception e)
        {
            Constants.Log.Error("GetSkinInfoAsync failed: {e}", e);
        }

        ValNameImage defNI = new();
        ValCard defCard = new();
        ValCard card = SafeDict.GetValue(cards, cardid, defCard);
        
        // Log undefined sprays for debugging
        if (loadout?.Sprays?.SpraySelections != null)
        {
            for (int i = 0; i < Math.Min(loadout.Sprays.SpraySelections.Length, 4); i++)
            {
                var sprayAssetId = loadout.Sprays.SpraySelections[i].AssetId;
                // Removed undefined spray logging
            }
        }
        
        if (loadout?.Sprays?.SpraySelections == null || loadout.Items == null || skins == null)
        {
            Constants.Log.Error("GetSkinInfoAsync: loadout or skins data is null");
            return new SkinData
            {
                CardImage = card.Image,
                LargeCardImage = card.FullImage,
                CardName = card.Name
            };
        }
        var validSpraySelections = loadout.Sprays.SpraySelections.Where(s => s.AssetId.ToString() != "0a6db78c-48b9-a32d-c47a-82be597584c1").Take(4).ToList();
        var skinData = new SkinData
        {
            CardImage = card.Image,
            LargeCardImage = card.FullImage,
            CardName = card.Name,
            Spray1Image = validSpraySelections.Count > 0 ? SafeDict
                .GetValue(sprays, validSpraySelections[0].AssetId, defNI)
                .Image : defNI.Image,
            Spray1Name = validSpraySelections.Count > 0 ? SafeDict
                .GetValue(sprays, validSpraySelections[0].AssetId, defNI)
                .Name : defNI.Name,
            Spray2Image = validSpraySelections.Count > 1 ? SafeDict
                .GetValue(sprays, validSpraySelections[1].AssetId, defNI)
                .Image : defNI.Image,
            Spray2Name = validSpraySelections.Count > 1 ? SafeDict
                .GetValue(sprays, validSpraySelections[1].AssetId, defNI)
                .Name : defNI.Name,
            Spray3Image = validSpraySelections.Count > 2 ? SafeDict
                .GetValue(sprays, validSpraySelections[2].AssetId, defNI)
                .Image : defNI.Image,
            Spray3Name = validSpraySelections.Count > 2 ? SafeDict
                .GetValue(sprays, validSpraySelections[2].AssetId, defNI)
                .Name : defNI.Name,
            Spray4Image = defNI.Image,
            Spray4Name = defNI.Name,
            ClassicImage = skins[
                loadout.Items["29a0cfab-485b-f5d5-779a-b59f85e204a8"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Image,
            ClassicName = skins[
                loadout.Items["29a0cfab-485b-f5d5-779a-b59f85e204a8"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Name,
            ShortyImage = skins[
                loadout.Items["42da8ccc-40d5-affc-beec-15aa47b42eda"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Image,
            ShortyName = skins[
                loadout.Items["42da8ccc-40d5-affc-beec-15aa47b42eda"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Name,
            FrenzyImage = skins[
                loadout.Items["44d4e95c-4157-0037-81b2-17841bf2e8e3"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Image,
            FrenzyName = skins[
                loadout.Items["44d4e95c-4157-0037-81b2-17841bf2e8e3"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Name,
            GhostImage = skins[
                loadout.Items["1baa85b4-4c70-1284-64bb-6481dfc3bb4e"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Image,
            GhostName = skins[
                loadout.Items["1baa85b4-4c70-1284-64bb-6481dfc3bb4e"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Name,
            BanditImage = skins[
                loadout.Items["410b2e0b-4ceb-1321-1727-20858f7f3477"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Image,
            BanditName = skins[
                loadout.Items["410b2e0b-4ceb-1321-1727-20858f7f3477"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Name,
            SheriffImage = skins[
                loadout.Items["e336c6b8-418d-9340-d77f-7a9e4cfe0702"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Image,
            SheriffName = skins[
                loadout.Items["e336c6b8-418d-9340-d77f-7a9e4cfe0702"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Name,
            StingerImage = skins[
                loadout.Items["f7e1b454-4ad4-1063-ec0a-159e56b58941"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Image,
            StingerName = skins[
                loadout.Items["f7e1b454-4ad4-1063-ec0a-159e56b58941"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Name,
            SpectreImage = skins[
                loadout.Items["462080d1-4035-2937-7c09-27aa2a5c27a7"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Image,
            SpectreName = skins[
                loadout.Items["462080d1-4035-2937-7c09-27aa2a5c27a7"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Name,
            BuckyImage = skins[
                loadout.Items["910be174-449b-c412-ab22-d0873436b21b"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Image,
            BuckyName = skins[
                loadout.Items["910be174-449b-c412-ab22-d0873436b21b"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Name,
            JudgeImage = skins[
                loadout.Items["ec845bf4-4f79-ddda-a3da-0db3774b2794"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Image,
            JudgeName = skins[
                loadout.Items["ec845bf4-4f79-ddda-a3da-0db3774b2794"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Name,
            BulldogImage = skins[
                loadout.Items["ae3de142-4d85-2547-dd26-4e90bed35cf7"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Image,
            BulldogName = skins[
                loadout.Items["ae3de142-4d85-2547-dd26-4e90bed35cf7"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Name,
            GuardianImage = skins[
                loadout.Items["4ade7faa-4cf1-8376-95ef-39884480959b"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Image,
            GuardianName = skins[
                loadout.Items["4ade7faa-4cf1-8376-95ef-39884480959b"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Name,
            PhantomImage = skins[
                loadout.Items["ee8e8d15-496b-07ac-e5f6-8fae5d4c7b1a"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Image,
            PhantomName = skins[
                loadout.Items["ee8e8d15-496b-07ac-e5f6-8fae5d4c7b1a"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Name,
            VandalImage = skins[
                loadout.Items["9c82e19d-4575-0200-1a81-3eacf00cf872"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Image,
            VandalName = skins[
                loadout.Items["9c82e19d-4575-0200-1a81-3eacf00cf872"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Name,
            MarshalImage = skins[
                loadout.Items["c4883e50-4494-202c-3ec3-6b8a9284f00b"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Image,
            MarshalName = skins[
                loadout.Items["c4883e50-4494-202c-3ec3-6b8a9284f00b"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Name,
            OutlawImage = skins[
                loadout.Items["5f0aaf7a-4289-3998-d5ff-eb9a5cf7ef5c"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Image,
            OutlawName = skins[
                loadout.Items["5f0aaf7a-4289-3998-d5ff-eb9a5cf7ef5c"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Name,
            OperatorImage = skins[
                loadout.Items["a03b24d3-4319-996d-0f8c-94bbfba1dfc7"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Image,
            OperatorName = skins[
                loadout.Items["a03b24d3-4319-996d-0f8c-94bbfba1dfc7"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Name,
            AresImage = skins[
                loadout.Items["55d8a0f4-4274-ca67-fe2c-06ab45efdf58"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Image,
            AresName = skins[
                loadout.Items["55d8a0f4-4274-ca67-fe2c-06ab45efdf58"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Name,
            OdinImage = skins[
                loadout.Items["63e6c2b6-4a8e-869c-3d4c-e38355226584"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Image,
            OdinName = skins[
                loadout.Items["63e6c2b6-4a8e-869c-3d4c-e38355226584"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Name,
            MeleeImage = skins[
                loadout.Items["2f59173c-4bed-b6c3-2191-dea9b58be9c7"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Image,
            MeleeName = skins[
                loadout.Items["2f59173c-4bed-b6c3-2191-dea9b58be9c7"].Sockets[
                    "3ad1b2b2-acdb-4524-852f-954a76ddae0a"
                ]
                    .Item
                    .Id
            ].Name
        };
        if (skinData == null)
            Constants.Log.Error("GetSkinInfoAsync failed: skinData is null");

        return skinData;
    }

    public static async Task<MatchHistoryData> GetMatchHistoryAsync(Guid puuid)
    {
        MatchHistoryData history =
            new()
            {
                PreviousGameColours = new string[3] { "#7f7f7f", "#7f7f7f", "#7f7f7f" },
                PreviousGames = new int[3]
            };

        try
        {
            if (puuid == Guid.Empty)
            {
                Constants.Log.Error("GetMatchHistoryAsync: Puuid is null");
                return history;
            }
            var response = await DoCachedRequestAsync(
                    Method.Get,
                    $"https://pd.{Constants.Region}.a.pvp.net/mmr/v1/players/{puuid}/competitiveupdates?queue=competitive",
                    true
                )
                .ConfigureAwait(false);
            if (!response.IsSuccessful)
            {
                if (response.StatusCode == (HttpStatusCode)429) // Too Many Requests
                {
                    await Task.Delay(5000); // Wait 5 seconds before retry
                }
                Constants.Log.Error(
                    "GetMatchHistoryAsync request failed: {e}",
                    response.ErrorException
                );
                return history;
            }

            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
            };
            var content = JsonSerializer.Deserialize<CompetitiveUpdatesResponse>(
                response.Content,
                options
            );

            if (content?.Matches == null || content.Matches.Length == 0)
            {
                return history;
            }

            history.RankProgress = content.Matches[0].RankedRatingAfterUpdate;

            for (int i = 0; i < 3; i++)
            {
                if (i >= content.Matches.Length)
                    break;
                var match = content.Matches[i].RankedRatingEarned;
                history.PreviousGames[i] = Math.Abs(match);
                history.PreviousGameColours[i] = match switch
                {
                    > 0 => "#32e2b2",
                    < 0 => "#ff4654",
                    _ => "#7f7f7f"
                };
            }
        }
        catch (Exception e)
        {
            Constants.Log.Error("GetMatchHistoryAsync failed: {e}", e);
        }

        return history;
    }

    private static async Task<RankData> GetPlayerHistoryAsync(Guid puuid, Guid[] seasonData)
    {
        var rankData = new RankData();
        var ranks = new int[4];

        rankData.RankImages = new Uri[ranks.Length];
        rankData.RankNames = new string[ranks.Length];
        Array.Fill(
            rankData.RankImages,
            new Uri(Constants.LocalAppDataPath + $"\\ValAPI\\ranksimg\\0.png")
        );
        Array.Fill(rankData.RankNames, "UNRATED");

        if (puuid == Guid.Empty)
        {
            Constants.Log.Error("GetPlayerHistoryAsync Failed: PUUID is empty");
            return rankData;
        }
        var response = await DoCachedRequestAsync(
                Method.Get,
                $"https://pd.{Constants.Region}.a.pvp.net/mmr/v1/players/{puuid}",
                true
            )
            .ConfigureAwait(false);

        if (!response.IsSuccessful && response.Content != null)
        {
            Constants.Log.Error("GetPlayerHistoryAsync Failed: {e}", response.ErrorException);
            return rankData;
        }

        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };
        var content = JsonSerializer.Deserialize<MmrResponse>(response.Content, options);

        if (content.QueueSkills.Competitive.SeasonalInfoBySeasonId is null)
        {
            Constants.Log.Warning("GetPlayerHistoryAsync: seasonInfoById is null, returning default rank data");
            return rankData;
        }

        var SeasonInfo = content.QueueSkills.Competitive.SeasonalInfoBySeasonId.Act;

        for (int i = 0; i < ranks.Length; i++)
        {
            if (!SeasonInfo.TryGetValue(seasonData[i].ToString(), out var currentActJsonElement))
                continue;

            var act = currentActJsonElement.Deserialize<ActInfo>();
            var rank = act.CompetitiveTier;

            if (rank is 1 or 2)
                rank = 0;
            if (Constants.BeforeAscendantSeasons.Contains(seasonData[i]))
                rank += 3;

            ranks[i] = rank;
        }

        if (ranks[0] >= 24)
        {
            var leaderboardResponse = await DoCachedRequestAsync(
                    Method.Get,
                    $"https://pd.{Constants.Shard}.a.pvp.net/mmr/v1/leaderboards/affinity/{Constants.Region}/queue/competitive/season/{seasonData[0]}?startIndex=0&size=0",
                    true
                )
                .ConfigureAwait(false);
            if (leaderboardResponse.Content != null && leaderboardResponse.IsSuccessful)
            {
                var leaderboardcontent = JsonSerializer.Deserialize<LeaderboardsResponse>(
                    leaderboardResponse.Content
                );
                try
                {
                    rankData.MaxRr = leaderboardcontent.TierDetails[
                        ranks[0].ToString()
                    ].RankedRatingThreshold;
                }
                catch (Exception e)
                {
                    Constants.Log.Error(
                        "GetPlayerHistoryAsync Failed; leaderboardcontent error: {e}",
                        e
                    );
                }
            }
            else
            {
                Constants.Log.Error(
                    "GetPlayerHistoryAsync Failed; leaderboardResponse error: {e}",
                    leaderboardResponse.ErrorException
                );
            }
        }

        try
        {
            var rankNames = JsonSerializer.Deserialize<Dictionary<int, string>>(
                await File.ReadAllTextAsync(
                        Constants.LocalAppDataPath + "\\ValAPI\\competitivetiers.json"
                    )
                    .ConfigureAwait(false)
            );

            for (int i = 0; i < ranks.Length; i++)
            {
                rankNames.TryGetValue(ranks[i], out var rank);
                rankData.RankImages[i] = new Uri(
                    Constants.LocalAppDataPath + $"\\ValAPI\\ranksimg\\{ranks[i]}.png"
                );
                rankData.RankNames[i] = rank;
            }
        }
        catch (Exception e)
        {
            Constants.Log.Error("GetPlayerHistoryAsync Failed; rank dictionary error: {e}", e);
        }

        return rankData;
    }

    private static async Task<Guid[]> GetSeasonsAsync()
    {
        var seasonData = new Guid[4];
        try
        {
            var response = await DoCachedRequestAsync(
                Method.Get,
                $"https://shared.{Constants.Region}.a.pvp.net/content-service/v3/content",
                true
            );

            if (!response.IsSuccessful)
            {
                Constants.Log.Error("GetSeasonsAsync Failed: {e}", response.ErrorException);
                return seasonData;
            }

            var data = JsonSerializer.Deserialize<ContentResponse>(response.Content);

            sbyte index = 0;
            var seasons = data.Seasons.Reverse();
            var acts = seasons.Where(season => season.Type == "act");

            foreach (var act in acts)
            {
                if (index >= seasonData.Length)
                    break;
                if (index > 0)
                {
                    seasonData[index] = act.Id;
                    index++;
                }
                if (index == 0 & act.IsActive)
                {
                    seasonData[0] = act.Id;
                    index++;
                }
            }
        }
        catch (Exception e)
        {
            Constants.Log.Error("GetSeasonsAsync Failed: {Exception}", e);
        }

        return seasonData;
    }

    private static Task<Uri> TrackerAsync(string username)
    {
        if (string.IsNullOrEmpty(username))
            return Task.FromResult<Uri>(null);
        
        try
        {
            var encodedUsername = Uri.EscapeDataString(username);
            // Directly return the link without validation
            return Task.FromResult(new Uri("https://tracker.gg/valorant/profile/riot/" + encodedUsername));
        }
        catch (Exception e)
        {
            Constants.Log.Error("TrackerAsync Failed: {Exception}", e);
        }
        return Task.FromResult<Uri>(null);
    }

    private static async Task<PresencesResponse> GetPresencesAsync()
    {
        var options = new RestClientOptions($"https://127.0.0.1:{Constants.Port}/chat/v4/presences")
        {
            RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                true
        };
        var client = new RestClient(options);
        var base64String = "";
        try
        {
            base64String = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"riot:{Constants.LPassword}")
            );
        }
        catch (Exception e)
        {
            Constants.Log.Error("GetPresencesAsync Failed; To Base 64 failed: {Exception}", e);
            return null;
        }

        var request = new RestRequest()
            .AddHeader("Authorization", $"Basic {base64String}")
            .AddHeader("X-Riot-ClientPlatform", Constants.Platform)
            .AddHeader("X-Riot-ClientVersion", Constants.Version);
        var response = await client
            .ExecuteGetAsync<PresencesResponse>(request)
            .ConfigureAwait(false);
        if (response.IsSuccessful)
            return response.Data;
        Constants.Log.Error(
            "GetPresencesAsync Failed: {e}. Presences: {presences}",
            response.ErrorException,
            response.Data
        );
        return null;
    }

    private async Task<PlayerUIData> GetPresenceInfoAsync(Guid puuid, PresencesResponse presences)
    {
        PlayerUIData playerUiData = new() { BackgroundColour = "#252A40", Puuid = puuid };

        try
        {
            if (presences?.Presences == null)
            {
                Constants.Log.Warning("GetPresenceInfoAsync: Presences is null");
                return playerUiData;
            }

            var friend = presences.Presences.FirstOrDefault(f => f.Puuid == puuid);
            if (friend == null)
            {
                // Not in friend list, expected for random players
                return playerUiData;
            }

            if (string.IsNullOrEmpty(friend.Private))
            {
                // Private data empty
                return playerUiData;
            }

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(friend.Private));
            var content = JsonSerializer.Deserialize<PresencesPrivate>(json);
            if (content == null)
                return playerUiData;

            playerUiData.PartyUuid = content.PartyId;

            if (puuid != Constants.Ppuuid)
                return playerUiData;

            var maps = JsonSerializer.Deserialize<Dictionary<string, ValMap>>(
                await File.ReadAllTextAsync(Constants.LocalAppDataPath + "\\ValAPI\\maps.json")
                    .ConfigureAwait(false)
            );

            if (maps != null && !string.IsNullOrEmpty(content.MatchMap) && maps.TryGetValue(content.MatchMap, out var map))
            {
                MatchInfo.Map = map.Name;
                MatchInfo.MapImage = new Uri(
                    Constants.LocalAppDataPath + $"\\ValAPI\\mapsimg\\{map.UUID}.png"
                );
            }
            playerUiData.BackgroundColour = "#181E34";
            Constants.PPartyId = content.PartyId;

            if (content?.ProvisioningFlow == "CustomGame")
            {
                MatchInfo.GameMode = "Custom";
                MatchInfo.GameModeImage = new Uri(
                    Constants.LocalAppDataPath
                        + "\\ValAPI\\gamemodeimg\\96bd3920-4f36-d026-2b28-c683eb0bcac5.png"
                );
                return playerUiData;
            }
            var textInfo = new CultureInfo("en-US", false).TextInfo;

            var gameModeName = "";
            var gameModeId = Guid.Parse("96bd3920-4f36-d026-2b28-c683eb0bcac5");
            QueueId = content?.QueueId;
            Status = content?.SessionLoopState;

            switch (content?.QueueId)
            {
                case "competitive":
                    gameModeName = "Competitive";
                    break;
                case "unrated":
                    gameModeName = "Unrated";
                    break;
                case "deathmatch":
                    gameModeId = Guid.Parse("a8790ec5-4237-f2f0-e93b-08a8e89865b2");
                    break;
                case "spikerush":
                    gameModeId = Guid.Parse("e921d1e6-416b-c31f-1291-74930c330b7b");
                    break;
                case "ggteam":
                    gameModeId = Guid.Parse("a4ed6518-4741-6dcb-35bd-f884aecdc859");
                    break;
                case "newmap":
                    gameModeName = "New Map";
                    break;
                case "onefa":
                    gameModeId = Guid.Parse("96bd3920-4f36-d026-2b28-c683eb0bcac5");
                    break;
                case "snowball":
                    gameModeId = Guid.Parse("57038d6d-49b1-3a74-c5ef-3395d9f23a97");
                    break;
                default:
                    gameModeName = textInfo.ToTitleCase(content.QueueId);
                    break;
            }

            MatchInfo.GameMode = gameModeName;

            if (string.IsNullOrEmpty(gameModeName))
            {
                try 
                {
                    var jsonContent = await File.ReadAllTextAsync(Constants.LocalAppDataPath + "\\ValAPI\\gamemode.json").ConfigureAwait(false);
                    
                    // Check if JSON is in new format (Dictionary<string, ValGamemodeInfo>) or old format
                    if (jsonContent.Contains("\"AssetPath\""))
                    {
                        var gamemodes = JsonSerializer.Deserialize<Dictionary<string, ValGamemodeInfo>>(jsonContent);
                        if (gamemodes.TryGetValue(gameModeId.ToString(), out var info))
                        {
                            MatchInfo.GameMode = info.Name;
                        }
                    }
                    else
                    {
                        // Fallback to old format
                        var gamemodes = JsonSerializer.Deserialize<Dictionary<Guid, string>>(jsonContent);
                        gamemodes.TryGetValue(gameModeId, out var gamemode);
                        MatchInfo.GameMode = gamemode;
                    }
                }
                catch (JsonException)
                {
                    // Handle JSON parsing errors gracefully
                    Constants.Log.Warning("GetPresenceInfoAsync: Failed to parse gamemode.json");
                }
            }

            MatchInfo.GameModeImage = new Uri(
                Constants.LocalAppDataPath + $"\\ValAPI\\gamemodeimg\\{gameModeId}.png"
            );
        }
        catch (InvalidOperationException)
        {
            return playerUiData;
        }
        catch (Exception e)
        {
            Constants.Log.Error("GetPresenceInfoAsync Failed; To Base 64 failed: {Exception}", e);
        }

        return playerUiData;
    }
}
