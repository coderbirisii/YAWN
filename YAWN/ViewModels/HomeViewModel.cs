using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using FontAwesome6;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YAWN.Helpers;
using YAWN.Objects;
using YAWN.Views;
using static YAWN.Helpers.Login;

namespace YAWN.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    public delegate void EventAction();

    [ObservableProperty]
    private int _countdownTime = 20;

    [ObservableProperty]
    private DispatcherTimer _countTimer;
    private int _cycle = 3;

    [ObservableProperty]
    private List<Player> _playerList;

    [ObservableProperty]
    private string _refreshTime = "-";

    [ObservableProperty]
    private string _currentQueueName = "Custom";

    [ObservableProperty]
    private string _currentShardName = "Sydney";

    [ObservableProperty]
    private string _currentMapName = "Ascent";

    [ObservableProperty]
    private string _currentQueueImage = $"{Constants.LocalAppDataPath}\\ValAPI\\gamemodeimg\\unrated.png";

    [ObservableProperty]
    private string _currentMapImage = $"{Constants.LocalAppDataPath}\\ValAPI\\mapsimg\\7eaecc1b-4337-bbf6-6ab9-04b8f06b3319.png";

    public HomeViewModel()
    {
        _countTimer = new DispatcherTimer();
        _countTimer.Tick += UpdateTimersAsync;
        _countTimer.Interval = new TimeSpan(0, 0, 1);
    }

    public event EventAction GoMatchEvent;

    [RelayCommand]
    private async Task LoadNowAsync()
    {
        CountdownTime = 20;
        await UpdateChecksAsync(true).ConfigureAwait(false);
    }

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
            await UpdateChecksAsync(true).ConfigureAwait(false);
        }
    }

    [RelayCommand]
    private void StopPassiveLoadAsync()
    {
        _countTimer.Stop();
        RefreshTime = "-";
    }

    private async void UpdateTimersAsync(object sender, EventArgs e)
    {
        RefreshTime = CountdownTime + "s";
        if (CountdownTime == 0)
        {
            CountdownTime = 15;
            await UpdateChecksAsync(false).ConfigureAwait(false);
        }

        CountdownTime--;
    }

    [RelayCommand]
    private async Task UpdateChecksAsync(bool forcePartyUpdate)
    {
        await UpdateMatchInfoAsync().ConfigureAwait(false);
        Application.Current.Dispatcher.Invoke(() =>
        {
            Home.RiotStatus.Icon = EFontAwesomeIcon.Solid_Question;
            Home.RiotStatus.Foreground = new SolidColorBrush(Color.FromRgb(0, 126, 249));
            Home.ValorantStatus.Icon = EFontAwesomeIcon.Solid_Question;
            Home.ValorantStatus.Foreground = new SolidColorBrush(Color.FromRgb(0, 126, 249));
            Home.AccountStatus.Icon = EFontAwesomeIcon.Solid_Question;
            Home.AccountStatus.Foreground = new SolidColorBrush(Color.FromRgb(0, 126, 249));
            Home.MatchStatus.Icon = EFontAwesomeIcon.Solid_Question;
            Home.MatchStatus.Foreground = new SolidColorBrush(Color.FromRgb(0, 126, 249));
        });

        var riotClientRunning =
            Process.GetProcessesByName("RiotClientServices").Length > 0
            || Process.GetProcessesByName("Riot Client").Length > 0;
        var valorantRunning = Process.GetProcessesByName("VALORANT-Win64-Shipping").Length > 0;

        if (await Checks.CheckLocalAsync().ConfigureAwait(false))
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    Home.RiotStatus.Icon = riotClientRunning
                        ? EFontAwesomeIcon.Solid_Check
                        : EFontAwesomeIcon.Solid_Xmark;
                    Home.RiotStatus.Foreground = new SolidColorBrush(
                        riotClientRunning ? Color.FromRgb(50, 226, 178) : Color.FromRgb(255, 70, 84)
                    );

                    Home.ValorantStatus.Icon = valorantRunning
                        ? EFontAwesomeIcon.Solid_Check
                        : EFontAwesomeIcon.Solid_Xmark;
                    Home.ValorantStatus.Foreground = new SolidColorBrush(
                        valorantRunning ? Color.FromRgb(50, 226, 178) : Color.FromRgb(255, 70, 84)
                    );
                }
                catch (Exception e)
                {
                    Constants.Log.Error("UpdateChecksAsync status icon update failed: {e}", e);
                }
            });
            if (await Checks.CheckLoginAsync().ConfigureAwait(false))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Home.AccountStatus.Icon = EFontAwesomeIcon.Solid_Check;
                    Home.AccountStatus.Foreground = new SolidColorBrush(
                        Color.FromRgb(50, 226, 178)
                    );
                });
                if (await Checks.CheckMatchAsync().ConfigureAwait(false))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Home.MatchStatus.Icon = EFontAwesomeIcon.Solid_Check;
                        Home.MatchStatus.Foreground = new SolidColorBrush(
                            Color.FromRgb(50, 226, 178)
                        );
                    });
                    CountTimer?.Stop();
                    GoMatchEvent?.Invoke();
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Home.MatchStatus.Icon = EFontAwesomeIcon.Solid_Xmark;
                        Home.MatchStatus.Foreground = new SolidColorBrush(
                            Color.FromRgb(255, 70, 84)
                        );
                    });
                    if (forcePartyUpdate)
                    {
                        _cycle++;
                        await GetPartyPlayerInfoAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        if (_cycle == 0)
                        {
                            await GetPartyPlayerInfoAsync().ConfigureAwait(false);
                            _cycle = 3;
                        }

                        _cycle--;
                    }
                }
            }
            else
            {
                try
                {
                    await LocalLoginAsync().ConfigureAwait(false);
                    await LocalRegionAsync().ConfigureAwait(false);
                    if (await Checks.CheckLoginAsync().ConfigureAwait(false))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Home.AccountStatus.Icon = EFontAwesomeIcon.Solid_Check;
                            Home.AccountStatus.Foreground = new SolidColorBrush(
                                Color.FromRgb(50, 226, 178)
                            );
                        });
                        if (await Checks.CheckMatchAsync().ConfigureAwait(false))
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                Home.MatchStatus.Icon = EFontAwesomeIcon.Solid_Check;
                                Home.MatchStatus.Foreground = new SolidColorBrush(
                                    Color.FromRgb(50, 226, 178)
                                );
                            });
                            CountTimer?.Stop();
                            GoMatchEvent?.Invoke();
                        }
                        else
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                Home.MatchStatus.Icon = EFontAwesomeIcon.Solid_Xmark;
                                Home.MatchStatus.Foreground = new SolidColorBrush(
                                    Color.FromRgb(255, 70, 84)
                                );
                            });
                            if (forcePartyUpdate)
                            {
                                _cycle++;
                                await GetPartyPlayerInfoAsync().ConfigureAwait(false);
                            }
                            else
                            {
                                if (_cycle == 0)
                                {
                                    await GetPartyPlayerInfoAsync().ConfigureAwait(false);
                                    _cycle = 3;
                                }

                                _cycle--;
                            }
                        }
                    }
                    else
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Home.AccountStatus.Icon = EFontAwesomeIcon.Solid_Xmark;
                            Home.AccountStatus.Foreground = new SolidColorBrush(
                                Color.FromRgb(255, 70, 84)
                            );
                            Home.MatchStatus.Icon = EFontAwesomeIcon.Solid_Xmark;
                            Home.MatchStatus.Foreground = new SolidColorBrush(
                                Color.FromRgb(255, 70, 84)
                            );
                        });
                    }
                }
                catch
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Home.AccountStatus.Icon = EFontAwesomeIcon.Solid_Xmark;
                        Home.AccountStatus.Foreground = new SolidColorBrush(
                            Color.FromRgb(255, 70, 84)
                        );
                        Home.MatchStatus.Icon = EFontAwesomeIcon.Solid_Xmark;
                        Home.MatchStatus.Foreground = new SolidColorBrush(
                            Color.FromRgb(255, 70, 84)
                        );
                    });
                }
            }
        }
        else
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Home.RiotStatus.Icon = EFontAwesomeIcon.Solid_Xmark;
                Home.RiotStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 70, 84));
                Home.ValorantStatus.Icon = EFontAwesomeIcon.Solid_Xmark;
                Home.ValorantStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 70, 84));
                Home.AccountStatus.Icon = EFontAwesomeIcon.Solid_Xmark;
                Home.AccountStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 70, 84));
                Home.MatchStatus.Icon = EFontAwesomeIcon.Solid_Xmark;
                Home.MatchStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 70, 84));
            });
        }
    }

    private async Task UpdateMatchInfoAsync()
    {
        try
        {
            // Note: This logic seems to be trying to get match info, but LiveMatch.GetMatchResponseAsync doesn't exist or isn't shown here.
            // Assuming we are adapting the logic from MatchViewModel to clean up the UI strings.
            // For HomeViewModel, we might be showing Party/PreGame info.

            // Since we can't easily access the full match object here without more changes, 
            // let's at least clean up the CurrentQueueName if it looks like a path.

            if (!string.IsNullOrEmpty(CurrentQueueName) && (CurrentQueueName.StartsWith("/Game/") || CurrentQueueName.Contains("/")))
            {
                if (CurrentQueueName.Contains("Swiftplay")) CurrentQueueName = "Swiftplay";
                else if (CurrentQueueName.Contains("Bomb")) CurrentQueueName = "Standard";
                else if (CurrentQueueName.Contains("SpikeRush")) CurrentQueueName = "Spike Rush";
                else if (CurrentQueueName.Contains("Deathmatch")) CurrentQueueName = "Deathmatch";
                else if (CurrentQueueName.Contains("OneForAll")) CurrentQueueName = "Replication";
                else if (CurrentQueueName.Contains("GunGame")) CurrentQueueName = "Escalation";
                else if (CurrentQueueName.Contains("HURM")) CurrentQueueName = "Team Deathmatch";
                else CurrentQueueName = "Custom";
            }
        }
        catch (Exception ex)
        {
            Constants.Log.Error(">>> [MATCH DATA] ERROR in UpdateMatchInfoAsync: {Error}", ex.ToString());
        }
    }
    [RelayCommand]
    private async Task GetPartyPlayerInfoAsync()
    {
        try
        {
            LiveMatch newLiveMatch = new();
            if (await newLiveMatch.CheckAndSetPartyIdAsync().ConfigureAwait(false))
            {
                var newPlayerList = await newLiveMatch.PartyOutputAsync().ConfigureAwait(false);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    PlayerList = newPlayerList;
                });
            }
        }
        catch (Exception)
        {
        }

        GC.Collect();
    }
}
