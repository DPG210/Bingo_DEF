namespace Bingo_DEF;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // 1. Generamos el ID automático de 6 caracteres al arrancar
        string idNuevaSala = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();

        // 2. Cargamos directamente la MainPage pasando ese ID
        // No usamos NavigationPage para que no haya barras superiores y sea pantalla completa
        return new Window(new MainPage(idNuevaSala));
    }
}