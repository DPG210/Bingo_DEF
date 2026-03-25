using System.Text.Json;

namespace BingoTop;

public partial class JugadorPage : ContentPage
{
    private readonly Random _rnd = new();
    private readonly string _salaId;
    private string _nick = string.Empty;
    private int _numCartones;
    private readonly List<FavoriteChoice> _favoritos = new();

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
        RestablecerFavoritosDisponibles();
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
        GuardarConfiguracion();
    }

    private async void OnJugarClicked(object sender, EventArgs e)
    {
        if (_numCartones <= 0)
        {
            await DisplayAlert("Cartones", "Elige cuántos cartones quieres.", "OK");
            return;
        }

        _favoritos.Clear();
        _favoritos.AddRange(LeerFavoritos());

        if (_favoritos.Count == 0)
        {
            await DisplayAlert("Favoritos", "Escribe al menos 1 número favorito y selecciónalo en un cartón.", "OK");
            return;
        }

        GuardarConfiguracion();
        GenerarCartones();
    }

    private void RestaurarConfiguracionGuardada()
    {
        var setup = PlayerSetupStore.Load(_salaId, _nick);
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

    private void GuardarConfiguracion()
    {
        if (string.IsNullOrWhiteSpace(_nick) || _numCartones <= 0)
            return;

        PlayerSetupStore.Save(_salaId, _nick, new PlayerSetup
        {
            NumCartones = _numCartones,
            Favoritos = LeerFavoritos().ToList()
        });
    }

    private IEnumerable<FavoriteChoice> LeerFavoritos()
    {
        for (int i = 1; i <= 2; i++)
        {
            var numeroTexto = i == 1 ? Fav1Number.Text?.Trim() : Fav2Number.Text?.Trim();
            if (string.IsNullOrWhiteSpace(numeroTexto))
                continue;

            if (!int.TryParse(numeroTexto, out var numero) || numero is < 1 or > 90)
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

    private void RestablecerFavoritosDisponibles()
    {
        if (_numCartones == 0)
            FavoritosStep.IsVisible = false;

        ActualizarDisponibilidadFavoritos();
        ActualizarEstiloCartones(_numCartones);
    }

    private void ActualizarDisponibilidadFavoritos()
    {
        void Update(CheckBox cb, int carton)
        {
            cb.IsEnabled = carton <= _numCartones;
            if (!cb.IsEnabled)
                cb.IsChecked = false;
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

        var cartones = BingoCartonGenerator.GenerateCartones(_numCartones, _favoritos, _rnd);
        for (int i = 0; i < cartones.Count; i++)
        {
            var cardGrid = CrearGridCarton();
            RellenarCarton(cardGrid, cartones[i]);

            var cardLayout = new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    new Label
                    {
                        Text = $"CARTÓN {i + 1}",
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