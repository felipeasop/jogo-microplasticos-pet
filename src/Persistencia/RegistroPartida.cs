using CacaAosResiduos.Jogo;
using Godot;

namespace CacaAosResiduos.Persistencia;

// Grava a partida atual. Uma falha vai para o log e não interrompe quem está jogando.
public class RegistroPartida(BancoPartidas banco)
{
    private long? _idPartida;

    public void Iniciar(long? idParticipante, string idCenario, string versaoJogo, string versaoConteudo, int totalResiduos)
    {
        _idPartida = null;
        try
        {
            _idPartida = banco.Iniciar(idParticipante, idCenario, versaoJogo, versaoConteudo, totalResiduos);
        }
        catch (Exception erro) when (BancoLocal.EhFalhaDeGravacao(erro))
        {
            GD.PushError($"Falha ao registrar o início da partida: {erro.GetType().Name}");
        }
    }

    public void RegistrarColeta(string tipo, int escala, int pontos, double segundo) =>
        Gravar(id => banco.RegistrarColeta(id, tipo, escala, pontos, segundo));

    public void RegistrarDica(double segundo) => Gravar(id => banco.RegistrarDica(id, segundo));

    public void Encerrar(SituacaoPartida situacao, double segundosJogados)
    {
        Gravar(id => banco.Encerrar(id, situacao, segundosJogados));
        _idPartida = null;
    }

    private void Gravar(Action<long> gravar)
    {
        if (_idPartida is not { } id)
            return;
        try
        {
            gravar(id);
        }
        catch (Exception erro) when (BancoLocal.EhFalhaDeGravacao(erro))
        {
            GD.PushError($"Falha ao gravar a partida {id}: {erro.GetType().Name}");
        }
    }
}
