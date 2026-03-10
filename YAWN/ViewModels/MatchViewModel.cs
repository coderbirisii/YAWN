using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YAWN.Helpers;
using YAWN.Objects;

namespace YAWN.ViewModels;

public partial class MatchViewModel : ObservableObject
{
    public delegate void EventAction();

    [ObservableProperty]
    private int _countdownTime = 80;

    [ObservableProperty]
    private DispatcherTimer _countTimer;

    [ObservableProperty]
    private List<Player> _leftPlayerList;

    [ObservableProperty]
    private MatchDetails _match;

    [ObservableProperty]
    private LoadingOverlay _overlay;

    [ObservableProperty]
    private string _refreshTime = "-";
    private int _resettime = 80;

    [ObservableProperty]
    private List<Player> _rightPlayerList;

    [ObservableProperty]
    private bool _isMatchActive;

    public MatchViewModel()
    {
        _countTimer = new DispatcherTimer();
        _countTimer.Tick += UpdateTimersAsync;
        _countTimer.Interval = new TimeSpan(0, 0, 1);

        Match = new MatchDetails();
        Overlay = new LoadingOverlay
        {
            Header = "Loading",
            Content = "Getting Match Details",
            IsBusy = false
        };

        LeftPlayerList = new List<Player>();
        RightPlayerList = new List<Player>();
    }

    public event EventAction GoHomeEvent;

    [RelayCommand]
    private void PassiveLoadAsync()
    {
        if (!_countTimer.IsEnabled)
            _countTimer.Start();
    }

    [RelayCommand]
    private async Task PassiveLoadCheckAsync()
    {
        if (!_countTimer.IsEnabled)
        {
            _countTimer.Start();
            await GetMatchInfoAsync().ConfigureAwait(false);
        }
    }

    [RelayCommand]
    private void StopPassiveLoadAsync()
    {
        CountTimer?.Stop();
        RefreshTime = "-";
    }

    private async void UpdateTimersAsync(object sender, EventArgs e)
    {
        RefreshTime = CountdownTime + "s";
        if (CountdownTime <= 0)
        {
            CountdownTime = _resettime;
            await GetMatchInfoAsync().ConfigureAwait(false);
        }

        CountdownTime--;
    }

    [RelayCommand]
    private async Task GetMatchInfoAsync()
    {
        Overlay = new LoadingOverlay
        {
            IsBusy = true,
            Header = "Loading",
            Progress = 0
        };

        try
        {
            LiveMatch newLiveMatch = new();
            if (await LiveMatch.LiveMatchChecksAsync().ConfigureAwait(false))
            {
                var AllPlayers = new List<Player>();
                Overlay.Content = "Getting Player Details";
                AllPlayers = await newLiveMatch
                    .LiveMatchOutputAsync(UpdatePercentage)
                    .ConfigureAwait(false);

                if (newLiveMatch.Status != "PREGAME")
                {
                    _resettime = 120;
                    CountdownTime = 120;
                }

                if (newLiveMatch.QueueId == "deathmatch" || AllPlayers.Count > 10)
                {
                    var mid = AllPlayers.Count / 2;
                    LeftPlayerList = AllPlayers.Take(mid).ToList();
                    RightPlayerList = AllPlayers.Skip(mid).ToList();
                }
                else
                {
                    LeftPlayerList.Clear();
                    RightPlayerList.Clear();
                    foreach (var player in AllPlayers)
                        switch (player.TeamId)
                        {
                            case "Blue":
                                LeftPlayerList.Add(player);
                                break;
                            case "Red":
                                RightPlayerList.Add(player);
                                break;
                        }

                    LeftPlayerList = LeftPlayerList.ToList();
                    RightPlayerList = RightPlayerList.ToList();
                }

                AllPlayers.Clear();

                if (newLiveMatch.MatchInfo != null)
                    Match = newLiveMatch.MatchInfo;

                await UpdateMatchInfoAsync().ConfigureAwait(false);

                UpdateStats();

                IsMatchActive = true;
                Overlay.IsBusy = false;
            }
            else
            {
                IsMatchActive = false;
                CountTimer?.Stop();
                GoHomeEvent?.Invoke();
            }
        }
        catch (Exception)
        {
            IsMatchActive = false;
        }
        finally
        {
            Overlay.IsBusy = false;
        }

        GC.Collect();
    }

