namespace SigaCean.Presentation.Views;

public partial class VoltarButton : Button
{
    public VoltarButton()
    {
        InitializeComponent();
    }

    private async void OnClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
