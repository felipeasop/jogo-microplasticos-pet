using Godot;

namespace CacaAosResiduos.Jogo;

public record ResiduoSorteado(string IdItem, string IdVista, Vector2 Centro, float TamanhoExibido,
    float AmplitudeFlutuacao, float FaseFlutuacao, bool Espelhado)
{
    public float RaioToque => Math.Max(RegrasJogo.RaioMinimoToque, TamanhoExibido * 0.5f);
}
