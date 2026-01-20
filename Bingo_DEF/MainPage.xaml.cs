using Firebase.Database;
using Firebase.Database.Query;
using SkiaSharp;
using SkiaSharp.QrCode;
using Microsoft.Maui.Dispatching;

namespace Bingo_DEF;

public partial class MainPage : ContentPage
{
    private readonly string _idSalaActual;
    private List<int> _bombo = Enumerable.Range(1, 90).ToList();
    private int _contadorBolas = 0; // Nueva variable para contar
    private readonly Random _rnd = new();
    private readonly IDispatcherTimer _timer;
    private readonly Dictionary<int, Border> _celdasTablero = new();
    private const double TIEMPO_BASE = 4.0;

    private readonly FirebaseClient _fb = new FirebaseClient("https://bingov3-1ec3a-default-rtdb.europe-west1.firebasedatabase.app/");

    public MainPage(string idRecibido)
    {
        InitializeComponent();
        this._idSalaActual = idRecibido;

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(TIEMPO_BASE);
        _timer.Tick += (s, e) => SacarBola();

        BuildBoard();
        _ = CargarVoces();
        ActualizarEstiloBotones(BtnVel1);
    }

    private async void SacarBola()
    {
        if (_bombo.Count == 0) { _timer.Stop(); return; }

        int n = _bombo[_rnd.Next(_bombo.Count)];
        _bombo.Remove(n);

        // Actualizamos contador
        _contadorBolas++;
        BallCounterLabel.Text = $"Bolas: {_contadorBolas}/90";

        // Animación de rodado
        await CurrentNumberLabel.TranslateToAsync(100, 0, 200, Easing.CubicIn);
        CurrentNumberLabel.TranslationX = -100;
        CurrentNumberLabel.Text = n.ToString();
        await CurrentNumberLabel.TranslateToAsync(0, 0, 250, Easing.CubicOut);

        MarcarTablero(n);
        ActualizarHistorial(n);

        try
        {
            await _fb.Child("Salas").Child(_idSalaActual).Child("Ultima").PutAsync(n);
        }
        catch { }

        if (VoicePicker.SelectedItem is Locale voz)
            await TextToSpeech.Default.SpeakAsync(n.ToString(), new SpeechOptions { Locale = voz });
    }

    private void OnReiniciarClicked(object sender, EventArgs e)
    {
        _timer.Stop();
        _bombo = Enumerable.Range(1, 90).ToList();

        // Reset contador
        _contadorBolas = 0;
        BallCounterLabel.Text = "Bolas: 0/90";

        BuildBoard();
        BallHistoryContainer.Children.Clear();
        CurrentNumberLabel.Text = "--";
    }

    // --- MÉTODOS DE APOYO (SIN CAMBIOS) ---

    private void BuildBoard()
    {
        BingoBoardGrid.Children.Clear();
        _celdasTablero.Clear();
        for (int i = 1; i <= 90; i++)
        {
            var b = new Border
            {
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                BackgroundColor = Color.FromArgb("#121533"),
                Content = new Label { Text = i.ToString(), TextColor = Color.FromArgb("#5C6291"), HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center, FontAttributes = FontAttributes.Bold }
            };
            _celdasTablero[i] = b;
            BingoBoardGrid.Add(b, (i - 1) % 10, (i - 1) / 10);
        }
    }

    private void MarcarTablero(int n)
    {
        if (_celdasTablero.TryGetValue(n, out var b))
        {
            b.BackgroundColor = Color.FromArgb("#FFD700");
            if (b.Content is Label l) l.TextColor = Colors.Black;
        }
    }

    private async void ActualizarHistorial(int n)
    {
        var bola = new Border { WidthRequest = 40, HeightRequest = 40, StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 }, BackgroundColor = Colors.White, Content = new Label { Text = n.ToString(), TextColor = Colors.Black, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }, Opacity = 0 };
        BallHistoryContainer.Children.Insert(0, bola);
        await bola.FadeToAsync(1, 400);
    }

    private void OnStartClicked(object sender, EventArgs e) => _timer.Start();
    private void OnPauseClicked(object sender, EventArgs e) => _timer.Stop();
    private void SetVel1(object sender, EventArgs e) { _timer.Interval = TimeSpan.FromSeconds(4); ActualizarEstiloBotones(BtnVel1); }
    private void SetVel1_5(object sender, EventArgs e) { _timer.Interval = TimeSpan.FromSeconds(4 / 1.5); ActualizarEstiloBotones(BtnVel1_5); }
    private void SetVel2(object sender, EventArgs e) { _timer.Interval = TimeSpan.FromSeconds(2); ActualizarEstiloBotones(BtnVel2); }

    private void ActualizarEstiloBotones(Button btn)
    {
        BtnVel1.BackgroundColor = BtnVel1_5.BackgroundColor = BtnVel2.BackgroundColor = Color.FromArgb("#2D3265");
        btn.BackgroundColor = Color.FromArgb("#00A3FF");
    }

    private void OnGenerateQRClicked(object sender, EventArgs e)
    {
        string url = $"https://dpg210.github.io/bingo/?sala={_idSalaActual}";
        var qrCode = QRCodeGenerator.CreateQrCode(url, ECCLevel.L);
        using var surface = SKSurface.Create(new SKImageInfo(300, 300));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);
        canvas.Render(qrCode, 300, 300, SKColors.White, SKColors.Black);
        using var img = surface.Snapshot();
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        QRCodeImage.Source = ImageSource.FromStream(() => data.AsStream());
        QRModal.IsVisible = true;
    }

    private void OnCloseQRClicked(object sender, EventArgs e) => QRModal.IsVisible = false;

    private async Task CargarVoces()
    {
        try
        {
            var locales = await TextToSpeech.Default.GetLocalesAsync();
            VoicePicker.ItemsSource = locales.Where(l => l.Language.StartsWith("es")).ToList();
            if (VoicePicker.ItemsSource.Count > 0) VoicePicker.SelectedIndex = 0;
        }
        catch { }
    }
}