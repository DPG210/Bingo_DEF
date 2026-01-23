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
                   var premio = notif.Object["tipo"]?.ToString() ?? "Premio";

                   MainThread.BeginInvokeOnMainThread(async () => {
                       _timer.Stop();
                       NotificationLabel.TextColor = Colors.Yellow;
                       NotificationLabel.Text = $"⚠️ {nick.ToUpper()} CANTA {premio} ⚠️";
                       await TextToSpeech.Default.SpeakAsync($"Atención, {nick} ha cantado {premio}");

                       bool esValido = await this.DisplayAlert("VALIDACIÓN", $"¿Es correcto el {premio} de {nick}?", "SÍ", "NO");

                       if (esValido)
                       {
                           await this.DisplayAlert("¡GANADOR!", $"El {premio} de {nick} ha sido validado.", "ACEPTAR");
                           NotificationLabel.TextColor = Colors.LimeGreen;
                           NotificationLabel.Text = $"¡{premio.ToUpper()} DE {nick.ToUpper()}!";
                       }
                       else
                       {
                           NotificationLabel.TextColor = Colors.White;
                           NotificationLabel.Text = "Esperando avisos...";
                           _timer.Start();
                       }
                   });
               }
           });
    }

    private async void OnReiniciarClicked(object sender, EventArgs e)
    {
        bool confirmar = await this.DisplayAlert("RESET", "¿Cerrar esta sala y crear una nueva?", "SÍ", "NO");
        if (!confirmar) return;

        _timer.Stop();
        _idSala = "SALA" + DateTime.Now.Ticks.ToString().Substring(10);
        _bombo = Enumerable.Range(1, 90).ToList();
        _ultimasBolas.Clear();
        _contadorBolas = 0;

        BallCounterLabel.Text = "Bolas: 0/90";
        NotificationLabel.Text = "NUEVA SALA GENERADA";
        NotificationLabel.TextColor = Colors.White;
        CurrentNumberLabel.Text = "--";
        BallHistoryContainer.Children.Clear();
        BuildBoard();

        await GenerarCodigoQR();
        IniciarEscuchaAvisos();
        QRModal.IsVisible = true;
    }

    private async Task GenerarCodigoQR()
    {
        try
        {
            string url = $"https://dpg210.github.io/bingo/?sala={_idSala}";
            byte[] imageBytes = await Task.Run(() => {
                var qrCodeData = QRCodeGenerator.CreateQrCode(url, ECCLevel.L);
                using var surface = SKSurface.Create(new SKImageInfo(300, 300));
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.White);
                canvas.Render(qrCodeData, 300, 300, SKColors.White, SKColors.Black);
                using var image = surface.Snapshot();
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                return data.ToArray();
            });
            QRCodeImage.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
        }
        catch (Exception ex) { await this.DisplayAlert("Error QR", ex.Message, "OK"); }
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
                Content = new Label
                {
                    Text = bola.ToString(),
                    TextColor = Colors.Black,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 18
                }
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

    private void ActualizarEstiloBotonesVelocidad(Button botonActivo)
    {
        BtnVel1.BackgroundColor = Color.FromArgb("#2D3265");
        BtnVel1_5.BackgroundColor = Color.FromArgb("#2D3265");
        BtnVel2.BackgroundColor = Color.FromArgb("#2D3265");
        botonActivo.BackgroundColor = Color.FromArgb("#C648FF");
    }

    private void OnStartClicked(object sender, EventArgs e) => _timer.Start();
    private void OnPauseClicked(object sender, EventArgs e) => _timer.Stop();
    private void OnCloseQRClicked(object sender, EventArgs e) => QRModal.IsVisible = false;
    private async void OnGenerateQRClicked(object sender, EventArgs e) { await GenerarCodigoQR(); QRModal.IsVisible = true; }
    private void SetVel1(object sender, EventArgs e) { _timer.Interval = TimeSpan.FromSeconds(4); ActualizarEstiloBotonesVelocidad((Button)sender); }
    private void SetVel1_5(object sender, EventArgs e) { _timer.Interval = TimeSpan.FromSeconds(2.6); ActualizarEstiloBotonesVelocidad((Button)sender); }
    private void SetVel2(object sender, EventArgs e) { _timer.Interval = TimeSpan.FromSeconds(2); ActualizarEstiloBotonesVelocidad((Button)sender); }

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