using Firebase.Database;
using Firebase.Database.Query;
using SkiaSharp;
using SkiaSharp.QrCode;
using Microsoft.Maui.Dispatching;
using Newtonsoft.Json.Linq;

namespace Bingo_DEF;

public partial class MainPage : ContentPage
{
    private string _idSala;
    private List<int> _bombo = Enumerable.Range(1, 90).ToList();
    private List<int> _ultimasBolas = new List<int>();
    private int _contadorBolas = 0;
    private readonly Random _rnd = new Random();
    private readonly IDispatcherTimer _timer;
    private readonly Dictionary<int, Border> _celdasTablero = new Dictionary<int, Border>();
    private IDisposable? _escuchaAvisos;

    private readonly FirebaseClient _fb = new FirebaseClient("https://bingov3-1ec3a-default-rtdb.europe-west1.firebasedatabase.app/");

    public MainPage(string idRecibido)
    {
        InitializeComponent();
        _idSala = idRecibido;
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(4);
        _timer.Tick += (s, e) => SacarBola();

        if (BtnVel1 != null) BtnVel1.BackgroundColor = Color.FromArgb("#C648FF");

        BuildBoard();
        _ = CargarVoces();
        IniciarEscuchaAvisos();
    }

    private void IniciarEscuchaAvisos()
    {
        _escuchaAvisos?.Dispose();
        _escuchaAvisos = _fb.Child("Salas").Child(_idSala).Child("Avisos")
           .AsObservable<JObject>()
           .Subscribe(notif => {
               if (notif?.Object != null)
               {
                   var nick = notif.Object["jugador"]?.ToString() ?? "Alguien";
                   var premioRaw = notif.Object["tipo"]?.ToString() ?? "Premio";
                   var premio = premioRaw.ToUpper().Trim();

                   MainThread.BeginInvokeOnMainThread(async () => {
                       _timer.Stop();

                       // AVISO POR VOZ
                       await TextToSpeech.Default.SpeakAsync($"Atención, {nick} ha cantado {premio}");

                       // MENSAJE DE VALIDACIÓN PERSONALIZADO
                       string textoAlerta = premio == "BINGO"
                           ? $"🔥 ¡{nick} dice tener BINGO! ¿Es correcto?"
                           : $"⭐ {nick} dice tener LÍNEA. ¿Es correcta?";

                       bool esValido = await this.DisplayAlert("VALIDACIÓN", textoAlerta, "SÍ", "NO");

                       if (esValido)
                           await MostrarAnimacionVictoria(nick, premio);
                       else
                       {
                           _timer.Start();
                           NotificationLabel.Text = "Esperando avisos...";
                       }
                   });
               }
           });
    }

    private async Task MostrarAnimacionVictoria(string nick, string premio)
    {
        string premioLimpio = premio.ToUpper().Trim();
        VictoryMessage.Text = $"¡{nick.ToUpper()} ha ganado {(premioLimpio == "BINGO" ? "el BINGO" : "la LÍNEA")}!";
        VictoryEmoji.Text = premioLimpio == "BINGO" ? "👑" : "⭐";

        // BOTÓN NUEVA PARTIDA SOLO EN BINGO
        BtnNuevaPartida.IsVisible = (premioLimpio == "BINGO");
        BtnSeguirPartida.WidthRequest = (premioLimpio == "BINGO") ? 200 : 350;

        VictoryOverlay.IsVisible = true;
        VictoryOverlay.Opacity = 0;
        await VictoryOverlay.FadeTo(1, 500);

        // VOZ DE CELEBRACIÓN
        await TextToSpeech.Default.SpeakAsync($"Felicidades {nick} por tu {premio}");
    }

    private void MarcarTablero(int n)
    {
        if (_celdasTablero.TryGetValue(n, out var b))
        {
            b.BackgroundColor = Color.FromArgb("#FFD700");
            if (b.Content is Label l) l.TextColor = Colors.Black;

            // NEÓN GLOW
            b.Shadow = new Shadow
            {
                Brush = new SolidColorBrush(Color.FromArgb("#FFD700")),
                Radius = 15,
                Opacity = 0.8f
            };

            // PARPADEO
            _ = b.ScaleTo(1.3, 300).ContinueWith(t => MainThread.BeginInvokeOnMainThread(() => b.ScaleTo(1.0, 300)));
        }
    }

