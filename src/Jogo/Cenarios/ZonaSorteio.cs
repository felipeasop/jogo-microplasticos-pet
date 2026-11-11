using Godot;

namespace CacaAosResiduos.Jogo;

// Superfície da foto onde resíduos podem aparecer, desenhada como polígono no editor.
[Tool]
public partial class ZonaSorteio : Area2D
{
    [Export] public string IdZona { get; set; } = "";
    [Export] public Color CorEditor { get; set; } = new(0.2f, 0.7f, 0.3f, 0.22f);
    [Export] public float AmplitudeFlutuacao { get; set; }
    // Tamanho relativo no fundo e na frente da zona, para imitar a perspectiva.
    [Export] public float FatorTamanhoFundo { get; set; } = 1;
    [Export] public float FatorTamanhoFrente { get; set; } = 1;

    private CollisionPolygon2D Poligono => GetNode<CollisionPolygon2D>("Polygon");
    private Transform2D ParaTela => Transform * Poligono.Transform;

    public float TamanhoEm(Vector2 centro, float tamanhoBase)
    {
        var paraTela = ParaTela;
        var pontos = Poligono.Polygon.Select(ponto => paraTela * ponto);
        var fundo = pontos.Min(ponto => ponto.Y);
        var frente = pontos.Max(ponto => ponto.Y);
        var profundidade = frente > fundo ? Mathf.Clamp((centro.Y - fundo) / (frente - fundo), 0, 1) : 0;
        return tamanhoBase * Mathf.Lerp(FatorTamanhoFundo, FatorTamanhoFrente, profundidade);
    }

    // Confere o quadrado inteiro em volta do centro, não só o centro.
    public bool ContemArea(Vector2 centro, float raio)
    {
        var poligono = Poligono.Polygon;
        var inversa = ParaTela.AffineInverse();
        for (var x = -1; x <= 1; x++)
            for (var y = -1; y <= 1; y++)
            {
                var ponto = centro + new Vector2(x, y) * raio;
                if (!Geometry2D.IsPointInPolygon(inversa * ponto, poligono))
                    return false;
            }
        return true;
    }

    public Vector2? PontoAleatorio(RandomNumberGenerator aleatorio)
    {
        var poligono = Poligono.Polygon;
        var limites = new Rect2(poligono[0], Vector2.Zero);
        foreach (var ponto in poligono)
            limites = limites.Expand(ponto);

        for (var tentativa = 0; tentativa < 100; tentativa++)
        {
            var local = new Vector2(
                aleatorio.RandfRange(limites.Position.X, limites.End.X),
                aleatorio.RandfRange(limites.Position.Y, limites.End.Y));
            if (Geometry2D.IsPointInPolygon(local, poligono))
                return ParaTela * local;
        }
        return null;
    }

    public override void _Draw()
    {
        if (!Engine.IsEditorHint())
            return;
        var poligono = GetNodeOrNull<CollisionPolygon2D>("Polygon");
        if (poligono is not null && poligono.Polygon.Length > 0)
            DrawColoredPolygon(poligono.Polygon, CorEditor);
    }
}
