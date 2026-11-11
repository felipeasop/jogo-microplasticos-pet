using CacaAosResiduos.Cadastro;

namespace CacaAosResiduos.Persistencia;

public class BancoCadastros(string caminho)
{
    public string? Inicializar() => BancoLocal.Inicializar(caminho);

    public long Salvar(DadosCadastro cadastro)
    {
        ArgumentNullException.ThrowIfNull(cadastro);
        var erro = ValidacaoCadastro.Erro(cadastro);
        if (erro.Length > 0)
            throw new ArgumentException(erro, nameof(cadastro));

        using var conexao = BancoLocal.Abrir(caminho);
        using var transacao = conexao.BeginTransaction();
        var idCidade = BancoLocal.ObterOuCriarId(conexao, transacao,
            "INSERT INTO cidades(nome) VALUES ($nome) ON CONFLICT(nome) DO NOTHING",
            "SELECT id FROM cidades WHERE nome = $nome",
            ("$nome", Normalizar(cadastro.Cidade)));
        var idEscola = BancoLocal.ObterOuCriarId(conexao, transacao,
            "INSERT INTO escolas(cidade_id, nome) VALUES ($cidade, $nome) ON CONFLICT(cidade_id, nome) DO NOTHING",
            "SELECT id FROM escolas WHERE cidade_id = $cidade AND nome = $nome",
            ("$cidade", idCidade), ("$nome", Normalizar(cadastro.Escola)));
        var idEtapa = BancoLocal.ObterOuCriarId(conexao, transacao,
            "INSERT INTO etapas_ensino(nome) VALUES ($nome) ON CONFLICT(nome) DO NOTHING",
            "SELECT id FROM etapas_ensino WHERE nome = $nome",
            ("$nome", Normalizar(cadastro.EtapaEnsino)));
        var idAno = BancoLocal.ObterOuCriarId(conexao, transacao,
            "INSERT INTO anos_serie(etapa_ensino_id, nome) VALUES ($etapa, $nome) ON CONFLICT(etapa_ensino_id, nome) DO NOTHING",
            "SELECT id FROM anos_serie WHERE etapa_ensino_id = $etapa AND nome = $nome",
            ("$etapa", idEtapa), ("$nome", Normalizar(cadastro.AnoSerie)));

        using var comando = conexao.CreateCommand();
        comando.Transaction = transacao;
        comando.CommandText = """
            INSERT INTO participantes(escola_id, ano_serie_id, idade, ja_jogou)
            VALUES ($escola, $ano, $idade, $jaJogou);
            SELECT last_insert_rowid();
            """;
        comando.Parameters.AddWithValue("$escola", idEscola);
        comando.Parameters.AddWithValue("$ano", idAno);
        comando.Parameters.AddWithValue("$idade", cadastro.Idade);
        comando.Parameters.AddWithValue("$jaJogou", cadastro.JaJogou ? 1 : 0);
        var id = (long)comando.ExecuteScalar()!;
        transacao.Commit();
        return id;
    }

    private static string Normalizar(string valor) => valor.Trim().ToUpperInvariant();
}
