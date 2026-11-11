using System.Globalization;
using CacaAosResiduos.Jogo;
using Microsoft.Data.Sqlite;

namespace CacaAosResiduos.Persistencia;

public class BancoPartidas(string caminho)
{
    // Só a data, sem horário: o horário permitiria cruzar a partida com o livro de visitas.
    public long Iniciar(long? idParticipante, string idCenario, string versaoJogo,
        string versaoConteudo, int totalResiduos)
    {
        using var conexao = BancoLocal.Abrir(caminho);
        using var transacao = conexao.BeginTransaction();
        var idCenarioBanco = BancoLocal.ObterOuCriarId(conexao, transacao,
            "INSERT INTO cenarios(identificador) VALUES ($identificador) ON CONFLICT(identificador) DO NOTHING",
            "SELECT id FROM cenarios WHERE identificador = $identificador",
            ("$identificador", idCenario));
        var idVersao = BancoLocal.ObterOuCriarId(conexao, transacao,
            """
            INSERT INTO versoes(versao_jogo, versao_conteudo) VALUES ($jogo, $conteudo)
            ON CONFLICT(versao_jogo, versao_conteudo) DO NOTHING
            """,
            "SELECT id FROM versoes WHERE versao_jogo = $jogo AND versao_conteudo = $conteudo",
            ("$jogo", versaoJogo), ("$conteudo", versaoConteudo));

        using var comando = conexao.CreateCommand();
        comando.Transaction = transacao;
        comando.CommandText = """
            INSERT INTO partidas(participante_id, cenario_id, versao_id, data_evento, situacao,
                segundos_jogados, total_residuos)
            VALUES ($participante, $cenario, $versao, $data, $situacao, 0, $total);
            SELECT last_insert_rowid();
            """;
        comando.Parameters.AddWithValue("$participante", (object?)idParticipante ?? DBNull.Value);
        comando.Parameters.AddWithValue("$cenario", idCenarioBanco);
        comando.Parameters.AddWithValue("$versao", idVersao);
        comando.Parameters.AddWithValue("$data", DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        comando.Parameters.AddWithValue("$situacao", Texto(SituacaoPartida.EmAndamento));
        comando.Parameters.AddWithValue("$total", totalResiduos);
        var id = (long)comando.ExecuteScalar()!;
        transacao.Commit();
        return id;
    }

    public void RegistrarColeta(long idPartida, string tipoResiduo, int escala, int pontos, double segundoColeta)
    {
        using var conexao = BancoLocal.Abrir(caminho);
        using var transacao = conexao.BeginTransaction();
        var idTipo = BancoLocal.ObterOuCriarId(conexao, transacao,
            "INSERT INTO tipos_residuo(nome) VALUES ($nome) ON CONFLICT(nome) DO NOTHING",
            "SELECT id FROM tipos_residuo WHERE nome = $nome",
            ("$nome", tipoResiduo));
        using var comando = conexao.CreateCommand();
        comando.Transaction = transacao;
        comando.CommandText = """
            INSERT INTO coletas(partida_id, tipo_residuo_id, escala, pontos, segundo_coleta)
            VALUES ($partida, $tipo, $escala, $pontos, $segundo);
            """;
        comando.Parameters.AddWithValue("$partida", idPartida);
        comando.Parameters.AddWithValue("$tipo", idTipo);
        comando.Parameters.AddWithValue("$escala", escala);
        comando.Parameters.AddWithValue("$pontos", pontos);
        comando.Parameters.AddWithValue("$segundo", segundoColeta);
        comando.ExecuteNonQuery();
        AtualizarSeEmAndamento(conexao, transacao, idPartida, SituacaoPartida.EmAndamento, segundoColeta);
        transacao.Commit();
    }

    public void RegistrarDica(long idPartida, double segundoDica)
    {
        using var conexao = BancoLocal.Abrir(caminho);
        using var transacao = conexao.BeginTransaction();
        using var comando = conexao.CreateCommand();
        comando.Transaction = transacao;
        comando.CommandText = "INSERT INTO dicas(partida_id, segundo_dica) VALUES ($partida, $segundo);";
        comando.Parameters.AddWithValue("$partida", idPartida);
        comando.Parameters.AddWithValue("$segundo", segundoDica);
        comando.ExecuteNonQuery();
        AtualizarSeEmAndamento(conexao, transacao, idPartida, SituacaoPartida.EmAndamento, segundoDica);
        transacao.Commit();
    }

    // Um encerramento nunca sobrescreve outro.
    public void Encerrar(long idPartida, SituacaoPartida situacao, double segundosJogados)
    {
        if (situacao == SituacaoPartida.EmAndamento)
            throw new ArgumentException("Encerrar exige uma situação final.", nameof(situacao));
        using var conexao = BancoLocal.Abrir(caminho);
        AtualizarSeEmAndamento(conexao, null, idPartida, situacao, segundosJogados);
    }

    // Na abertura do jogo, o que ficou em andamento foi cortado por queda de energia ou fechamento.
    public int MarcarInterrompidas()
    {
        using var conexao = BancoLocal.Abrir(caminho);
        using var comando = conexao.CreateCommand();
        comando.CommandText = "UPDATE partidas SET situacao = $interrompida WHERE situacao = $emAndamento;";
        comando.Parameters.AddWithValue("$interrompida", Texto(SituacaoPartida.Interrompida));
        comando.Parameters.AddWithValue("$emAndamento", Texto(SituacaoPartida.EmAndamento));
        return comando.ExecuteNonQuery();
    }

    public static string Texto(SituacaoPartida situacao) => situacao switch
    {
        SituacaoPartida.EmAndamento => "em_andamento",
        SituacaoPartida.Completou => "completou",
        SituacaoPartida.TempoEsgotado => "tempo_esgotado",
        SituacaoPartida.Abandonou => "abandonou",
        SituacaoPartida.Interrompida => "interrompida",
        _ => throw new ArgumentOutOfRangeException(nameof(situacao))
    };

    private static void AtualizarSeEmAndamento(SqliteConnection conexao, SqliteTransaction? transacao, long idPartida,
        SituacaoPartida situacao, double segundosJogados)
    {
        using var comando = conexao.CreateCommand();
        comando.Transaction = transacao;
        comando.CommandText = """
            UPDATE partidas SET situacao = $situacao, segundos_jogados = $segundos
            WHERE id = $partida AND situacao = $emAndamento;
            """;
        comando.Parameters.AddWithValue("$situacao", Texto(situacao));
        comando.Parameters.AddWithValue("$segundos", segundosJogados);
        comando.Parameters.AddWithValue("$partida", idPartida);
        comando.Parameters.AddWithValue("$emAndamento", Texto(SituacaoPartida.EmAndamento));
        comando.ExecuteNonQuery();
    }
}
