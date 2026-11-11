using Godot;

namespace CacaAosResiduos.Jogo;

public class Residuo(ResiduoSorteado sorteio, string tipo, ArteColetavel arte)
{
    public string Tipo => tipo;
    public ArteColetavel Arte => arte;
    public string IdVista => sorteio.IdVista;
    public Vector2 Centro => sorteio.Centro;
    public float TamanhoExibido => sorteio.TamanhoExibido;
    public float RaioToque => sorteio.RaioToque;
    public bool Espelhado => sorteio.Espelhado;
    public bool Coletado { get; set; }

    // Oscila um pouco para parecer boiar, sem sair da zona.
    public Vector2 CentroEm(double segundosDecorridos)
    {
        if (sorteio.AmplitudeFlutuacao <= 0)
            return Centro;
        var fase = (float)segundosDecorridos * 1.6f + sorteio.FaseFlutuacao;
        return Centro + new Vector2(Mathf.Sin(fase) * sorteio.AmplitudeFlutuacao,
            Mathf.Sin(fase * 1.3f) * sorteio.AmplitudeFlutuacao * 0.6f);
    }
}