    private async void SacarBola()
    {
        if (_bombo.Count == 0) return;
        int n = _bombo[_rnd.Next(_bombo.Count)];
        _bombo.Remove(n);
        _contadorBolas++;

        BallCounterLabel.Text = $"Bolas: {_contadorBolas}/90";
        CurrentNumberLabel.Text = n.ToString();
        MarcarTablero(n);
        ActualizarHistorial(n);

        await _fb.Child("Salas").Child(_idSala).Child("Ultima").PutAsync(n);

        if (VoicePicker.SelectedItem is Locale voz)
            await TextToSpeech.Default.SpeakAsync(n.ToString(), new SpeechOptions { Locale = voz });
    }

    private void ActualizarHistorial(int n)
    {
        _ultimasBolas.Insert(0, n);
        if (_ultimasBolas.Count > 5) _ultimasBolas.RemoveAt(5);
        BallHistoryContainer.Children.Clear();
        foreach (var bola in _ultimasBolas)
        {
            var b = new Border
            {
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 25 },
                BackgroundColor = Colors.White,
                WidthRequest = 55,
                HeightRequest = 55,
                Content = new Label { Text = bola.ToString(), TextColor = Colors.Black, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center, FontAttributes = FontAttributes.Bold, FontSize = 18 }
            };
            if (bola == n) b.Stroke = Color.FromArgb("#C648FF");
            BallHistoryContainer.Children.Add(b);
        }
    }

    private void BuildBoard()
    {
        BingoBoardGrid.Children.Clear();
        for (int i = 1; i <= 90; i++)
        {
            var b = new Border
            {
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                BackgroundColor = Color.FromArgb("#11White"),
                Content = new Label { Text = i.ToString(), TextColor = Color.FromArgb("#48CAE4"), HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center, FontAttributes = FontAttributes.Bold }
            };
            _celdasTablero[i] = b;
            BingoBoardGrid.Add(b, (i - 1) % 10, (i - 1) / 10);
        }
    }

    // EVENTOS DE BOTONES
    private void OnCloseVictoryClicked(object sender, EventArgs e) { VictoryOverlay.IsVisible = false; _timer.Start(); }
    private void OnVictoryResetClicked(object sender, EventArgs e) { VictoryOverlay.IsVisible = false; OnReiniciarClicked(sender, e); }
    private void OnStartClicked(object sender, EventArgs e) => _timer.Start();
    private void OnPauseClicked(object sender, EventArgs e) => _timer.Stop();
    private void OnCloseQRClicked(object sender, EventArgs e) => QRModal.IsVisible = false;
    private async void OnGenerateQRClicked(object sender, EventArgs e) { await GenerarCodigoQR(); QRModal.IsVisible = true; }

    private void SetVel1(object sender, EventArgs e) { _timer.Interval = TimeSpan.FromSeconds(4); ActualizarEstiloBotonesVelocidad((Button)sender); }
    private void SetVel1_5(object sender, EventArgs e) { _timer.Interval = TimeSpan.FromSeconds(2.6); ActualizarEstiloBotonesVelocidad((Button)sender); }
    private void SetVel2(object sender, EventArgs e) { _timer.Interval = TimeSpan.FromSeconds(2); ActualizarEstiloBotonesVelocidad((Button)sender); }

    private void ActualizarEstiloBotonesVelocidad(Button botonActivo)
    {
        BtnVel1.BackgroundColor = Color.FromArgb("#1E5B6E");
        BtnVel1_5.BackgroundColor = Color.FromArgb("#1E5B6E");
        BtnVel2.BackgroundColor = Color.FromArgb("#1E5B6E");
        botonActivo.BackgroundColor = Color.FromArgb("#C648FF");
    }

    private async void OnReiniciarClicked(object sender, EventArgs e)
    {
        _timer.Stop();
        _idSala = "SALA" + DateTime.Now.Ticks.ToString().Substring(10);
        _bombo = Enumerable.Range(1, 90).ToList();
        _ultimasBolas.Clear();
        _contadorBolas = 0;
        BuildBoard();
        await GenerarCodigoQR();
        QRModal.IsVisible = true;
    }

    private async Task GenerarCodigoQR()
    {
        string url = $"https://dpg210.github.io/bingo/?sala={_idSala}";
        byte[] imageBytes = await Task.Run(() => {
            var qr = QRCodeGenerator.CreateQrCode(url, ECCLevel.L);
            using var surface = SKSurface.Create(new SKImageInfo(300, 300));
            surface.Canvas.Clear(SKColors.White);
            surface.Canvas.Render(qr, 300, 300, SKColors.White, SKColors.Black);
            using var img = surface.Snapshot();
            return img.Encode(SKEncodedImageFormat.Png, 100).ToArray();
        });
        QRCodeImage.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
    }

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