    private async Task UpdateMatchInfoAsync()
    {
        try
        {
            var matchIdInfo = await LiveMatch.GetMatchResponseAsync().ConfigureAwait(false);
            if (matchIdInfo != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Map Info
                    if (matchIdInfo.MapId != null)
                    {
                        var mapUrl = matchIdInfo.MapId.ToString();
                        Match.Map = ValApi.GetMapName(mapUrl);
                        var mapId = ValApi.GetMapIdFromUrl(mapUrl);
                        
                        Constants.Log.Information(">>> [MATCH DATA] MapURL: {Url}", mapUrl);
                        Constants.Log.Information(">>> [MATCH DATA] MapID: {Id}", mapId);
                        Constants.Log.Information(">>> [MATCH DATA] MapName: {Name}", Match.Map);

                        if (mapId != null)
                        {
                            var mapPath = System.IO.Path.Combine(Constants.LocalAppDataPath, "ValAPI", "mapsimg", mapId + ".png");
                            if (System.IO.File.Exists(mapPath))
                            {
                                Match.MapImage = new Uri(mapPath);
                                Constants.Log.Information(">>> [MATCH DATA] Map Image FOUND: {Path}", mapPath);
                            }
                            else
                            {
                                Constants.Log.Warning(">>> [MATCH DATA] Map Image MISSING: {Path}", mapPath);
                            }
                        }
                    }

                    // Queue Info
                    string queueId = matchIdInfo.QueueId?.ToString();
                    string provisioningFlow = "Matchmaking"; // Default
                    try 
                    {
                        // Handle dynamic ProvisioningFlow property which might not exist on PreMatchResponse
                        if (matchIdInfo is LiveMatchResponse liveMatch)
                        {
                            provisioningFlow = liveMatch.ProvisioningFlow;
                        }
                        // PreMatchResponse doesn't have ProvisioningFlow usually, but we can check if it's dynamic
                        else 
                        {
                            // If it's a dynamic object or has the property via reflection
                            var prop = matchIdInfo.GetType().GetProperty("ProvisioningFlow");
                            if (prop != null)
                            {
                                provisioningFlow = prop.GetValue(matchIdInfo)?.ToString() ?? "Matchmaking";
                            }
                        }
                    }
                    catch { }

                    if (queueId == null || queueId == "")
                    {
                        if (provisioningFlow == "CustomGame")
                        {
                            queueId = "custom";
                        }
                    }

                    if (queueId != null)
                    {
                        Match.GameMode = ValApi.GetQueueName(queueId);
                        
                        // If it's a custom game, try to get more specific game mode info from ModeId
                        // Custom games might have weird QueueIds (like paths) or "custom" or "null" (handled above)
                        bool isCustomGame = queueId == "custom" || 
                                            Match.GameMode == "CUSTOM" || 
                                            provisioningFlow == "CustomGame" ||
                                            queueId.StartsWith("/Game/");

                        if (isCustomGame && matchIdInfo.ModeId != null)
                        {
                            var modeId = matchIdInfo.ModeId.ToString();
                            // First try to get info using the full path
                            var modeInfo = ValApi.GetGamemodeInfo(modeId);
                            
                            // If not found, try to extract the game mode name from the path (e.g. BombGameMode -> Bomb)
                            if (modeInfo == null && modeId.Contains("/"))
                            {
                                // Try to find a partial match in the gamemode dictionary
                                // This is a bit of a hack, but might work if exact path matching fails
                            }
                            
                            if (modeInfo != null)
                            {
                                Match.GameMode = modeInfo.Name;
                                // Try to update image with the specific mode image
                                var specificModeIconPath = System.IO.Path.Combine(Constants.LocalAppDataPath, "ValAPI", "gamemodeimg", modeInfo.Uuid + ".png");
                                if (System.IO.File.Exists(specificModeIconPath))
                                {
                                    Match.GameModeImage = new Uri(specificModeIconPath);
                                    Constants.Log.Information(">>> [MATCH DATA] Specific GameMode Image FOUND: {Path}", specificModeIconPath);
                                }
                            }
                            else
                            {
                                // Fallback: if we can't find the mode info, at least try to make the name look nicer
                                if (modeId.Contains("Bomb")) Match.GameMode = "Standard";
                                else if (modeId.Contains("Deathmatch")) Match.GameMode = "Deathmatch";
                                else if (modeId.Contains("OneForAll")) Match.GameMode = "Replication";
                                else if (modeId.Contains("GunGame")) Match.GameMode = "Escalation";
                                else if (modeId.Contains("SpikeRush")) Match.GameMode = "Spike Rush";
                                else if (modeId.Contains("Swiftplay")) Match.GameMode = "Swiftplay";
                                else if (modeId.Contains("HURM")) Match.GameMode = "Team Deathmatch";
                            }
                        }

                        // Final check to prevent path-like strings in UI
                        if (!string.IsNullOrEmpty(Match.GameMode) && (Match.GameMode.StartsWith("/Game/") || Match.GameMode.Contains("/")))
                        {
                            if (Match.GameMode.Contains("Swiftplay")) Match.GameMode = "Swiftplay";
                            else if (Match.GameMode.Contains("Bomb")) Match.GameMode = "Standard";
                            else if (Match.GameMode.Contains("SpikeRush")) Match.GameMode = "Spike Rush";
                            else if (Match.GameMode.Contains("Deathmatch")) Match.GameMode = "Deathmatch";
                            else Match.GameMode = "Custom"; // Fallback to simple "Custom" if we can't parse it
                        }

                        Constants.Log.Information(">>> [MATCH DATA] QueueID: {Id}", queueId);
                        Constants.Log.Information(">>> [MATCH DATA] QueueName: {Name}", Match.GameMode);

                        if (Match.GameModeImage == null) // Only set if not already set by specific mode logic
                        {
                            var queueIconPath = System.IO.Path.Combine(Constants.LocalAppDataPath, "ValAPI", "gamemodeimg", queueId + ".png");
                            if (System.IO.File.Exists(queueIconPath))
                            {
                                Match.GameModeImage = new Uri(queueIconPath);
                                Constants.Log.Information(">>> [MATCH DATA] Queue Image FOUND: {Path}", queueIconPath);
                            }
                            else
                            {
                                Constants.Log.Warning(">>> [MATCH DATA] Queue Image MISSING: {Path}", queueIconPath);
                            }
                        }
                    }

                    // Server Info
                    string gamePodId = matchIdInfo.GamePodId?.ToString();
                    Match.ServerName = GetServerName(gamePodId);
                    Constants.Log.Information(">>> [MATCH DATA] GamePodID: {Id}, ServerName: {Name}", gamePodId, Match.ServerName);
                    
                    Constants.Log.Information(">>> [MATCH DATA] Match Info Updated. Mode: {Mode}, Map: {Map}", Match.GameMode, Match.Map);
                });
            }
            else
            {
                Constants.Log.Warning(">>> [MATCH DATA] No Match Data received from API");
            }
        }
        catch (Exception ex)
        {
            Constants.Log.Error(">>> [MATCH DATA] ERROR in UpdateMatchInfoAsync: {Error}", ex.ToString());
        }
    }

    private string GetServerName(string gamePodId)
    {
        if (string.IsNullOrEmpty(gamePodId)) return Constants.Region.ToUpper();
        gamePodId = gamePodId.ToLower();
        
        if (gamePodId.Contains("fra")) return "Frankfurt";
        if (gamePodId.Contains("par")) return "Paris";
        if (gamePodId.Contains("lon")) return "London";
        if (gamePodId.Contains("mad")) return "Madrid";
        if (gamePodId.Contains("waw")) return "Warsaw";
        if (gamePodId.Contains("ist")) return "Istanbul";
        if (gamePodId.Contains("bah")) return "Bahrain";
        if (gamePodId.Contains("sto")) return "Stockholm";
        if (gamePodId.Contains("tok")) return "Tokyo";
        if (gamePodId.Contains("mum")) return "Mumbai";
        if (gamePodId.Contains("syd")) return "Sydney";
        if (gamePodId.Contains("seo")) return "Seoul";
        if (gamePodId.Contains("hk")) return "Hong Kong";
        if (gamePodId.Contains("sg")) return "Singapore";
        if (gamePodId.Contains("us-east")) return "N. Virginia";
        if (gamePodId.Contains("us-west")) return "N. California";
        if (gamePodId.Contains("br")) return "Sao Paulo";
        if (gamePodId.Contains("latam")) return "Santiago";
        
        return Constants.Region.ToUpper();
    }

    private async void UpdateStats()
    {
        // List<Task> tasks = new();
        var AllPlayers = LeftPlayerList.Concat(RightPlayerList).ToList();
        foreach (var player in AllPlayers)
        {
            if (player.PlayerUiData is null)
                continue;
            // var t1 = LiveMatch.GetMatchHistoryAsync(player.PlayerUiData.Puuid);
            // player.MatchHistoryData = t1.Result;
            player.MatchHistoryData = await LiveMatch
                .GetMatchHistoryAsync(player.PlayerUiData.Puuid)
                .ConfigureAwait(false);
        }
    }

    private void UpdatePercentage(int percentage)
    {
        Overlay.Progress = percentage;
    }
}
