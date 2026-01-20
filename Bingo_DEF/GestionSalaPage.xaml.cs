using Microsoft.Maui.Controls;

namespace Bingo_DEF;

public partial class GestionSalaPage : ContentPage
{
    public GestionSalaPage()
    {
        InitializeComponent();
    }

    private async void OnIniciarPartidaClicked(object sender, EventArgs e)
    {
        // Generamos un ID de 5 letras/números aleatorios
        string codigoUnico = Guid.NewGuid().ToString("N").Substring(0, 5).ToUpper();

        // OPCIONAL: Descomenta la línea de abajo para ver el ID antes de entrar
        // await DisplayAlert("Nueva Sala", "ID generado: " + codigoUnico, "OK");

        // IMPORTANTE: Pasamos 'codigoUnico' a la MainPage
        await Navigation.PushAsync(new MainPage(codigoUnico));
    }
}