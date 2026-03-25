using System.Text.Json;

namespace BingoTop;

public partial class JugadorPage : ContentPage
{
    private readonly Random _rnd = new();
    private readonly string _salaId;
    private int _numCartones;
    private string _nick = string.Empty;
    private readonly List<FavoriteChoice> _favoritos = new();

    private string SetupKey => $"bingo_setup_{_salaId}_{_nick}";

    private sealed class FavoriteChoice
    {
        public int Numero { get; set; }
        public List<int> Cartones { get; set; } = [];
    }

    private sealed class PlayerSetup
    {
        public int NumCartones { get; set; }
        public List<FavoriteChoice> Favoritos { get; set; } = [];
    }

    public JugadorPage(string salaId)
    {
        InitializeComponent();
        _salaId = salaId;
        SalaLabel.Text = $"SALA: {_salaId}";
    }

    private async void OnConfirmarNickClicked(object sender, EventArgs e)
    {
        _nick = NickEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_nick))
        {
            await DisplayAlert("Nick requerido", "Introduce tu nick para continuar.", "OK");
            return;
        }

        CartonesStep.IsVisible = true;
        FavoritosStep.IsVisible = false;
        CartonesContainer.Children.Clear();
        RestaurarConfiguracionGuardada();
    }

    private void OnSeleccionar1Carton(object sender, EventArgs e) => SeleccionarCartones(1);
    private void OnSeleccionar2Cartones(object sender, EventArgs e) => SeleccionarCartones(2);
    private void OnSeleccionar3Cartones(object sender, EventArgs e) => SeleccionarCartones(3);

    private void SeleccionarCartones(int cantidad)
    {
        _numCartones = cantidad;
        FavoritosStep.IsVisible = true;
        ActualizarEstiloCartones(cantidad);
        ActualizarDisponibilidadFavoritos();
        GuardarConfiguracionParcial();
    }

    private async void OnJugarClicked(object sender, EventArgs e)
    {
        if (_numCartones <= 0)
        {
            await DisplayAlert("Cartones", "Elige cuántos cartones quieres.", "OK");
            return;
        }

        _favoritos.Clear();
        foreach (var fav in LeerFavoritos())
            _favoritos.Add(fav);

        if (_favoritos.Count == 0)
        {
            await DisplayAlert("Favoritos", "Escribe al menos 1 número favorito y selecciónalo en un cartón.", "OK");
            return;
        }

        GuardarConfiguracionCompleta();
        GenerarCartones();
    }

    private void RestaurarConfiguracionGuardada()
    {
        var json = Preferences.Get(SetupKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            _numCartones = 0;
            ActualizarEstiloCartones(0);
            return;
        }

        var setup = JsonSerializer.Deserialize<PlayerSetup>(json);
        if (setup is null)
            return;

        _numCartones = setup.NumCartones;
        _favoritos.Clear();
        _favoritos.AddRange(setup.Favoritos);

        if (_numCartones > 0)
        {
            FavoritosStep.IsVisible = true;
            ActualizarEstiloCartones(_numCartones);
            ActualizarDisponibilidadFavoritos();
            RestaurarFavoritosEnUI();
        }
    }

    private void RestaurarFavoritosEnUI()
    {
        if (_favoritos.Count > 0)
        {
            Fav1Number.Text = _favoritos[0].Numero.ToString();
            Fav1C1.IsChecked = _favoritos[0].Cartones.Contains(1);
            Fav1C2.IsChecked = _favoritos[0].Cartones.Contains(2);
            Fav1C3.IsChecked = _favoritos[0].Cartones.Contains(3);
        }

        if (_favoritos.Count > 1)
        {
            Fav2Number.Text = _favoritos[1].Numero.ToString();
            Fav2C1.IsChecked = _favoritos[1].Cartones.Contains(1);
            Fav2C2.IsChecked = _favoritos[1].Cartones.Contains(2);
            Fav2C3.IsChecked = _favoritos[1].Cartones.Contains(3);
        }
    }

    private void GuardarConfiguracionParcial()
    {
        if (string.IsNullOrWhiteSpace(_nick) || _numCartones <= 0)
            return;

        var setup = new PlayerSetup
        {
            NumCartones = _numCartones,
            Favoritos = LeerFavoritos().ToList()
        };

        Preferences.Set(SetupKey, JsonSerializer.Serialize(setup));
    }

    private void GuardarConfiguracionCompleta()
    {
        var setup = new PlayerSetup
        {
            NumCartones = _numCartones,
            Favoritos = _favoritos
        };

        Preferences.Set(SetupKey, JsonSerializer.Serialize(setup));
    }

    private IEnumerable<FavoriteChoice> LeerFavoritos()
    {
        for (int i = 1; i <= 2; i++)
        {
            var numeroTexto = i == 1 ? Fav1Number.Text?.Trim() : Fav2Number.Text?.Trim();
            if (string.IsNullOrWhiteSpace(numeroTexto))
                continue;

            if (!int.TryParse(numeroTexto, out var numero) || numero < 1 || numero > 90)
                continue;

            var cartones = new List<int>();
            if (i == 1)
            {
                if (Fav1C1.IsChecked) cartones.Add(1);
                if (Fav1C2.IsChecked) cartones.Add(2);
                if (Fav1C3.IsChecked) cartones.Add(3);
            }
            else
            {
                if (Fav2C1.IsChecked) cartones.Add(1);
                if (Fav2C2.IsChecked) cartones.Add(2);
                if (Fav2C3.IsChecked) cartones.Add(3);
            }

            cartones = cartones.Where(c => c <= _numCartones).Distinct().ToList();
            if (cartones.Count == 0)
                continue;

            yield return new FavoriteChoice { Numero = numero, Cartones = cartones };
        }
    }

    private void ActualizarDisponibilidadFavoritos()
    {
        void Update(CheckBox cb, int carton)
        {
            cb.IsEnabled = carton <= _numCartones;
            if (!cb.IsEnabled) cb.IsChecked = false;
        }

        Update(Fav1C1, 1); Update(Fav1C2, 2); Update(Fav1C3, 3);
        Update(Fav2C1, 1); Update(Fav2C2, 2); Update(Fav2C3, 3);
    }

    private void ActualizarEstiloCartones(int cantidad)
    {
        BtnCarton1.BackgroundColor = cantidad == 1 ? Color.FromArgb("#e94560") : Color.FromArgb("#1E5B6E");
        BtnCarton2.BackgroundColor = cantidad == 2 ? Color.FromArgb("#e94560") : Color.FromArgb("#1E5B6E");
        BtnCarton3.BackgroundColor = cantidad == 3 ? Color.FromArgb("#e94560") : Color.FromArgb("#1E5B6E");
    }

    private void GenerarCartones()
    {
        CartonesContainer.Children.Clear();

        for (int i = 1; i <= _numCartones; i++)
        {
            var cardGrid = CrearGridCarton();
            var matriz = GenerarMatrizCarton(ObtenerNumerosForzados(i));
            RellenarCarton(cardGrid, matriz);

            var cardLayout = new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    new Label
                    {
                        Text = $"CARTÓN {i}",
                        TextColor = Colors.Gold,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalOptions = LayoutOptions.Center
                    },
                    cardGrid
                }
            };

            CartonesContainer.Children.Add(new Border
            {
                BackgroundColor = Color.FromArgb("#CC000000"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 },
                Padding = 10,
                Content = cardLayout
            });
        }
    }

    private List<int> ObtenerNumerosForzados(int carton)
    {
        return _favoritos
            .Where(f => f.Cartones.Contains(carton))
            .Select(f => f.Numero)
            .Distinct()
            .ToList();
    }

    private Grid CrearGridCarton()
    {
        var grid = new Grid
        {
            BackgroundColor = Color.FromArgb("#2c3e50"),
            Padding = 5,
            ColumnSpacing = 4,
            RowSpacing = 4,
            HeightRequest = 170
        };

        for (int r = 0; r < 3; r++)
            grid.RowDefinitions.Add(new RowDefinition());

        for (int c = 0; c < 9; c++)
            grid.ColumnDefinitions.Add(new ColumnDefinition());

        return grid;
    }

    private int?[,] GenerarMatrizCarton(List<int> forcedNumbers)
    {
        var forced = forcedNumbers.Where(n => n >= 1 && n <= 90).Distinct().ToList();

        for (int attempt = 0; attempt < 250; attempt++)
        {
            var matriz = new int?[3, 9];
            int[] rowCounts = [0, 0, 0];
            var columnCounts = new int[9];
            var pools = CrearPoolsNumericos(forced);
            var forcedOk = true;

            foreach (var numero in forced)
            {
                int col = ObtenerColumna(numero);
                var rows = Enumerable.Range(0, 3).Where(r => rowCounts[r] < 5 && matriz[r, col] is null).ToList();
                if (rows.Count == 0)
                {
                    forcedOk = false;
                    break;
                }

                int row = rows[_rnd.Next(rows.Count)];
                matriz[row, col] = numero;
                rowCounts[row]++;
                columnCounts[col]++;
            }

            if (!forcedOk)
                continue;

            var pendiente = new List<(int row, int col)>();
            for (int row = 0; row < 3; row++)
            {
                while (rowCounts[row] < 5)
                {
                    var candidates = Enumerable.Range(0, 9)
                        .Where(col => matriz[row, col] is null && columnCounts[col] < 3 && pools[col].Count > 0)
                        .ToList();

                    if (candidates.Count == 0)
                    {
                        forcedOk = false;
                        break;
                    }

                    int col = candidates[_rnd.Next(candidates.Count)];
                    matriz[row, col] = -1;
                    rowCounts[row]++;
                    columnCounts[col]++;
                    pendiente.Add((row, col));
                }

                if (!forcedOk)
                    break;
            }

            if (!forcedOk)
                continue;

            for (int col = 0; col < 9; col++)
            {
                var rows = Enumerable.Range(0, 3).Where(r => matriz[r, col] is not null).ToList();
                var fixedNumbers = rows
                    .Select(r => matriz[r, col])
                    .Where(v => v.HasValue && v.Value != -1)
                    .Select(v => v!.Value)
                    .OrderBy(v => v)
                    .ToList();

                var needed = rows.Count - fixedNumbers.Count;
                if (pools[col].Count < needed)
                {
                    forcedOk = false;
                    break;
                }

                var extras = pools[col].Take(needed).ToList();
                pools[col].RemoveRange(0, needed);
                var values = fixedNumbers.Concat(extras).OrderBy(v => v).ToList();

                for (int i = 0; i < rows.Count; i++)
                    matriz[rows[i], col] = values[i];
            }

            if (!forcedOk)
                continue;

            return matriz;
        }

        throw new InvalidOperationException("No se pudo generar el cartón.");
    }

    private List<int>[] CrearPoolsNumericos(List<int> forced)
    {
        return Enumerable.Range(0, 9)
            .Select(c =>
            {
                int min = c == 0 ? 1 : c * 10;
                int max = c == 8 ? 90 : (c * 10) + 9;
                return Enumerable.Range(min, max - min + 1)
                    .Where(n => !forced.Contains(n))
                    .OrderBy(_ => _rnd.Next())
                    .ToList();
            })
            .ToArray();
    }

    private int ObtenerColumna(int numero)
    {
        if (numero >= 1 && numero <= 9) return 0;
        if (numero >= 10 && numero <= 19) return 1;
        if (numero >= 20 && numero <= 29) return 2;
        if (numero >= 30 && numero <= 39) return 3;
        if (numero >= 40 && numero <= 49) return 4;
        if (numero >= 50 && numero <= 59) return 5;
        if (numero >= 60 && numero <= 69) return 6;
        if (numero >= 70 && numero <= 79) return 7;
        return 8;
    }

    private void RellenarCarton(Grid grid, int?[,] matriz)
    {
        grid.Children.Clear();

        for (int f = 0; f < 3; f++)
        {
            for (int c = 0; c < 9; c++)
            {
                var valor = matriz[f, c];
                if (valor is null)
                {
                    grid.Add(new Border
                    {
                        BackgroundColor = Color.FromArgb("#bdc3c7"),
                        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 4 }
                    }, c, f);
                    continue;
                }

                var numero = valor.Value;
                var btn = new Button
                {
                    Text = numero.ToString(),
                    BackgroundColor = Colors.White,
                    TextColor = Color.FromArgb("#333333"),
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 18,
                    CornerRadius = 4,
                    Padding = 0
                };

                bool marcado = false;
                btn.Clicked += (_, _) =>
                {
                    marcado = !marcado;
                    btn.Text = marcado ? "X" : numero.ToString();
                    btn.TextColor = marcado ? Color.FromArgb("#e94560") : Color.FromArgb("#333333");
                };

                grid.Add(btn, c, f);
            }
        }
    }

    private async void OnCantarBingo(object sender, EventArgs e)
    {
        await DisplayAlert("Bingo", "¡Has cantado bingo!", "OK");
    }
}