using Godot;

namespace CacaAosResiduos.Jogo;

public static class SorteioResiduos
{
    private const int TentativasPorResiduo = 250;

    // A mesma semente gera sempre os mesmos resíduos.
    public static List<ResiduoSorteado> Sortear(Cenario cenario,
        IReadOnlyDictionary<string, MapaVista> mapas,
        IReadOnlyDictionary<string, TipoColetavel> coletaveis, ulong semente)
    {
        using var aleatorio = new RandomNumberGenerator { Seed = semente };
        var resultado = new List<ResiduoSorteado>();
        foreach (var vista in cenario.Vistas)
        {
            if (vista.RegrasSorteio.Count == 0)
                continue;

            var mapa = mapas[vista.Id];
            foreach (var regra in vista.RegrasSorteio)
            {
                var zona = mapa.ZonasSorteio.First(item => item.IdZona == regra.Zona);
                var itensDisponiveis = new List<string>(regra.Itens);
                for (var i = 0; i < regra.Quantidade; i++)
                {
                    if (itensDisponiveis.Count == 0)
                        itensDisponiveis.AddRange(regra.Itens);
                    var indiceItem = aleatorio.RandiRange(0, itensDisponiveis.Count - 1);
                    var idItem = itensDisponiveis[indiceItem];
                    itensDisponiveis.RemoveAt(indiceItem);
                    var tamanhoBase = coletaveis[idItem].Tamanho;
                    var posicionado = false;
                    for (var tentativa = 0; tentativa < TentativasPorResiduo; tentativa++)
                    {
                        var centro = zona.PontoAleatorio(aleatorio);
                        if (centro is null)
                            continue;
                        var centroTela = centro.Value;
                        var tamanho = zona.TamanhoEm(centroTela, tamanhoBase) * cenario.MultiplicadorTamanho;
                        if (!zona.ContemArea(centroTela, Math.Max(tamanho * 0.5f, RegrasJogo.RaioMinimoToque) + zona.AmplitudeFlutuacao) ||
                            !Cabe(centroTela, tamanho, vista.Id, cenario.EspacamentoSorteio, resultado))
                            continue;
                        resultado.Add(new ResiduoSorteado(idItem, vista.Id, centroTela, tamanho,
                            zona.AmplitudeFlutuacao, aleatorio.RandfRange(0, Mathf.Tau),
                            aleatorio.Randf() < 0.5f));
                        posicionado = true;
                        break;
                    }
                    if (!posicionado)
                        throw new InvalidOperationException(
                            $"Sem espaço para {idItem} na zona {regra.Zona} da vista {vista.Id}.");
                }
            }
        }
        return resultado;
    }

    private static bool Cabe(Vector2 centro, float tamanho, string idVista, float espacamento,
        List<ResiduoSorteado> residuos)
    {
        var raio = Math.Max(tamanho * 0.5f, RegrasJogo.RaioMinimoToque);
        if (centro.X < raio || centro.X > RegrasJogo.TamanhoTela.X - raio ||
            centro.Y < RegrasJogo.AreaJogoTopo + raio ||
            centro.Y > RegrasJogo.AreaJogoBase - raio)
            return false;
        return residuos.Where(residuo => residuo.IdVista == idVista).All(residuo =>
            centro.DistanceTo(residuo.Centro) >= raio + residuo.RaioToque + espacamento);
    }
}
