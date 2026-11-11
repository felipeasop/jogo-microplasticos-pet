namespace CacaAosResiduos.Jogo;

// Regras da partida, sem nós do Godot nem banco.
public class Partida
{
    private readonly HashSet<int> _idsColetados = [];
    private readonly Dictionary<string, int> _coletadosPorTipo = [];

    public IReadOnlyDictionary<string, int> ColetadosPorTipo => _coletadosPorTipo;
    public int TotalResiduos { get; private set; }
    public int ResiduosColetados => _idsColetados.Count;
    public int Pontuacao => ResiduosColetados * RegrasJogo.PontosPorColeta;
    public int DicasUsadas { get; private set; }
    public int DicasRestantes => RegrasJogo.MaximoDicas - DicasUsadas;
    public double SegundosRestantes { get; private set; }
    public double SegundosDecorridos { get; private set; }
    public bool EmAndamento { get; private set; }
    public SituacaoPartida? MotivoFim { get; private set; }

    public void Iniciar(int totalResiduos)
    {
        Limpar();
        TotalResiduos = totalResiduos;
        SegundosRestantes = RegrasJogo.LimiteSegundos;
        EmAndamento = true;
    }

    public bool Coletar(int idResiduo, string tipo)
    {
        if (!EmAndamento || idResiduo < 0 || idResiduo >= TotalResiduos || !_idsColetados.Add(idResiduo))
            return false;
        _coletadosPorTipo[tipo] = _coletadosPorTipo.GetValueOrDefault(tipo) + 1;
        if (ResiduosColetados == TotalResiduos)
            Encerrar(SituacaoPartida.Completou);
        return true;
    }

    public void Avancar(double delta)
    {
        if (!EmAndamento)
            return;
        var decorrido = Math.Min(Math.Max(0, delta), SegundosRestantes);
        SegundosDecorridos += decorrido;
        SegundosRestantes -= decorrido;
        if (SegundosRestantes <= 0)
            Encerrar(SituacaoPartida.TempoEsgotado);
    }

    public bool UsarDica()
    {
        if (!EmAndamento || DicasRestantes <= 0 || ResiduosColetados == TotalResiduos)
            return false;
        DicasUsadas++;
        SegundosRestantes = Math.Max(0, SegundosRestantes - RegrasJogo.PenalidadeDicaSegundos);
        if (SegundosRestantes <= 0)
            Encerrar(SituacaoPartida.TempoEsgotado);
        return true;
    }

    public void Limpar()
    {
        _idsColetados.Clear();
        _coletadosPorTipo.Clear();
        TotalResiduos = 0;
        DicasUsadas = 0;
        SegundosRestantes = 0;
        SegundosDecorridos = 0;
        EmAndamento = false;
        MotivoFim = null;
    }

    private void Encerrar(SituacaoPartida motivo)
    {
        EmAndamento = false;
        MotivoFim = motivo;
    }
}
