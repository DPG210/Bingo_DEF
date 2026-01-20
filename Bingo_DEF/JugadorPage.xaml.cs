namespace BingoTop;

public partial class JugadorPage : ContentPage
{
    private string sala;
    public JugadorPage(string salaId)
    {
        InitializeComponent();
        this.sala = salaId;
        GenerarCarton();
    }

    private void GenerarCarton()
    {
        var rnd = new Random();
        var usados = new HashSet<int>();
        CartonGrid.Children.Clear();

        for (int f = 0; f < 3; f++)
        {
            var filaNums = new List<int>();
            while (filaNums.Count < 5)
            {
                int n = rnd.Next(1, 91);
                if (usados.Add(n)) filaNums.Add(n);
            }
            filaNums.Sort();

            for (int c = 0; c < 5; c++)
            {
                var btn = CrearBotonBingo(filaNums[c].ToString());
                CartonGrid.Add(btn, c, f);
            }
        }
    }

    private Button CrearBotonBingo(string texto)
    {
        var btn = new Button
        {
            Text = texto,
            BackgroundColor = Color.FromArgb("#252A4E"),
            TextColor = Colors.White,
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
            HeightRequest = 70,
            Margin = 2
        };

        btn.Clicked += (s, e) =>
        {
            if (btn.BackgroundColor == Color.FromArgb("#FFD700"))
            {
                btn.BackgroundColor = Color.FromArgb("#252A4E");
                btn.TextColor = Colors.White;
            }
            else
            {
                btn.BackgroundColor = Color.FromArgb("#FFD700");
                btn.TextColor = Color.FromArgb("#0F1123");
            }
        };

        return btn;
    }

    private async void OnCantarBingo(object sender, EventArgs e)
    {
        await DisplayAlertAsync("Bingo", "¡Has cantado bingo!", "OK");
    }
}