using TemplateNavegacaoMaui.Models;
using TemplateNavegacaoMaui.Services;

namespace TemplateNavegacaoMaui.Pages;

public partial class ContentPageTemplate : ContentPage
{
    private readonly IBatalhaService _batalhaService;
    private readonly OnibusStore _onibusStore;

    private bool _batalhaEmAndamento;

    public ContentPageTemplate(IBatalhaService batalhaService, OnibusStore onibusStore)
    {
        _batalhaService = batalhaService;
        _onibusStore = onibusStore;

        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        AtualizarStatus();
    }

    private void AtualizarStatus()
    {
        var disponiveis = _onibusStore.Onibus.Count(onibus => onibus.Pokemon != null);

        QuantidadeLabel.Text = $"{disponiveis} Pokémon disponíveis para batalha.";

        BatalharButton.IsEnabled = disponiveis >= 2 && !_batalhaEmAndamento;
    }

    private async void OnBatalharClicked(object sender, EventArgs e)
    {
        if (_batalhaEmAndamento)
            return;

        var disponiveis = _onibusStore.Onibus.Where(onibus => onibus.Pokemon != null).ToList();

        if (disponiveis.Count < 2)
        {
            await DisplayAlert(
                "Pokémon insuficientes",
                "É necessário ter pelo menos dois ônibus com Pokémon carregados.",
                "OK"
            );

            AtualizarStatus();
            return;
        }

        _batalhaEmAndamento = true;
        BatalharButton.IsEnabled = false;
        BatalharButton.Text = "Preparando batalha...";

        try
        {
            // Sorteia dois ônibus diferentes.
            int indiceA = Random.Shared.Next(disponiveis.Count);

            int indiceB;

            do
            {
                indiceB = Random.Shared.Next(disponiveis.Count);
            } while (indiceA == indiceB);

            var onibusA = disponiveis[indiceA];
            var onibusB = disponiveis[indiceB];

            // Calcula o resultado antes de abrir a página.
            var batalha = _batalhaService.SimularBatalha(onibusA, onibusB);

            await Shell.Current.GoToAsync(
                nameof(BatalhaPage),
                new Dictionary<string, object> { ["Batalha"] = batalha }
            );
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro ao iniciar batalha", ex.Message, "OK");
        }
        finally
        {
            _batalhaEmAndamento = false;
            BatalharButton.Text = "Sortear batalha";
            AtualizarStatus();
        }
    }
}
