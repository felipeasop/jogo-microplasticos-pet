using Godot;

namespace CacaAosResiduos.Jogo;

// Separa toque rápido de pinça. Uma pinça nunca termina como toque.
public class GestoToque
{
    private readonly Dictionary<int, Vector2> _posicoes = [];
    private readonly Dictionary<int, Vector2> _inicios = [];
    private bool _foiPinca;
    private bool _passoDado;
    private float _distanciaInicial;

    public void Pressionar(int indice, Vector2 posicao)
    {
        _posicoes[indice] = posicao;
        _inicios[indice] = posicao;
        if (_posicoes.Count == 2 && !_foiPinca)
        {
            _foiPinca = true;
            _distanciaInicial = DistanciaDedos();
        }
    }

    public Vector2? Soltar(int indice, Vector2 posicao, bool cancelado)
    {
        var ehToqueRapido = !cancelado && !_foiPinca && _posicoes.Count == 1 &&
            _inicios.TryGetValue(indice, out var inicio) && inicio.DistanceTo(posicao) <= RegrasJogo.ToleranciaMovimentoToque;
        _posicoes.Remove(indice);
        _inicios.Remove(indice);
        if (_posicoes.Count == 0) Limpar();
        return ehToqueRapido ? posicao : null;
    }

    // Um passo de escala por gesto.
    public void Arrastar(int indice, Vector2 posicao, Func<bool> ampliar, Action reduzir)
    {
        if (!_posicoes.ContainsKey(indice)) return;
        _posicoes[indice] = posicao;
        if (_posicoes.Count != 2 || _passoDado || _distanciaInicial <= 0) return;
        var distancia = DistanciaDedos();
        if (distancia > _distanciaInicial * RegrasJogo.RazaoPincaAmpliar)
            _passoDado = ampliar();
        else if (distancia < _distanciaInicial * RegrasJogo.RazaoPincaReduzir)
        {
            reduzir();
            _passoDado = true;
        }
    }

    private float DistanciaDedos() => _posicoes.Values.First().DistanceTo(_posicoes.Values.Last());

    public void Limpar()
    {
        _posicoes.Clear();
        _inicios.Clear();
        _foiPinca = false;
        _passoDado = false;
        _distanciaInicial = 0;
    }
}
