using System.Text.Json;

namespace BingoTop;

public static class PlayerSetupStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static string BuildKey(string salaId, string nick) => $"bingo_setup_{salaId}_{nick}";

    public static PlayerSetup? Load(string salaId, string nick)
    {
        var key = BuildKey(salaId, nick);
        var json = Preferences.Get(key, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<PlayerSetup>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public static void Save(string salaId, string nick, PlayerSetup setup)
    {
        var key = BuildKey(salaId, nick);
        Preferences.Set(key, JsonSerializer.Serialize(setup, JsonOptions));
    }
}
