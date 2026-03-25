namespace BingoTop;

public sealed class FavoriteChoice
{
    public int Numero { get; set; }
    public List<int> Cartones { get; set; } = new();
}

public sealed class PlayerSetup
{
    public int NumCartones { get; set; }
    public List<FavoriteChoice> Favoritos { get; set; } = new();
}
