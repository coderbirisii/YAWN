using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using YAWN.Objects;

namespace YAWN.Helpers;

public static class BuddyTitleHelper
{
    public static async Task<string> GetPlayerTitleAsync(Guid titleId)
    {
        if (titleId == Guid.Empty)
            return "No Title";

        try
        {
            var titles = JsonSerializer.Deserialize<Dictionary<Guid, string>>(
                await File.ReadAllTextAsync(Constants.LocalAppDataPath + "\\ValAPI\\titles.json")
                    .ConfigureAwait(false)
            );
            titles.TryGetValue(titleId, out var title);
            return title ?? "No Title";
        }
        catch (Exception e)
        {
            Constants.Log.Error("GetPlayerTitleAsync failed: {e}", e);
            return "No Title";
        }
    }

    public static async Task<ValNameImage> GetBuddyInfoAsync(Guid? buddyLevelId)
    {
        ValNameImage defNI = new();
        
        if (buddyLevelId == null || buddyLevelId == Guid.Empty)
            return defNI;

        try
        {
            var buddies = JsonSerializer.Deserialize<Dictionary<Guid, ValNameImage>>(
                await File.ReadAllTextAsync(Constants.LocalAppDataPath + "\\ValAPI\\buddies.json")
                    .ConfigureAwait(false)
            );
            buddies.TryGetValue(buddyLevelId.Value, out var buddy);
            return buddy ?? defNI;
        }
        catch (Exception e)
        {
            Constants.Log.Error("GetBuddyInfoAsync failed: {e}", e);
            return defNI;
        }
    }
}
