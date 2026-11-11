using Microsoft.Data.Sqlite;

namespace CacaAosResiduos.Persistencia;

public static class BancoLocal
{
    private const int VersaoEsquema = 2;

    // O SQLite não guarda essas opções no arquivo; cada conexão precisa ligá-las.
    public static SqliteConnection Abrir(string caminho)
    {
        var conexao = new SqliteConnection(TextoConexao(caminho));
        try
        {
            conexao.Open();
            Executar(conexao, null, "PRAGMA foreign_keys = ON; PRAGMA synchronous = FULL; PRAGMA journal_mode = DELETE;");
            return conexao;
        }
        catch
        {
            conexao.Dispose();
            throw;
        }
    }

    // Banco de outra versão não é apagado: é renomeado e um novo é criado. Devolve o nome guardado.
    public static string? Inicializar(string caminho)
    {
        using var conexao = Abrir(caminho);
        using var leitura = conexao.CreateCommand();
        leitura.CommandText = "PRAGMA user_version;";
        var versao = Convert.ToInt32(leitura.ExecuteScalar());
        if (versao == VersaoEsquema)
            return null;
        if (versao == 0)
        {
            using var transacao = conexao.BeginTransaction();
            Executar(conexao, transacao, Esquema);
            Executar(conexao, transacao, $"PRAGMA user_version = {VersaoEsquema};");
            transacao.Commit();
            return null;
        }
        leitura.Dispose();
        var guardado = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(caminho))!,
            $"{Path.GetFileNameWithoutExtension(caminho)}-esquema{versao}-{DateTime.Now:yyyyMMdd-HHmmss}{Path.GetExtension(caminho)}");
        conexao.Close();
        File.Move(caminho, guardado);
        if (File.Exists(caminho + "-journal"))
            File.Move(caminho + "-journal", guardado + "-journal");
        Inicializar(caminho);
        return guardado;
    }

    // O SQLite também lança InvalidOperationException em alguns erros de conexão.
    public static bool EhFalhaDeGravacao(Exception erro) =>
        erro is SqliteException or IOException or UnauthorizedAccessException or InvalidOperationException;

    public static bool VerificacaoRapida(string caminho)
    {
        using var conexao = Abrir(caminho);
        using var comando = conexao.CreateCommand();
        comando.CommandText = "PRAGMA quick_check;";
        return comando.ExecuteScalar() as string == "ok";
    }

    public static void FazerBackup(string caminho, string destino)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destino))!);
        using var origem = Abrir(caminho);
        using var copia = new SqliteConnection(TextoConexao(destino));
        copia.Open();
        origem.BackupDatabase(copia);
    }

    public static (long Participantes, long Partidas) Contagens(string caminho)
    {
        using var conexao = Abrir(caminho);
        using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT (SELECT count(*) FROM participantes), (SELECT count(*) FROM partidas);";
        using var leitor = comando.ExecuteReader();
        leitor.Read();
        return (leitor.GetInt64(0), leitor.GetInt64(1));
    }

    internal static void Executar(SqliteConnection conexao, SqliteTransaction? transacao, string sql)
    {
        using var comando = conexao.CreateCommand();
        comando.Transaction = transacao;
        comando.CommandText = sql;
        comando.ExecuteNonQuery();
    }

    internal static long ObterOuCriarId(SqliteConnection conexao, SqliteTransaction transacao, string sqlInsercao,
        string sqlBusca, params (string Nome, object Valor)[] parametros)
    {
        using var comando = conexao.CreateCommand();
        comando.Transaction = transacao;
        comando.CommandText = $"{sqlInsercao};\n{sqlBusca};";
        foreach (var (nome, valor) in parametros)
            comando.Parameters.AddWithValue(nome, valor);
        return (long)comando.ExecuteScalar()!;
    }

    private static string TextoConexao(string caminho) => new SqliteConnectionStringBuilder
    {
        DataSource = caminho,
        Mode = SqliteOpenMode.ReadWriteCreate,
        Pooling = false
    }.ToString();

    private const string Esquema = """
        CREATE TABLE cidades (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            nome TEXT NOT NULL UNIQUE CHECK(length(trim(nome)) BETWEEN 1 AND 100)
        );

        CREATE TABLE escolas (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            cidade_id INTEGER NOT NULL REFERENCES cidades(id),
            nome TEXT NOT NULL CHECK(length(trim(nome)) BETWEEN 1 AND 160),
            UNIQUE(cidade_id, nome)
        );

        CREATE TABLE etapas_ensino (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            nome TEXT NOT NULL UNIQUE CHECK(length(trim(nome)) BETWEEN 1 AND 80)
        );

        CREATE TABLE anos_serie (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            etapa_ensino_id INTEGER NOT NULL REFERENCES etapas_ensino(id),
            nome TEXT NOT NULL CHECK(length(trim(nome)) BETWEEN 1 AND 80),
            UNIQUE(etapa_ensino_id, nome)
        );

        CREATE TABLE participantes (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            escola_id INTEGER NOT NULL REFERENCES escolas(id),
            ano_serie_id INTEGER NOT NULL REFERENCES anos_serie(id),
            idade INTEGER NOT NULL CHECK(idade BETWEEN 1 AND 100),
            ja_jogou INTEGER NOT NULL CHECK(ja_jogou IN (0, 1))
        );

        CREATE TABLE cenarios (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            identificador TEXT NOT NULL UNIQUE CHECK(length(trim(identificador)) BETWEEN 1 AND 40)
        );

        CREATE TABLE versoes (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            versao_jogo TEXT NOT NULL CHECK(length(trim(versao_jogo)) BETWEEN 1 AND 40),
            versao_conteudo TEXT NOT NULL CHECK(length(trim(versao_conteudo)) BETWEEN 1 AND 80),
            UNIQUE(versao_jogo, versao_conteudo)
        );

        CREATE TABLE tipos_residuo (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            nome TEXT NOT NULL UNIQUE CHECK(length(trim(nome)) BETWEEN 1 AND 80)
        );

        CREATE TABLE partidas (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            participante_id INTEGER REFERENCES participantes(id),
            cenario_id INTEGER NOT NULL REFERENCES cenarios(id),
            versao_id INTEGER NOT NULL REFERENCES versoes(id),
            data_evento TEXT NOT NULL CHECK(data_evento GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]'),
            situacao TEXT NOT NULL CHECK(situacao IN ('em_andamento', 'completou', 'tempo_esgotado', 'abandonou', 'interrompida')),
            segundos_jogados REAL NOT NULL CHECK(segundos_jogados >= 0),
            total_residuos INTEGER NOT NULL CHECK(total_residuos > 0)
        );

        CREATE TABLE coletas (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            partida_id INTEGER NOT NULL REFERENCES partidas(id),
            tipo_residuo_id INTEGER NOT NULL REFERENCES tipos_residuo(id),
            escala INTEGER NOT NULL CHECK(escala BETWEEN 1 AND 4),
            pontos INTEGER NOT NULL CHECK(pontos >= 0),
            segundo_coleta REAL NOT NULL CHECK(segundo_coleta >= 0)
        );

        CREATE TABLE dicas (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            partida_id INTEGER NOT NULL REFERENCES partidas(id),
            segundo_dica REAL NOT NULL CHECK(segundo_dica >= 0)
        );

        CREATE INDEX escolas_por_cidade ON escolas(cidade_id);
        CREATE INDEX anos_por_etapa ON anos_serie(etapa_ensino_id);
        CREATE INDEX participantes_por_escola ON participantes(escola_id);
        CREATE INDEX participantes_por_ano ON participantes(ano_serie_id);
        CREATE INDEX partidas_por_participante ON partidas(participante_id);
        CREATE INDEX coletas_por_partida ON coletas(partida_id);
        CREATE INDEX dicas_por_partida ON dicas(partida_id);

        CREATE TRIGGER coleta_exige_partida_em_andamento BEFORE INSERT ON coletas
        WHEN (SELECT situacao FROM partidas WHERE id = NEW.partida_id) IS NOT 'em_andamento'
        BEGIN
            SELECT RAISE(ABORT, 'coleta em partida encerrada');
        END;

        CREATE TRIGGER coleta_dentro_do_total BEFORE INSERT ON coletas
        WHEN (SELECT count(*) FROM coletas WHERE partida_id = NEW.partida_id) >=
            (SELECT total_residuos FROM partidas WHERE id = NEW.partida_id)
        BEGIN
            SELECT RAISE(ABORT, 'coletas acima do total de resíduos');
        END;

        CREATE TRIGGER dica_exige_partida_em_andamento BEFORE INSERT ON dicas
        WHEN (SELECT situacao FROM partidas WHERE id = NEW.partida_id) IS NOT 'em_andamento'
        BEGIN
            SELECT RAISE(ABORT, 'dica em partida encerrada');
        END;

        CREATE VIEW resumo_partidas AS
        SELECT
            p.id AS partida_id,
            p.participante_id,
            c.identificador AS cenario,
            v.versao_jogo,
            v.versao_conteudo,
            p.data_evento,
            p.situacao,
            p.segundos_jogados,
            p.total_residuos,
            (SELECT count(*) FROM coletas WHERE partida_id = p.id) AS residuos_coletados,
            (SELECT coalesce(sum(pontos), 0) FROM coletas WHERE partida_id = p.id) AS pontuacao,
            (SELECT count(*) FROM dicas WHERE partida_id = p.id) AS dicas_usadas
        FROM partidas p
        JOIN cenarios c ON c.id = p.cenario_id
        JOIN versoes v ON v.id = p.versao_id;
        """;
}
