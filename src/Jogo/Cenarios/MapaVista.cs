using Godot;

namespace CacaAosResiduos.Jogo;

public partial class MapaVista : Node2D
{
    public IReadOnlyList<ZonaSorteio> ZonasSorteio { get; private set; } = [];

    public override void _Ready()
    {
        ZonasSorteio = [.. GetChildren().OfType<ZonaSorteio>()];
        // A foto do mapa serve só de referência no editor.
        if (!Engine.IsEditorHint())
            GetNode<Sprite2D>("EditorReference").Visible = false;
    }

    public void ValidarPara(Vista vista, Cenario cenario, IReadOnlyDictionary<string, TipoColetavel> coletaveis)
    {
        if (ZonasSorteio.Any(zona => string.IsNullOrWhiteSpace(zona.IdZona) || !float.IsFinite(zona.AmplitudeFlutuacao) || zona.AmplitudeFlutuacao < 0 ||
                !float.IsFinite(zona.FatorTamanhoFundo) || zona.FatorTamanhoFundo <= 0 ||
                !float.IsFinite(zona.FatorTamanhoFrente) || zona.FatorTamanhoFrente <= 0) ||
            ZonasSorteio.Select(zona => zona.IdZona).Distinct().Count() != ZonasSorteio.Count ||
            vista.RegrasSorteio.Any(regra => !ZonasSorteio.Any(zona => zona.IdZona == regra.Zona)))
            throw new InvalidOperationException($"Zona de geração inválida em {vista.Id}.");
        foreach (var zona in ZonasSorteio)
        {
            var poligono = zona.GetNodeOrNull<CollisionPolygon2D>("Polygon");
            if (poligono is null || poligono.Polygon.Length < 3 || poligono.Polygon.Any(ponto => !ponto.IsFinite()))
                throw new InvalidOperationException($"Polígono inválido em {vista.Id}/{zona.Name}.");
        }
        // No ponto mais distante da zona o resíduo ainda precisa ser visível.
        foreach (var regra in vista.RegrasSorteio)
        {
            var zona = ZonasSorteio.First(zona => zona.IdZona == regra.Zona);
            var menorFator = Math.Min(zona.FatorTamanhoFundo, zona.FatorTamanhoFrente) * cenario.MultiplicadorTamanho;
            foreach (var idItem in regra.Itens)
                if (coletaveis[idItem].Tamanho * menorFator < RegrasJogo.TamanhoMinimoVisivel)
                    throw new InvalidOperationException($"{idItem} fica menor que o mínimo visível na zona {regra.Zona} de {vista.Id}.");
        }
    }
}
