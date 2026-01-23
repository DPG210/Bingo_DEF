using Firebase.Database;
using Firebase.Database.Query;
using SkiaSharp;
using SkiaSharp.QrCode;
using Microsoft.Maui.Dispatching;
using Newtonsoft.Json.Linq;

namespace Bingo_DEF;

public partial class MainPage : ContentPage
{
    private readonly string _idSala;
    private List<int> _bombo = Enumerable.Range(1, 90).ToList();
    private int _contadorBolas = 0;
    private readonly Random _rnd = new();
    private readonly IDispatcherTimer _timer;
    private readonly Dictionary<int, Border> _celdasTablero = new();

    private readonly FirebaseClient _fb = new FirebaseClient("https://bingov3-1ec3a-default-rtdb.europe-west1.firebasedatabase.app/");

    public MainPage(string idRecibido)
    {
        InitializeComponent();
        _idSala = idRecibido;

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(4);
        _timer.Tick += (s, e) => SacarBola();

        // Marcamos la velocidad x1 por defecto al iniciar
        BtnVel1.BackgroundColor = Color.FromArgb("#00A3FF");

        BuildBoard();
        _ = CargarVoces();

        // ESCUCHA DE AVISOS
        _fb.Child("Salas").Child(_idSala).Child("Avisos")
           .AsObservable<JObject>()
           .Subscribe(notif => {
               if (notif.Object != null)
               {
                   var nick = notif.Object["jugador"]?.ToString() ?? "Alguien";
                   var premio = notif.Object["tipo"]?.ToString() ?? "Premio";

                   MainThread.BeginInvokeOnMainThread(async () => {
                       _timer.Stop();
                       NotificationLabel.TextColor = Colors.Yellow;
                       NotificationLabel.Text = $"⚠️ {nick.ToUpper()} CANTA {premio} ⚠️";

                       await TextToSpeech.Default.SpeakAsync($"Atención, {nick} ha cantado {premio}");

                       bool esValido = await DisplayAlert("VALIDACIÓN", $"¿Es correcto el {premio} de {nick}?", "SÍ, ¡GANADOR!", "NO, ERROR");

                       if (esValido)
                       {
                           NotificationLabel.TextColor = Colors.LimeGreen;
                           NotificationLabel.Text = $"¡FELICIDADES {nick.ToUpper()} POR TU {premio}!";
                           await TextToSpeech.Default.SpeakAsync($"¡Felicidades {nick}!");
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

    private void ActualizarEstiloBotonesVelocidad(Button botonActivo)
    {
        Color colorActivo = Color.FromArgb("#8000ff");
        Color colorInactivo = Color.FromArgb("#2D3265");

        BtnVel1.BackgroundColor = colorInactivo;
        BtnVel1_5.BackgroundColor = colorInactivo;
        BtnVel2.BackgroundColor = colorInactivo;

        botonActivo.BackgroundColor = colorActivo;
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
        _fb.Child("Salas").Child(_idSala).Child("Ultima").PutAsync(n);

        if (VoicePicker.SelectedItem is Locale voz)
            await TextToSpeech.Default.SpeakAsync(n.ToString(), new SpeechOptions { Locale = voz });
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

    private void OnStartClicked(object sender, EventArgs e) => _timer.Start();
    private void OnPauseClicked(object sender, EventArgs e) => _timer.Stop();
    private void OnReiniciarClicked(object sender, EventArgs e)
    {
        _timer.Stop();
        _bombo = Enumerable.Range(1, 90).ToList();
        _contadorBolas = 0;
        BallCounterLabel.Text = "Bolas: 0/90";
        NotificationLabel.Text = "Esperando avisos...";
        BuildBoard();
        CurrentNumberLabel.Text = "--";
    }

    private void OnGenerateQRClicked(object sender, EventArgs e)
    {
        string url = $"https://dpg210.github.io/bingo/?sala={_idSala}";
        var qrCode = QRCodeGenerator.CreateQrCode(url, ECCLevel.L);
        using var surface = SKSurface.Create(new SKImageInfo(300, 300));
        surface.Canvas.Clear(SKColors.White);
        surface.Canvas.Render(qrCode, 300, 300, SKColors.White, SKColors.Black);
        QRCodeImage.Source = ImageSource.FromStream(() => surface.Snapshot().Encode().AsStream());
        QRModal.IsVisible = true;
    }

    private void OnCloseQRClicked(object sender, EventArgs e) => QRModal.IsVisible = false;

    private void SetVel1(object sender, EventArgs e)
    {
        _timer.Interval = TimeSpan.FromSeconds(4);
        ActualizarEstiloBotonesVelocidad((Button)sender);
    }
    private void SetVel1_5(object sender, EventArgs e)
    {
        _timer.Interval = TimeSpan.FromSeconds(2.6);
        ActualizarEstiloBotonesVelocidad((Button)sender);
    }
    private void SetVel2(object sender, EventArgs e)
    {
        _timer.Interval = TimeSpan.FromSeconds(2);
        ActualizarEstiloBotonesVelocidad((Button)sender);
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