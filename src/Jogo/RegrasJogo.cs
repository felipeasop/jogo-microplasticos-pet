using Godot;

namespace CacaAosResiduos.Jogo;

public static class RegrasJogo
{
    public const int LimiteSegundos = 45;
    public const int PontosPorColeta = 10;
    public const int MaximoDicas = 5;
    public const int PenalidadeDicaSegundos = 3;
    public const int QuantidadeEscalas = 4;
    public const float RaioMinimoToque = 36;
    // Tela lógica: tudo é posicionado nela e escalado para a tela real.
    public static readonly Vector2 TamanhoTela = new(1280, 720);
    // Item menor que isso fica para a próxima escala.
    public const float TamanhoMinimoVisivel = 20;
    // Faixa livre entre a barra superior e o rodapé.
    public const float AreaJogoTopo = 68;
    public const float AreaJogoBase = 660;
    public const double PulsoDicaSegundos = 3.5;
    public const double TransicaoSegundos = 0.28;
    public const double AvisoSegundos = 4;
    public const double EfeitoColetaSegundos = 0.6;
    public const float ToleranciaMovimentoToque = 24;
    public const float RazaoPincaAmpliar = 1.18f;
    public const float RazaoPincaReduzir = 0.84f;

    public static string CenarioPorIdade(int idade) => idade switch
    {
        <= 12 => "facil",
        <= 16 => "medio",
        _ => "dificil"
    };
}
