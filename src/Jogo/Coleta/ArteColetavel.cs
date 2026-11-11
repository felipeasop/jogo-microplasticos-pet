using Godot;

namespace CacaAosResiduos.Jogo;

public class ArteColetavel
{
    public Texture2D Textura { get; }
    public Rect2 RetanguloVisivel { get; }

    // Ignora as margens transparentes do PNG ao calcular o tamanho.
    public ArteColetavel(string caminho)
    {
        Textura = GD.Load<Texture2D>(caminho)
            ?? throw new InvalidOperationException($"Não foi possível carregar: {caminho}");
        using var imagem = Textura.GetImage();
        var usado = imagem.GetUsedRect();
        RetanguloVisivel = new Rect2(usado.Position, usado.Size);
    }
}
