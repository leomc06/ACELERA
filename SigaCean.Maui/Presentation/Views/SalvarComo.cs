namespace SigaCean.Presentation.Views;

// Janela "Salvar como" do Windows, usada pelas telas que geram PDF.
public static class SalvarComo
{
    // Devolve o caminho escolhido, ou null se o usuario cancelar.
    public static async Task<string?> PdfAsync(string nomeSugerido)
    {
        var seletor = new Windows.Storage.Pickers.FileSavePicker
        {
            SuggestedFileName = Path.GetFileNameWithoutExtension(nomeSugerido),
            SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary
        };
        seletor.FileTypeChoices.Add("Documento PDF", [".pdf"]);

        var janela = Application.Current?.Windows[0].Handler?.PlatformView as Microsoft.UI.Xaml.Window;
        if (janela is null) return null;
        WinRT.Interop.InitializeWithWindow.Initialize(seletor, WinRT.Interop.WindowNative.GetWindowHandle(janela));

        var arquivo = await seletor.PickSaveFileAsync();
        return arquivo?.Path;
    }
}
