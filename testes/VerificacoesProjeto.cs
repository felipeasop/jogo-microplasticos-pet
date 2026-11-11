using CacaAosResiduos.Cadastro;
using CacaAosResiduos.Dados;
using CacaAosResiduos.Jogo;
using CacaAosResiduos.Menu;
using CacaAosResiduos.Persistencia;
using Godot;
using Microsoft.Data.Sqlite;

namespace CacaAosResiduos.Testes;

// Roda no motor real, com SQLite real. Compila só em Debug.
public partial class VerificacoesProjeto : Node
{
    private int _aprovados;
    private int _falhas;

    public override async void _Ready()
    {
        try
        {
            GD.Print($"AMBIENTE: Godot {Engine.GetVersionInfo()["string"]} · .NET {System.Environment.Version}");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Verificar("catálogo em ordem alfabética, com OUTRA no fim e escola vinculada à cidade", VerificarBusca);
            Verificar("SQLite reutiliza relações, reabre e mantém integridade", VerificarBancoCadastros);
            Verificar("SQLite desfaz cadastro inválido inteiro", VerificarDesfazer);
            Verificar("SQLite rejeita entrada inválida antes de escrever", VerificarValidacaoBanco);
            Verificar("SQLite grava partida, coletas, dicas e interrupção", VerificarBancoPartidas);
            Verificar("banco de outra versão é guardado com outro nome e um novo é criado", VerificarVersaoEsquema);
            Verificar("backup do SQLite gera cópia íntegra", VerificarBackup);
            Verificar("segunda cópia do jogo não abre no mesmo banco", VerificarTravaBanco);
            Verificar("operador copia os dados com o jogo aberto e vê aviso de banco com problema", VerificarCopiaEAvisoBanco);
            Verificar("formulário vazio, parcial e reset", VerificarFormulario);
            Verificar("modo de teste oferece Campus da UENP, Parque do Povo e Hotel Yara", VerificarNiveisManuais);
            Verificar("idade escolhe a fase: 8 a 12, 13 a 16 e 17 ou mais", VerificarFasePorIdade);
            Verificar("cadastro completo inicia a fase da idade sem seleção", VerificarCadastroEscolheFase);
            Verificar("toque longo no título pula o cadastro", VerificarToqueLongoTitulo);
            Verificar("tela de regras mostra tempo, pontos e dicas de RegrasJogo", VerificarTextosRegras);
            Verificar("fácil mantém resolução para tela grande em todas as escalas", VerificarConteudoFacil);
            Verificar("médio e difícil mantêm detalhe por escala e mapas de superfície", VerificarImagensOutrosCenarios);
            Verificar("spawn mantém o resíduo inteiro dentro da superfície", VerificarAreaSorteio);
            Verificar("cantos da imagem não atravessam limite diagonal da zona", VerificarCantosArea);
            Verificar("profundidade do cenário altera tamanho sem reduzir área de toque", VerificarPerspectiva);
            Verificar("UENP não gera resíduos na placa, no marco nem na estrada", VerificarExclusoesSuperficie);
            Verificar("todo microscópio inclui microplásticos dentro da área de jogo", VerificarMicroplasticosMicroscopio);
            Verificar("sementes repetem alvos e respeitam limites", VerificarSorteio);
            Verificar("sorteio sem espaço tenta outra semente em vez de falhar a partida", VerificarSorteioSemEspaco);
            Verificar("posição do mapa não altera canvas da geração", VerificarCoordenadasMapa);
            Verificar("conteúdo rejeita referência de zoom inexistente", VerificarLigacoesConteudo);
            Verificar("conteúdo rejeita imagem sem tamanho e mensagem", VerificarColetaveisConteudo);
            Verificar("coletáveis usam imagens fixas na escala correspondente", VerificarEscalaColetaveis);
            Verificar("mapas validam zonas e destinos", VerificarValidacaoMapa);
            Verificar("dica de outra vista orienta ampliar ou reduzir", VerificarNavegacaoDica);
            Verificar("dica na vista atual aponta a região sem revelar o resíduo", VerificarDicaMesmaVista);
            Verificar("dica em vista mais ampla orienta reduzir sem nomear a camada", VerificarDicaReduzir);
            Verificar("sessão bloqueia ações após término e limpa totais", VerificarPartida);
            Verificar("coleta pontua uma vez e termina ao coletar todos", VerificarColetas);
            Verificar("tempo e cinco dicas mantêm penalidade", VerificarTempoEDicas);
            Verificar("zoom pela roda preserva hierarquia", VerificarZoom);
            Verificar("soltar pinça não coleta", VerificarPinca);
            Verificar("pinça muda uma camada por gesto e pode reduzir", VerificarZoomPinca);
            Verificar("toque cancelado não coleta", VerificarToqueCancelado);
            Verificar("roda sobre HUD não altera camada", VerificarRodaHud);
            Verificar("toque no aviso da dica não coleta resíduo oculto", VerificarToqueNoAvisoNaoColeta);
            Verificar("teclado virtual reinicia inatividade", VerificarAtividadeTeclado);
            Verificar("duplo início não reinicia uma partida em andamento", VerificarInicioDuplicado);
            Verificar("partidas completas, abandonadas, por tempo e interrompidas são gravadas", VerificarGravacaoPartidas);
            Verificar("campos do cadastro com o mesmo visual, lista e altura", VerificarCamposCadastro);
            Verificar("texto dos botões tem contraste de pelo menos 4,5", VerificarContrasteBotoes);
            Verificar("interface mantém textos em caixa alta e contorno nos textos brancos", VerificarApresentacaoTexto);
            await VerificarSeletorLocal();
            await VerificarBotaoToque();
            await VerificarCliqueDoToque();
            await VerificarRolagemResultado();
            if (OS.GetCmdlineUserArgs().Contains("--preview")) await CapturarPrevias();
        }
        catch (Exception erro)
        {
            _falhas++;
            GD.PrintErr($"FAIL execução: {erro}");
        }
        if (Directory.Exists(s_pastaBancoTelaPrincipal)) Directory.Delete(s_pastaBancoTelaPrincipal, true);
        GD.Print($"RESULTADO: {_aprovados} aprovados, {_falhas} falhas");
        GetTree().Quit(_falhas == 0 ? 0 : 1);
    }

    private void Verificar(string nome, Action acao)
    {
        try { acao(); _aprovados++; GD.Print($"PASS {nome}"); }
        catch (Exception erro) { _falhas++; GD.PrintErr($"FAIL {nome}: {erro}"); }
    }

    private static void Exigir(bool condicao, string mensagem)
    {
        if (!condicao) throw new InvalidOperationException(mensagem);
    }

    private static readonly string s_pastaBancoTelaPrincipal =
        Path.Combine(Path.GetTempPath(), "caca-residuos-main-" + Guid.NewGuid());
    private static readonly string[] s_todosOsCenarios = ["facil", "medio", "dificil"];

    // Sempre com banco temporário, nunca o do operador.
    private static TelaPrincipal NovaTelaPrincipal(string? caminhoBanco = null)
    {
        Directory.CreateDirectory(s_pastaBancoTelaPrincipal);
        var telaPrincipal = GD.Load<PackedScene>("res://scenes/TelaPrincipal.tscn").Instantiate<TelaPrincipal>();
        telaPrincipal.CaminhoBanco = caminhoBanco ?? Path.Combine(s_pastaBancoTelaPrincipal, "main.sqlite3");
        return telaPrincipal;
    }

    private static void ComPastaTemporaria(Action<string> acao)
    {
        var pasta = Path.Combine(Path.GetTempPath(), "caca-residuos-" + Guid.NewGuid());
        Directory.CreateDirectory(pasta);
        try { acao(pasta); }
        finally { Directory.Delete(pasta, true); }
    }

    private static Button[] LinhasDaLista(FormularioCadastro formulario) =>
        [.. formulario.GetNode("%SelectionList/Scroll/Options").GetChildren().OfType<Button>()];

    private static void Escolher(FormularioCadastro formulario, string campo, int linha = 0)
    {
        formulario.GetNode<Button>(campo).EmitSignal(BaseButton.SignalName.Pressed);
        LinhasDaLista(formulario)[linha].EmitSignal(BaseButton.SignalName.Pressed);
    }

    private static DadosCadastro Ficticio(int idade = 12) =>
        new("CIDADE FICTÍCIA", "ESCOLA FICTÍCIA", "ENSINO FUNDAMENTAL", "7º ANO", idade, false);

    private static void VerificarBusca()
    {
        var busca = new BuscaEscolas(new CatalogoEscolas
        {
            Cidades = [
                new() { Nome = "ZETA FICTÍCIA", Escolas = ["ESCOLA ZETA", "ESCOLA ÁGUA EFM", "ESCOLA BETA"] },
                new() { Nome = "ÁGUA FICTÍCIA", Escolas = ["ESCOLA ÚNICA"] }
            ]
        });
        Exigir(busca.Cidades().SequenceEqual(["ÁGUA FICTÍCIA", "ZETA FICTÍCIA", BuscaEscolas.OutraCidade]), "cidades fora de ordem ou sem OUTRA");
        Exigir(busca.Escolas("ZETA FICTÍCIA").Select(item => item.Escola).SequenceEqual(
            ["ESCOLA ÁGUA — ENSINO FUNDAMENTAL E MÉDIO", "ESCOLA BETA", "ESCOLA ZETA", BuscaEscolas.ComunidadeExterna, BuscaEscolas.OutraEscola]),
            "escolas fora de ordem, sem expansão, sem COMUNIDADE EXTERNA ou sem OUTRA");
        Exigir(busca.Escolas(BuscaEscolas.OutraCidade).SequenceEqual([
                new SugestaoEscola(BuscaEscolas.ComunidadeExterna, BuscaEscolas.OutraCidade),
                new SugestaoEscola(BuscaEscolas.OutraEscola, BuscaEscolas.OutraCidade)]),
            "OUTRA CIDADE deveria ter só COMUNIDADE EXTERNA e OUTRA ESCOLA");
        Exigir(busca.Escolas().Count == 10 && busca.Escolas().All(item => item.Cidade.Length > 0), "lista completa perdeu escola ou cidade");
    }

    private async Task VerificarSeletorLocal()
    {
        const string nomeTeste = "listas completas, rolagem por toque e seleção correta";
        var telaPrincipal = NovaTelaPrincipal();
        AddChild(telaPrincipal);
        try
        {
            telaPrincipal.GetNode<Button>("%PlayButton").EmitSignal(BaseButton.SignalName.Pressed);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var formulario = telaPrincipal.GetNode<FormularioCadastro>("%FormularioCadastro");
            var cidade = formulario.GetNode<Button>("CityInput");
            var escola = formulario.GetNode<Button>("SchoolInput");
            var seletor = telaPrincipal.GetNode<Control>("%SelectionList");
            var catalogo = CatalogoEscolas.Carregar();
            var busca = new BuscaEscolas(catalogo);
            Button[] Linhas() => [.. seletor.FindChildren("*", "Button", true, false).OfType<Button>()];
            async Task TocarEm(Vector2 ponto)
            {
                GetViewport().PushInput(new InputEventScreenTouch { Index = 0, Pressed = true, Position = ponto }, true);
                GetViewport().PushInput(new InputEventScreenTouch { Index = 0, Position = ponto }, true);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            Vector2 Centro(Control controle) => controle.GlobalPosition + controle.Size / 2;

            await TocarEm(Centro(escola));
            Exigir(seletor.Visible && Linhas().Length == busca.Escolas().Count, "lista de escolas incompleta");
            var rolagem = seletor.GetNode<ScrollContainer>("Scroll");
            var ponto = rolagem.GlobalPosition + new Vector2(100, 100);
            GetViewport().PushInput(new InputEventScreenTouch { Index = 0, Pressed = true, Position = ponto }, true);
            for (var passo = 0; passo < 15; passo++)
            {
                ponto.Y -= 8;
                GetViewport().PushInput(new InputEventScreenDrag { Index = 0, Position = ponto, Relative = new Vector2(0, -8) }, true);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            GetViewport().PushInput(new InputEventScreenTouch { Index = 0, Position = ponto }, true);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Exigir(rolagem.ScrollVertical > 0 && escola.Text == "TOQUE PARA ESCOLHER", "arrastar não rolou a lista ou selecionou uma escola");
            var linhaVisivel = Linhas().First(linha => rolagem.GetGlobalRect().Encloses(linha.GetGlobalRect()));
            var textoTocado = linhaVisivel.Text.Split(" · ");
            await TocarEm(Centro(linhaVisivel));
            Exigir(escola.Text == textoTocado[0] && cidade.Text == textoTocado[1] && !seletor.Visible, "toque não selecionou a escola correta");

            await TocarEm(Centro(cidade));
            Exigir(Linhas().Select(linha => linha.Text).SequenceEqual(busca.Cidades()), "lista ou ordem das cidades incorreta");
            Linhas()[0].EmitSignal(BaseButton.SignalName.Pressed);
            var cidadeEscolhida = cidade.Text;
            Exigir(escola.Text == "TOQUE PARA ESCOLHER" && !seletor.Visible, "trocar a cidade não apagou a escola");

            await TocarEm(Centro(escola));
            var esperadas = busca.Escolas(cidadeEscolhida).Select(item => item.Escola.ToUpperInvariant());
            Exigir(Linhas().Select(linha => linha.Text).SequenceEqual(esperadas), "lista não filtrou as escolas pela cidade");
            await TocarEm(new Vector2(10, 10));
            Exigir(!seletor.Visible, "toque fora não fechou a lista");
            Verificar(nomeTeste, () => Exigir(true, ""));
        }
        catch (Exception erro) { Verificar(nomeTeste, () => throw erro); }
        finally { telaPrincipal.Free(); }
    }

    private static void ComBanco(Action<BancoCadastros, string> acao) => ComPastaTemporaria(pasta =>
    {
        var caminho = Path.Combine(pasta, "fictional.sqlite3");
        var bancoCadastros = new BancoCadastros(caminho);
        bancoCadastros.Inicializar();
        acao(bancoCadastros, caminho);
    });

    private static object? Valor(string caminho, string sql)
    {
        using var conexao = new SqliteConnection($"Data Source={caminho};Pooling=False");
        conexao.Open();
        using var comando = conexao.CreateCommand();
        comando.CommandText = sql;
        return comando.ExecuteScalar();
    }

    private static long ValorInteiro(string caminho, string sql) => (long)Valor(caminho, sql)!;

    private static void VerificarBancoCadastros() => ComBanco((bancoCadastros, caminho) =>
    {
        var primeiro = bancoCadastros.Salvar(Ficticio());
        for (var i = 1; i < 400; i++) bancoCadastros.Salvar(Ficticio() with { Cidade = " cidade fictícia " });
        var reaberta = new BancoCadastros(caminho);
        reaberta.Inicializar();
        Exigir(primeiro == 1 && ValorInteiro(caminho, "SELECT count(*) FROM participantes") == 400, "cadastros perdidos");
        Exigir(ValorInteiro(caminho, "SELECT count(*) FROM cidades") == 1 && ValorInteiro(caminho, "SELECT count(*) FROM escolas") == 1, "relações duplicadas");
        Exigir(ValorInteiro(caminho, "SELECT count(*) FROM pragma_foreign_key_check") == 0, "vínculo inválido");
        Exigir(ValorInteiro(caminho, "SELECT count(*) FROM pragma_integrity_check WHERE integrity_check != 'ok'") == 0, "banco corrompido");
    });

    private static void VerificarDesfazer() => ComBanco((bancoCadastros, caminho) =>
    {
        Valor(caminho, "CREATE TRIGGER reject_test BEFORE INSERT ON participantes BEGIN SELECT RAISE(ABORT, 'fictional test'); END;");
        try { bancoCadastros.Salvar(Ficticio()); throw new Exception("trigger não impediu escrita"); }
        catch (SqliteException) { }
        Exigir(ValorInteiro(caminho, "SELECT count(*) FROM cidades") == 0 &&
            ValorInteiro(caminho, "SELECT count(*) FROM escolas") == 0 &&
            ValorInteiro(caminho, "SELECT count(*) FROM participantes") == 0, "transação deixou cadastro parcial");
    });

    private static void VerificarValidacaoBanco() => ComBanco((bancoCadastros, caminho) =>
    {
        foreach (var invalido in new[] { Ficticio() with { Cidade = "   " }, Ficticio(idade: 0) })
        {
            try { bancoCadastros.Salvar(invalido); throw new Exception("entrada inválida foi salva"); }
            catch (ArgumentException) { }
        }
        Exigir(ValorInteiro(caminho, "SELECT count(*) FROM cidades") == 0, "entrada inválida criou cidade");
    });

    private static void VerificarBancoPartidas() => ComBanco((bancoCadastros, caminho) =>
    {
        var participante = bancoCadastros.Salvar(Ficticio());
        bancoCadastros.Salvar(Ficticio());
        Exigir(ValorInteiro(caminho, "SELECT count(*) FROM etapas_ensino") == 1 &&
            ValorInteiro(caminho, "SELECT count(*) FROM anos_serie") == 1, "etapa ou ano duplicados");

        var bancoPartidas = new BancoPartidas(caminho);
        var completa = bancoPartidas.Iniciar(participante, "facil", "0.1.0", "conteudo-ficticio", 3);
        bancoPartidas.RegistrarColeta(completa, "GARRAFA PLÁSTICA", 1, 10, 2.5);
        bancoPartidas.RegistrarDica(completa, 4);
        bancoPartidas.RegistrarColeta(completa, "TAMPINHA", 2, 10, 6.25);
        bancoPartidas.RegistrarColeta(completa, "MICROPLÁSTICOS", 3, 10, 9.25);
        try { bancoPartidas.RegistrarColeta(completa, "PAPEL", 1, 10, 9.5); throw new Exception("coleta acima do total aceita"); }
        catch (SqliteException) { }
        bancoPartidas.Encerrar(completa, SituacaoPartida.Completou, 9.25);
        bancoPartidas.Encerrar(completa, SituacaoPartida.Abandonou, 20);
        string Resumo(string colunas) => (string)Valor(caminho, $"SELECT {colunas} FROM resumo_partidas WHERE partida_id = {completa}")!;
        Exigir(Resumo("situacao") == "completou", "estado final sobrescrito");
        Exigir(Resumo("participante_id || '|' || cenario || '|' || versao_jogo || '|' || versao_conteudo") ==
            $"{participante}|facil|0.1.0|conteudo-ficticio", "identificação da partida incorreta");
        Exigir(Resumo("total_residuos || '|' || residuos_coletados || '|' || pontuacao || '|' || dicas_usadas || '|' || segundos_jogados") ==
            "3|3|30|1|9.25", "totais da partida incorretos");
        Exigir(ValorInteiro(caminho, $"SELECT count(*) FROM coletas c JOIN tipos_residuo t ON t.id = c.tipo_residuo_id " +
            $"WHERE c.partida_id = {completa} AND c.escala = 3 AND t.nome = 'MICROPLÁSTICOS' AND c.segundo_coleta = 9.25") == 1,
            "coleta elementar incorreta");
        try { bancoPartidas.RegistrarDica(completa, 10); throw new Exception("dica após encerramento aceita"); }
        catch (SqliteException) { }
        Exigir((string?)Valor(caminho, $"SELECT data_evento FROM partidas WHERE id = {completa}") ==
            DateTime.Now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), "data do evento incorreta");
        Exigir(ValorInteiro(caminho, "SELECT count(*) FROM pragma_table_info('partidas') WHERE name GLOB '*_at_*' OR name GLOB '*time*' OR name = 'semente'") == 0,
            "partida guarda horário exato ou semente");

        var anonima = bancoPartidas.Iniciar(null, "medio", "0.1.0", "conteudo-ficticio", 15);
        bancoPartidas.RegistrarColeta(anonima, "PAPEL", 1, 10, 3);
        Exigir(ValorInteiro(caminho, "SELECT count(*) FROM versoes") == 1 && ValorInteiro(caminho, "SELECT count(*) FROM cenarios") == 2,
            "versões ou cenários duplicados");
        Exigir(bancoPartidas.MarcarInterrompidas() == 1, "partida em andamento não foi marcada");
        Exigir((string?)Valor(caminho, $"SELECT situacao FROM partidas WHERE id = {anonima}") == "interrompida" &&
            (double)Valor(caminho, $"SELECT segundos_jogados FROM partidas WHERE id = {anonima}")! == 3, "interrupção perdeu o progresso");
        Exigir(bancoPartidas.MarcarInterrompidas() == 0, "partida encerrada foi alterada");

        try { bancoPartidas.RegistrarColeta(999, "PAPEL", 1, 10, 1); throw new Exception("coleta sem partida aceita"); }
        catch (SqliteException) { }
        Exigir(ValorInteiro(caminho, "SELECT count(*) FROM pragma_foreign_key_check") == 0, "vínculo inválido");
    });

    private static void VerificarVersaoEsquema() => ComBanco((bancoCadastros, caminho) =>
    {
        bancoCadastros.Salvar(Ficticio());
        Valor(caminho, "PRAGMA user_version = 1;");
        var guardado = BancoLocal.Inicializar(caminho);
        Exigir(guardado is not null && File.Exists(guardado) && guardado != caminho, "banco antigo não foi guardado");
        Exigir(ValorInteiro(guardado!, "PRAGMA user_version") == 1 && ValorInteiro(guardado!, "SELECT count(*) FROM participantes") == 1,
            "dados do banco antigo foram alterados");
        Exigir(ValorInteiro(caminho, "PRAGMA user_version") == 2 && ValorInteiro(caminho, "SELECT count(*) FROM participantes") == 0,
            "banco novo não foi criado");
        Exigir(bancoCadastros.Salvar(Ficticio()) == 1 && BancoLocal.Inicializar(caminho) is null, "banco novo não aceita cadastro");
    });

    private static void VerificarBackup() => ComBanco((bancoCadastros, caminho) =>
    {
        for (var i = 0; i < 5; i++) bancoCadastros.Salvar(Ficticio());
        var bancoPartidas = new BancoPartidas(caminho);
        bancoPartidas.Iniciar(1, "facil", "0.1.0", "c", 12);
        var destino = Path.Combine(Path.GetDirectoryName(caminho)!, "backups", "copia.sqlite3");
        BancoLocal.FazerBackup(caminho, destino);
        Exigir(ValorInteiro(destino, "SELECT count(*) FROM participantes") == 5 &&
            ValorInteiro(destino, "SELECT count(*) FROM partidas") == 1, "backup incompleto");
        Exigir((string?)Valor(destino, "PRAGMA integrity_check") == "ok" && BancoLocal.VerificacaoRapida(destino), "backup corrompido");
    });

    private void VerificarFormulario()
    {
        var telaPrincipal = NovaTelaPrincipal();
        AddChild(telaPrincipal);
        try
        {
            var formulario = telaPrincipal.GetNode<FormularioCadastro>("%FormularioCadastro");
            Exigir(!formulario.TentarLer(out var cadastro, out var erro) && cadastro is null && erro.Length > 0, "vazio pulou o cadastro");
            Escolher(formulario, "CityInput");
            Exigir(!formulario.TentarLer(out _, out erro) && erro.Length > 0, "parcial aceito");
            formulario.Limpar();
            Exigir(!formulario.TentarLer(out cadastro, out _) && cadastro is null, "reset deixou respostas");
        }
        finally { telaPrincipal.Free(); }
    }

    private void VerificarSorteio()
    {
        var conteudo = ConteudoJogo.Carregar();
        foreach (var cenario in conteudo.Cenarios)
        {
            var mapas = new Dictionary<string, MapaVista>();
            try
            {
                foreach (var vista in cenario.Vistas.Where(vista => vista.CenaMapa is not null))
                {
                    var mapa = GD.Load<PackedScene>(vista.CenaMapa!).Instantiate<MapaVista>();
                    AddChild(mapa);
                    mapas[vista.Id] = mapa;
                    mapa.ValidarPara(vista, cenario, conteudo.Coletaveis);
                }
                var quantidadeEsperada = cenario.Vistas.Sum(vista => vista.RegrasSorteio.Sum(regra => regra.Quantidade));
                for (ulong semente = 1; semente <= 1000; semente++)
                {
                    var residuos = SorteioResiduos.Sortear(cenario, mapas, conteudo.Coletaveis, semente);
                    Exigir(residuos.SequenceEqual(SorteioResiduos.Sortear(cenario, mapas, conteudo.Coletaveis, semente)), "semente não reproduz alvos");
                    Exigir(residuos.Count == quantidadeEsperada, "quantidade mudou");
                    foreach (var residuo in residuos)
                    {
                        Exigir(residuo.Centro.Y >= RegrasJogo.AreaJogoTopo && residuo.Centro.Y <= RegrasJogo.AreaJogoBase &&
                            residuo.Centro.X >= 0 && residuo.Centro.X <= 1280, "alvo fora da área");
                        var vista = cenario.Vistas.Single(vista => vista.Id == residuo.IdVista);
                        if (vista.RegrasSorteio.Count == 0) continue;
                        Exigir(residuo.Centro.Y >= RegrasJogo.AreaJogoTopo + residuo.RaioToque && residuo.Centro.Y <= RegrasJogo.AreaJogoBase - residuo.RaioToque &&
                            residuo.Centro.X >= residuo.RaioToque && residuo.Centro.X <= 1280 - residuo.RaioToque, "spawn sem margem de toque");
                        Exigir(mapas[vista.Id].ZonasSorteio.Any(zona =>
                            vista.RegrasSorteio.Any(regra => regra.Zona == zona.IdZona && regra.Itens.Contains(residuo.IdItem)) &&
                            zona.ContemArea(residuo.Centro, residuo.RaioToque + residuo.AmplitudeFlutuacao)), "resíduo saiu da superfície");
                        foreach (var outro in residuos.Where(outro => outro.IdVista == residuo.IdVista && outro != residuo))
                            Exigir(residuo.Centro.DistanceTo(outro.Centro) >= residuo.RaioToque + outro.RaioToque + cenario.EspacamentoSorteio, "spawn sobreposto");
                    }
                }
            }
            finally { foreach (var mapa in mapas.Values) mapa.Free(); }
        }
    }

    // Semente em que um resíduo grande não cabe na fase fácil.
    private const long SementeSemEspacoFacil = 8891587186961508514;

    private void VerificarSorteioSemEspaco() => ComJogo(jogo =>
    {
        var sorteioDireto = false;
        try { ResiduosPara(jogo, "facil", SementeSemEspacoFacil); sorteioDireto = true; }
        catch (InvalidOperationException) { }
        Exigir(!sorteioDireto, "a semente de referência voltou a caber; atualize SementeSemEspacoFacil");
        jogo.IniciarComSemente("facil", SementeSemEspacoFacil);
        Exigir(jogo.EmAndamento && jogo.TotalResiduos == 12, "partida não abriu após sorteio sem espaço");
        Exigir(jogo.SementeAtual != SementeSemEspacoFacil, "semente usada não foi atualizada");
        Exigir(ResiduosPara(jogo, "facil", jogo.SementeAtual).Count == 12, "semente registrada não reproduz a partida");
    });

    private void VerificarCopiaEAvisoBanco() => ComPastaTemporaria(pasta =>
    {
        var caminho = Path.Combine(pasta, "evento.sqlite3");
        var tela = NovaTelaPrincipal(caminho);
        AddChild(tela);
        try
        {
            Exigir(!tela.GetNode<Label>("%MenuScreen/DatabaseWarning").Visible, "aviso aparece com o banco bom");
            new BancoCadastros(caminho).Salvar(Ficticio());
            tela.GetNode<Button>("%PlayButton").EmitSignal(BaseButton.SignalName.Pressed);
            tela.PularCadastro();
            tela.GetNode<Button>("%DifficultySelection/CopyDataButton").EmitSignal(BaseButton.SignalName.Pressed);
            var copias = Directory.GetFiles(Path.Combine(pasta, "copias"), "*.sqlite3");
            Exigir(copias.Length == 1 && BancoLocal.VerificacaoRapida(copias[0]) &&
                ValorInteiro(copias[0], "SELECT count(*) FROM participantes") == 1, "cópia ausente ou incompleta");
            Exigir(tela.GetNode<Label>("%DifficultySelection/CopyDataResult").Text.StartsWith("CÓPIA ÍNTEGRA: 1 CADASTROS"), "resultado da cópia não mostrado");
        }
        finally { tela.Free(); }

        var corrompido = Path.Combine(pasta, "corrompido.sqlite3");
        var lixo = new byte[8192]; new Random(3).NextBytes(lixo); File.WriteAllBytes(corrompido, lixo);
        var telaRuim = NovaTelaPrincipal(corrompido);
        AddChild(telaRuim);
        try { Exigir(telaRuim.GetNode<Label>("%MenuScreen/DatabaseWarning").Visible, "banco corrompido sem aviso para a equipe"); }
        finally { telaRuim.Free(); }
    });

    private static void VerificarTravaBanco() => ComPastaTemporaria(pasta =>
    {
        var caminho = Path.Combine(pasta, "trava.sqlite3");
        var primeira = TravaBanco.Tentar(caminho, out var travouPrimeira);
        Exigir(travouPrimeira && primeira is not null, "primeira cópia não obteve a trava");
        TravaBanco.Tentar(caminho, out var travouSegunda);
        Exigir(!travouSegunda, "segunda cópia abriu o mesmo banco");
        primeira!.Dispose();
        using var depois = TravaBanco.Tentar(caminho, out var travouDepois);
        Exigir(travouDepois, "trava não foi liberada ao fechar a primeira cópia");
    });

    private static void VerificarFasePorIdade()
    {
        foreach (var (idade, fase) in new[] { (7, "facil"), (8, "facil"), (12, "facil"), (13, "medio"), (16, "medio"), (17, "dificil"), (40, "dificil") })
            Exigir(RegrasJogo.CenarioPorIdade(idade) == fase, $"idade {idade} não foi para {fase}");
    }

    private void VerificarCadastroEscolheFase()
    {
        // O + começa em 8 anos; cada toque seguinte soma um.
        foreach (var (toques, total) in new[] { (5, 12), (6, 15), (10, 20) })
        {
            var telaPrincipal = NovaTelaPrincipal();
            AddChild(telaPrincipal);
            try
            {
                telaPrincipal.GetNode<Button>("%PlayButton").EmitSignal(BaseButton.SignalName.Pressed);
                var formulario = telaPrincipal.GetNode<FormularioCadastro>("%FormularioCadastro");
                foreach (var campo in new[] { "CityInput", "SchoolInput", "EducationLevelInput", "SchoolYearInput" })
                    Escolher(formulario, campo);
                for (var i = 0; i < toques; i++)
                    formulario.AjustarIdade(1);
                telaPrincipal.GetNode<Button>("%DataContinueButton").EmitSignal(BaseButton.SignalName.Pressed);
                Exigir(telaPrincipal.GetNode<Control>("%RulesScreen").Visible &&
                    !telaPrincipal.GetNode<Control>("%DifficultySelection").Visible, "seleção apareceu fora do modo de teste");
                telaPrincipal.GetNode<Button>("%RulesStartButton").EmitSignal(BaseButton.SignalName.Pressed);
                var jogo = telaPrincipal.GetNode<ControladorPartida>("GameScreen");
                Exigir(jogo.EmAndamento && jogo.TotalResiduos == total, $"idade {7 + toques} abriu a fase errada");
            }
            finally { telaPrincipal.Free(); }
        }
    }

    private void VerificarToqueLongoTitulo()
    {
        var telaPrincipal = NovaTelaPrincipal();
        AddChild(telaPrincipal);
        try
        {
            telaPrincipal.GetNode<Button>("%PlayButton").EmitSignal(BaseButton.SignalName.Pressed);
            var titulo = telaPrincipal.GetNode<Control>("%DataTitle");
            var temporizador = telaPrincipal.GetChildren().OfType<Godot.Timer>().Single();
            titulo.EmitSignal(Control.SignalName.GuiInput, new InputEventScreenTouch { Index = 0, Pressed = true });
            titulo.EmitSignal(Control.SignalName.GuiInput, new InputEventScreenTouch { Index = 0 });
            Exigir(temporizador.IsStopped(), "toque curto manteve a contagem");
            titulo.EmitSignal(Control.SignalName.GuiInput, new InputEventScreenTouch { Index = 0, Pressed = true });
            Exigir(!temporizador.IsStopped() && temporizador.WaitTime >= 3, "toque longo não começou a contar");
            temporizador.EmitSignal(Godot.Timer.SignalName.Timeout);
            Exigir(telaPrincipal.GetNode<Control>("%RulesScreen").Visible &&
                telaPrincipal.GetNode<Control>("%DifficultySelection").Visible, "toque longo não abriu a seleção de teste");
        }
        finally { telaPrincipal.Free(); }
    }

    private void VerificarTextosRegras()
    {
        var telaPrincipal = NovaTelaPrincipal();
        AddChild(telaPrincipal);
        try
        {
            var cartoes = telaPrincipal.GetNode("%RulesScreen/Panel/Margin/Content/RoundInfo");
            string Texto(string cartao) => cartoes.GetNode<Label>($"{cartao}/Text").Text;
            Exigir(Texto("TimeCard").Contains($"{RegrasJogo.LimiteSegundos} SEGUNDOS"), "tempo diferente de RegrasJogo");
            Exigir(Texto("PointsCard").Contains($"{RegrasJogo.PontosPorColeta} PONTOS"), "pontos diferentes de RegrasJogo");
            Exigir(Texto("HintsCard").Contains($"{RegrasJogo.MaximoDicas} DICAS") &&
                Texto("HintsCard").Contains($"CUSTA {RegrasJogo.PenalidadeDicaSegundos} SEGUNDOS"), "dicas diferentes de RegrasJogo");
        }
        finally { telaPrincipal.Free(); }
    }

    private void VerificarNiveisManuais()
    {
        var telaPrincipal = NovaTelaPrincipal();
        AddChild(telaPrincipal);
        try
        {
            var opcoes = telaPrincipal.GetNode<Container>("%DifficultyInput");
            Exigir(opcoes.GetChildCount() == 3, "seleção de teste sem as três fases");
            foreach (var (indice, nome) in new[] { (0, "COMEÇAR"), (1, "EXPLORAR"), (2, "INVESTIGAR") })
            {
                telaPrincipal.GetNode<Button>("%PlayButton").EmitSignal(BaseButton.SignalName.Pressed);
                telaPrincipal.PularCadastro();
                opcoes.GetChild<Button>(indice).ButtonPressed = true;
                telaPrincipal.GetNode<Button>("%RulesStartButton").EmitSignal(BaseButton.SignalName.Pressed);
                var jogo = telaPrincipal.GetNode<ControladorPartida>("GameScreen");
                Exigir(jogo.EmAndamento && jogo.Visible && opcoes.GetChild<Button>(indice).Text.StartsWith(nome), "escolha não abriu partida");
                jogo.GetNode<Button>("%LeaveButton").EmitSignal(BaseButton.SignalName.Pressed);
                jogo.GetNode<Button>("%LeaveConfirmButton").EmitSignal(BaseButton.SignalName.Pressed);
                Exigir(telaPrincipal.GetNode<Control>("%MenuScreen").Visible && jogo.SementeAtual == 0, "saída não limpou sessão");
            }
        }
        finally { telaPrincipal.Free(); }
    }

    private static void VerificarConteudoFacil()
    {
        var conteudo = ConteudoJogo.Carregar();
        var cenario = conteudo.Cenarios.Single(s => s.Id == "facil");
        foreach (var vista in cenario.Vistas)
        {
            Exigir(ResourceLoader.Exists(vista.Fundo), $"imagem ausente em {vista.Id}");
            var tamanhoImagem = GD.Load<Texture2D>(vista.Fundo).GetSize();
            var origem = new Rect2(Vector2.Zero, tamanhoImagem);
            Exigir(origem.Size.X >= 1600 && origem.Size.Y >= 900,
                $"{vista.Id} amplia apenas {origem.Size.X} × {origem.Size.Y} pixels para o totem");
            Exigir(Math.Abs(origem.Size.X / origem.Size.Y - 16f / 9) < 0.002f, "foto fora da proporção 16:9 distorce o recorte");
            Exigir(vista.CenaMapa is not null, "camada fácil ainda não tem geração por superfície");
            var micro = vista.RegrasSorteio.SelectMany(regra => regra.Itens).Any(item => conteudo.Coletaveis[item].Tipo == "MICROPLÁSTICOS");
            Exigir(micro == (vista.Escala == 4), $"{vista.Id}: microplásticos devem aparecer só no microscópio");
        }
    }

    private static void VerificarImagensOutrosCenarios()
    {
        foreach (var cenario in ConteudoJogo.Carregar().Cenarios.Where(s => s.Id is "medio" or "dificil"))
            foreach (var vista in cenario.Vistas)
            {
                Exigir(ResourceLoader.Exists(vista.Fundo), $"imagem ausente em {cenario.Id}/{vista.Id}");
                var tamanhoImagem = GD.Load<Texture2D>(vista.Fundo).GetSize();
                Exigir(tamanhoImagem.X >= 1600 && tamanhoImagem.Y >= 900,
                    $"{cenario.Id}/{vista.Id} amplia apenas {tamanhoImagem.X} × {tamanhoImagem.Y} pixels");
                Exigir(vista.CenaMapa is not null && ResourceLoader.Exists(vista.CenaMapa) &&
                    vista.RegrasSorteio.Count > 0, $"superfícies sem mapa em {cenario.Id}/{vista.Id}");
            }
    }

    private void VerificarAreaSorteio()
    {
        var mapa = new MapaVista();
        mapa.AddChild(new Sprite2D { Name = "EditorReference" });
        var zona = new ZonaSorteio { Name = "Surface", IdZona = "surface" };
        zona.AddChild(new CollisionPolygon2D
        {
            Name = "Polygon", Polygon = [new(200, 250), new(400, 250), new(400, 450), new(200, 450)]
        });
        mapa.AddChild(zona);
        AddChild(mapa);
        try
        {
            var cenario = new Cenario { Vistas = [new Vista
            {
                Id = "visao_geral", RegrasSorteio = [new RegraSorteio { Zona = "surface", Itens = ["bottle"], Quantidade = 1 }]
            }] };
            var itens = new Dictionary<string, TipoColetavel>
            {
                ["bottle"] = new() { Escala = 1, Tamanho = 100 }
            };
            for (ulong semente = 1; semente <= 100; semente++)
            {
                var residuo = SorteioResiduos.Sortear(cenario, new Dictionary<string, MapaVista> { ["visao_geral"] = mapa }, itens, semente).Single();
                Exigir(residuo.Centro.X >= 250 && residuo.Centro.X <= 350 && residuo.Centro.Y >= 300 && residuo.Centro.Y <= 400,
                    "centro está na zona, mas parte do resíduo fica fora dela");
            }
        }
        finally { mapa.Free(); }
    }

    private void VerificarPerspectiva() => ComJogo(jogo =>
    {
        jogo.IniciarComSemente("dificil", 123);
        var amostras = new List<ResiduoSorteado>();
        for (long semente = 1; semente <= 50; semente++)
            amostras.Add(ResiduosPara(jogo, "dificil", semente)[0]);
        Exigir(amostras.Select(residuo => residuo.TamanhoExibido).Distinct().Count() > 10, "tamanho ignora profundidade");
        Exigir(amostras.MinBy(residuo => residuo.Centro.Y)!.TamanhoExibido < amostras.MaxBy(residuo => residuo.Centro.Y)!.TamanhoExibido,
            "perspectiva invertida: resíduo distante é maior");
        Exigir(amostras.All(residuo => residuo.RaioToque >= 36), "calibração reduziu área acessível de toque");
    });

    private static void VerificarCantosArea()
    {
        using var zona = new ZonaSorteio();
        zona.AddChild(new CollisionPolygon2D
        {
            Name = "Polygon", Polygon = [new(300, 265), new(385, 350), new(300, 435), new(215, 350)]
        });
        try { Exigir(!zona.ContemArea(new Vector2(300, 350), 50), "raio circular ignorou os cantos de uma textura quadrada"); }
        finally { zona.Free(); }
    }

    private void VerificarExclusoesSuperficie()
    {
        foreach (var (cena, idZona, ponto) in new[]
        {
            ("FacilVisaoGeral", "grama_frente", new Vector2(700, 460)),
            ("FacilVisaoGeral", "grama_fundo", new Vector2(700, 460)),
            ("FacilVisaoGeral", "grama_frente", new Vector2(600, 440)),
            ("FacilVisaoGeral", "grama_frente", new Vector2(300, 370))
        })
        {
            var mapa = GD.Load<PackedScene>($"res://scenes/gameplay/maps/{cena}.tscn").Instantiate<MapaVista>();
            AddChild(mapa);
            try
            {
                Exigir(!mapa.ZonasSorteio.Single(zona => zona.IdZona == idZona).ContemArea(ponto, 0),
                    $"zona {cena}/{idZona} inclui uma superfície incompatível na foto");
            }
            finally { mapa.Free(); }
        }
    }

    private void VerificarMicroplasticosMicroscopio() => ComJogo(jogo =>
    {
        var conteudo = ConteudoJogo.Carregar();
        foreach (var idCenario in s_todosOsCenarios)
        {
            jogo.IniciarComSemente(idCenario, 123);
            for (long semente = 1; semente <= 100; semente++)
            {
                var micro = ResiduosPara(jogo, idCenario, semente).Where(residuo => residuo.IdVista == "microscopio").ToList();
                Exigir(micro.Count > 0 && micro.All(residuo => conteudo.Coletaveis[residuo.IdItem].Tipo == "MICROPLÁSTICOS"),
                    $"{idCenario}, semente {semente}: microscópio sem microplásticos");
                Exigir(micro.All(residuo => residuo.Centro.Y - residuo.RaioToque >= RegrasJogo.AreaJogoTopo &&
                    residuo.Centro.Y + residuo.RaioToque <= RegrasJogo.AreaJogoBase),
                    $"{idCenario}, semente {semente}: microplástico sob o HUD ou o rodapé");
            }
        }
    });

    private void VerificarCoordenadasMapa()
    {
        var conteudo = ConteudoJogo.Carregar();
        var cenario = conteudo.Cenarios.Single(s => s.Id == "dificil");
        var mapas = new Dictionary<string, MapaVista>();
        try
        {
            foreach (var vista in cenario.Vistas)
            {
                var mapa = GD.Load<PackedScene>(vista.CenaMapa!).Instantiate<MapaVista>();
                AddChild(mapa);
                mapas[vista.Id] = mapa;
            }
            var original = SorteioResiduos.Sortear(cenario, mapas, conteudo.Coletaveis, 123);
            foreach (var mapa in mapas.Values) mapa.Position = new Vector2(20, 15);
            var movido = SorteioResiduos.Sortear(cenario, mapas, conteudo.Coletaveis, 123);
            Exigir(original.SequenceEqual(movido), "posições globais vazaram para canvas");
        }
        finally { foreach (var mapa in mapas.Values) mapa.Free(); }
    }

    private static void ConteudoInvalido(ConteudoJogo conteudo)
    {
        try { conteudo.Validar(); }
        catch (InvalidOperationException) { return; }
        throw new Exception("conteúdo inválido foi aceito");
    }

    private static void VerificarLigacoesConteudo()
    {
        var conteudo = ConteudoJogo.Carregar();
        conteudo.Cenarios[0].Vistas[0].IdProxima = "missing";
        ConteudoInvalido(conteudo);
        conteudo = ConteudoJogo.Carregar();
        conteudo.Cenarios[0].Vistas[1].IdPai = "detalhe";
        ConteudoInvalido(conteudo);
    }

    private static void VerificarColetaveisConteudo()
    {
        var conteudo = ConteudoJogo.Carregar();
        conteudo.Coletaveis["garrafas_pet_conjunto"].Tamanho = 0;
        ConteudoInvalido(conteudo);
        conteudo = ConteudoJogo.Carregar();
        conteudo.Coletaveis["garrafas_pet_conjunto"].Escala = 2;
        ConteudoInvalido(conteudo);
        conteudo = ConteudoJogo.Carregar();
        conteudo.Mensagens.Remove("MICROPLÁSTICOS");
        ConteudoInvalido(conteudo);
    }

    private static void VerificarEscalaColetaveis()
    {
        var conteudo = ConteudoJogo.Carregar();
        foreach (var vista in conteudo.Cenarios.SelectMany(cenario => cenario.Vistas))
        {
            var itens = vista.RegrasSorteio.SelectMany(regra => regra.Itens);
            foreach (var id in itens)
                Exigir(conteudo.Coletaveis[id].Escala == vista.Escala &&
                    conteudo.Coletaveis[id].Textura.Contains($"/escala-{vista.Escala}/") &&
                    ResourceLoader.Exists(conteudo.Coletaveis[id].Textura),
                    $"{id} usa imagem de outra escala em {vista.Id}");
        }
    }

    private void VerificarValidacaoMapa()
    {
        var conteudo = ConteudoJogo.Carregar();
        var cenario = conteudo.Cenarios.Single(s => s.Id == "dificil");
        var vista = cenario.Vistas[0];
        var mapa = GD.Load<PackedScene>(vista.CenaMapa!).Instantiate<MapaVista>();
        AddChild(mapa);
        try
        {
            mapa.ValidarPara(vista, cenario, conteudo.Coletaveis);
            var idPequeno = vista.RegrasSorteio[0].Itens[0];
            var tamanhoOriginal = conteudo.Coletaveis[idPequeno].Tamanho;
            conteudo.Coletaveis[idPequeno].Tamanho = RegrasJogo.TamanhoMinimoVisivel / 2;
            var recusouPequeno = false;
            try { mapa.ValidarPara(vista, cenario, conteudo.Coletaveis); }
            catch (InvalidOperationException) { recusouPequeno = true; }
            Exigir(recusouPequeno, "resíduo abaixo do mínimo visível foi aceito");
            conteudo.Coletaveis[idPequeno].Tamanho = tamanhoOriginal;
        }
        finally { mapa.Free(); }
    }

    private void VerificarNavegacaoDica() => ComJogo(jogo =>
    {
        jogo.IniciarComSemente("dificil", 123);
        jogo.SetProcess(false);
        var cenario = ConteudoJogo.Carregar().Cenarios.Single(s => s.Id == "dificil");
        foreach (var residuo in ResiduosPara(jogo, "dificil").Where(residuo => residuo.IdVista == "visao_geral"))
            Tocar(jogo, CentroNoInicio(residuo));
        jogo.GetNode<Button>("%HintButton").EmitSignal(BaseButton.SignalName.Pressed);
        var aviso = jogo.GetNode<Label>("%LayerNotice").Text;
        Exigir(aviso.Contains("AMPLIE"), "dica não orientou ampliar");
        Exigir(!cenario.Vistas.Any(vista => aviso.Contains(vista.Nome, StringComparison.OrdinalIgnoreCase)),
            "dica revelou o nome da camada de destino");
    });

    private void VerificarDicaMesmaVista() => ComJogo(jogo =>
    {
        jogo.GetNode<Button>("%HintButton").EmitSignal(BaseButton.SignalName.Pressed);
        var aviso = jogo.GetNode<Label>("%LayerNotice").Text;
        Exigir(aviso.Contains("REGIÃO QUE PULSA") && aviso.Contains("PERTO"), "dica local não apontou uma região próxima");
        Exigir(!aviso.Contains("AMPLIE") && !aviso.Contains("REDUZA"), "dica local pediu troca de camada");
    });

    private void VerificarDicaReduzir() => ComJogo(jogo =>
    {
        jogo.IniciarComSemente("dificil", 123);
        jogo.SetProcess(false);
        for (var i = 0; i < 3; i++) Rolar(jogo, MouseButton.WheelUp, s_centroTela);
        foreach (var residuo in ResiduosPara(jogo, "dificil", 123).Where(residuo => residuo.IdVista == "microscopio"))
            Tocar(jogo, CentroNoInicio(residuo));

        jogo.GetNode<Button>("%HintButton").EmitSignal(BaseButton.SignalName.Pressed);
        var aviso = jogo.GetNode<Label>("%LayerNotice").Text;
        var cenario = ConteudoJogo.Carregar().Cenarios.Single(item => item.Id == "dificil");
        Exigir(aviso.Contains("REDUZA"), "dica não orientou reduzir a escala");
        Exigir(!cenario.Vistas.Any(vista => aviso.Contains(vista.Nome, StringComparison.OrdinalIgnoreCase)),
            "dica ao reduzir revelou o nome da camada de destino");
    });

    private static void VerificarPartida()
    {
        var partida = new Partida();
        partida.Iniciar(2);
        Exigir(partida.Coletar(0, "PAPEL") && !partida.Coletar(0, "PAPEL") && !partida.Coletar(2, "PAPEL"), "coleta repetida/inexistente");
        Exigir(partida.Pontuacao == 10 && partida.ColetadosPorTipo["PAPEL"] == 1, "totais incorretos");
        partida.Avancar(-5);
        Exigir(partida.SegundosRestantes == 45, "tempo negativo estendeu partida");
        partida.Avancar(44);
        Exigir(partida.UsarDica() && !partida.EmAndamento && partida.MotivoFim == SituacaoPartida.TempoEsgotado, "penalidade não encerrou");
        Exigir(partida.SegundosRestantes == 0 && partida.SegundosDecorridos == 44 && !partida.Coletar(1, "PAPEL") && !partida.UsarDica(), "ações após fim");
        partida.Iniciar(1);
        Exigir(partida.Pontuacao == 0 && partida.DicasUsadas == 0 && partida.ColetadosPorTipo.Count == 0, "reinício não limpou totais");
        partida.Avancar(100);
        Exigir(partida.SegundosDecorridos == 45, "duração excedeu tempo disponível");
        partida.Limpar();
        Exigir(!partida.EmAndamento && partida.TotalResiduos == 0 && partida.SegundosRestantes == 0 && partida.MotivoFim is null, "estado residual");
    }

    private void ComJogo(Action<ControladorPartida> acao)
    {
        var jogo = GD.Load<PackedScene>("res://scenes/ControladorPartida.tscn").Instantiate<ControladorPartida>();
        AddChild(jogo);
        try
        {
            jogo.IniciarComSemente("facil", 123);
            jogo.SetProcess(false);
            acao(jogo);
        }
        finally { jogo.PararELimpar(); jogo.Free(); }
    }

    private static List<ResiduoSorteado> ResiduosPara(ControladorPartida jogo, string idCenario, long semente = 123)
    {
        var conteudo = ConteudoJogo.Carregar();
        var cenario = conteudo.Cenarios.Single(s => s.Id == idCenario);
        var mapas = cenario.Vistas.Where(vista => vista.CenaMapa is not null).ToDictionary(vista => vista.Id,
            vista => jogo.GetChildren().OfType<MapaVista>().Single(mapa => mapa.Name.ToString() == Path.GetFileNameWithoutExtension(vista.CenaMapa!)));
        return SorteioResiduos.Sortear(cenario, mapas, conteudo.Coletaveis, (ulong)semente);
    }

    private static Vector2 CentroNoInicio(ResiduoSorteado residuo) => residuo.Centro +
        new Vector2(Mathf.Sin(residuo.FaseFlutuacao) * residuo.AmplitudeFlutuacao,
            Mathf.Sin(residuo.FaseFlutuacao * 1.3f) * residuo.AmplitudeFlutuacao * 0.6f);

    // Volta à visão geral e amplia, pela roda do mouse, até a vista de destino.
    private static void Visitar(ControladorPartida jogo, Cenario cenario, Vista destino)
    {
        for (var i = 0; i < RegrasJogo.QuantidadeEscalas; i++)
            Rolar(jogo, MouseButton.WheelDown, s_centroTela);
        for (var passo = destino; passo.IdPai is not null; passo = cenario.Vistas.Single(vista => vista.Id == passo.IdPai))
            Rolar(jogo, MouseButton.WheelUp, s_centroTela);
    }

    private static readonly Vector2 s_centroTela = RegrasJogo.TamanhoTela / 2;

    private static void Rolar(ControladorPartida jogo, MouseButton roda, Vector2 posicao) =>
        jogo._Input(new InputEventMouseButton { Pressed = true, ButtonIndex = roda, Position = posicao });

    private static void Tocar(ControladorPartida jogo, Vector2 posicao) => jogo._Input(new InputEventMouseButton
    {
        Pressed = true, ButtonIndex = MouseButton.Left, Position = posicao
    });

    private void VerificarColetas() => ComJogo(jogo =>
    {
        foreach (var idCenario in s_todosOsCenarios)
        {
            jogo.IniciarComSemente(idCenario, 123);
            jogo.SetProcess(false);
            var residuos = ResiduosPara(jogo, idCenario);
            var primeiro = CentroNoInicio(residuos[0]);
            Tocar(jogo, primeiro);
            Exigir(jogo.GetNode<Label>("%ScoreLabel").Text == "PONTOS: 10", "coleta não pontuou");
            Tocar(jogo, primeiro);
            Exigir(jogo.GetNode<Label>("%ScoreLabel").Text == "PONTOS: 10", "coleta duplicada");
            if (idCenario == "facil")
                Exigir(EscalaAtual(jogo) == 1, "toque repetido virou zoom fora de região marcada");
            var conteudo = ConteudoJogo.Carregar();
            var cenario = conteudo.Cenarios.Single(s => s.Id == idCenario);
            foreach (var vista in cenario.Vistas)
            {
                Visitar(jogo, cenario, vista);
                foreach (var residuo in residuos.Where(residuo => residuo.IdVista == vista.Id && residuo != residuos[0]))
                {
                    var posicao = CentroNoInicio(residuo);
                    jogo._Input(new InputEventScreenTouch { Index = 0, Pressed = true, Position = posicao });
                    jogo._Input(new InputEventScreenTouch { Index = 0, Position = posicao });
                }
            }
            var quantidade = idCenario switch { "facil" => 12, "medio" => 15, _ => 20 };
            Exigir(jogo.GetNode<Control>("%ResultPanel").Visible &&
                jogo.GetNode<Label>("%ResultSummary").Text.StartsWith($"{quantidade * 10} PONTOS · {quantidade} DE {quantidade}"), "fim por coleta mudou");
            var botaoResultado = jogo.GetNode<GridContainer>("%ResultTypes").GetChildren().OfType<Button>()
                .Single(botao => botao.Text.StartsWith("GARRAFA PLÁSTICA"));
            botaoResultado.EmitSignal(BaseButton.SignalName.Pressed);
            Exigir(jogo.GetNode<Label>("%EducationText").Text == conteudo.Mensagens["GARRAFA PLÁSTICA"], "explicação não abriu");
            jogo.PararELimpar();
            Exigir(jogo.SementeAtual == 0 && jogo.GetNode<Label>("%ResultSummary").Text == "", "sessão não limpa");
        }
    });

    private void VerificarTempoEDicas() => ComJogo(jogo =>
    {
        foreach (var idCenario in s_todosOsCenarios)
        {
            jogo.IniciarComSemente(idCenario, 123);
            jogo.SetProcess(false);
            for (var i = 0; i < 6; i++) jogo.GetNode<Button>("%HintButton").EmitSignal(BaseButton.SignalName.Pressed);
            Exigir(jogo.GetNode<Label>("%TimeLabel").Text == "TEMPO: 30 SEG", "limite/penalidade de dicas mudou");
            jogo._Process(29);
            Exigir(!jogo.GetNode<Control>("%ResultPanel").Visible, "fim antecipado");
            jogo._Process(1);
            Exigir(jogo.GetNode<Control>("%ResultPanel").Visible, "tempo não encerrou");
        }
    });

    private static int EscalaAtual(ControladorPartida jogo) =>
        int.Parse(jogo.GetNode<Label>("%ScaleLabel").Text["ESCALA ".Length..].Split('/')[0]);

    private void VerificarZoom() => ComJogo(jogo =>
    {
        foreach (var idCenario in s_todosOsCenarios)
        {
            jogo.IniciarComSemente(idCenario, 123);
            Rolar(jogo, MouseButton.WheelUp, s_centroTela);
            Exigir(EscalaAtual(jogo) == 2, "não ampliou");
            Rolar(jogo, MouseButton.WheelUp, s_centroTela);
            Exigir(EscalaAtual(jogo) == 3, "não chegou à lente");
            Rolar(jogo, MouseButton.WheelUp, s_centroTela);
            Exigir(EscalaAtual(jogo) == 4, "não chegou ao microscópio");
            Rolar(jogo, MouseButton.WheelDown, s_centroTela);
            Exigir(EscalaAtual(jogo) == 3, "não voltou à lente");
            Rolar(jogo, MouseButton.WheelDown, s_centroTela);
            Exigir(EscalaAtual(jogo) == 2, "não voltou ao pai");
            Rolar(jogo, MouseButton.WheelDown, s_centroTela);
            Exigir(EscalaAtual(jogo) == 1, "visão geral perdida");
        }
    });

    private void VerificarPinca() => ComJogo(jogo =>
    {
        var posicao = CentroNoInicio(ResiduosPara(jogo, "facil")[0]);
        jogo._Input(new InputEventScreenTouch { Index = 0, Pressed = true, Position = posicao - new Vector2(20, 0) });
        jogo._Input(new InputEventScreenTouch { Index = 1, Pressed = true, Position = posicao + new Vector2(20, 0) });
        jogo._Input(new InputEventScreenTouch { Index = 1, Position = posicao });
        jogo._Input(new InputEventScreenTouch { Index = 0, Position = posicao });
        Exigir(jogo.GetNode<Label>("%ScoreLabel").Text == "PONTOS: 0", "fim de pinça coletou");
    });

    private void VerificarToqueCancelado() => ComJogo(jogo =>
    {
        var posicao = CentroNoInicio(ResiduosPara(jogo, "facil")[0]);
        jogo._Input(new InputEventScreenTouch { Index = 0, Pressed = true, Position = posicao });
        jogo._Input(new InputEventScreenTouch { Index = 0, Canceled = true, Position = posicao });
        Exigir(jogo.GetNode<Label>("%ScoreLabel").Text == "PONTOS: 0", "toque cancelado coletou");
    });

    private void VerificarZoomPinca() => ComJogo(jogo =>
    {
        foreach (var idCenario in s_todosOsCenarios)
        {
            jogo.IniciarComSemente(idCenario, 123);
            jogo.SetProcess(false);
            jogo._Input(new InputEventScreenTouch { Index = 0, Pressed = true, Position = new Vector2(500, 420) });
            jogo._Input(new InputEventScreenTouch { Index = 1, Pressed = true, Position = new Vector2(600, 420) });
            jogo._Input(new InputEventScreenDrag { Index = 1, Position = new Vector2(700, 420) });
            Exigir(EscalaAtual(jogo) == 2, "pinça não ampliou");
            jogo._Input(new InputEventScreenDrag { Index = 1, Position = new Vector2(900, 420) });
            Exigir(EscalaAtual(jogo) == 2, "um gesto ampliou duas camadas");
            jogo._Input(new InputEventScreenTouch { Index = 0, Position = new Vector2(500, 420) });
            jogo._Input(new InputEventScreenTouch { Index = 1, Position = new Vector2(900, 420) });
            jogo._Input(new InputEventScreenTouch { Index = 0, Pressed = true, Position = new Vector2(500, 420) });
            jogo._Input(new InputEventScreenTouch { Index = 1, Pressed = true, Position = new Vector2(700, 420) });
            jogo._Input(new InputEventScreenDrag { Index = 1, Position = new Vector2(550, 420) });
            Exigir(EscalaAtual(jogo) == 1, "pinça não reduziu");
            Exigir(jogo.GetNode<Label>("%ScoreLabel").Text == "PONTOS: 0", "pinça coletou");
        }
    });

    private void VerificarRodaHud() => ComJogo(jogo =>
    {
        jogo._Input(new InputEventMouseButton { Pressed = true, ButtonIndex = MouseButton.WheelUp, Position = new Vector2(100, 50) });
        Exigir(EscalaAtual(jogo) == 1, "roda sobre HUD ampliou cenário");
    });

    private void VerificarToqueNoAvisoNaoColeta() => ComJogo(jogo =>
    {
        var ponto = CentroNoInicio(ResiduosPara(jogo, "facil")[0]);
        var aviso = jogo.GetNode<Control>("%NoticePanel");
        aviso.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
        aviso.Position = ponto - new Vector2(60, 24);
        aviso.Size = new Vector2(120, 48);
        aviso.Show();
        Tocar(jogo, ponto);
        Exigir(jogo.GetNode<Label>("%ScoreLabel").Text == "PONTOS: 0", "toque no aviso coletou resíduo por baixo");
    });

    private void VerificarAtividadeTeclado()
    {
        var telaPrincipal = NovaTelaPrincipal();
        AddChild(telaPrincipal);
        try
        {
            telaPrincipal.GetNode<Button>("%PlayButton").EmitSignal(BaseButton.SignalName.Pressed);
            telaPrincipal._Process(170);
            telaPrincipal._Input(new InputEventKey { Pressed = true, Unicode = 'A' });
            telaPrincipal._Process(20);
            Exigir(telaPrincipal.GetNode<Control>("%DataScreen").Visible, "digitação não contou como atividade");
            telaPrincipal._Process(180);
            Exigir(telaPrincipal.GetNode<Control>("%MenuScreen").Visible, "inatividade não retornou ao início");
            var formulario = telaPrincipal.GetNode<FormularioCadastro>("%FormularioCadastro");
            Exigir(!formulario.TentarLer(out var cadastro, out _) && cadastro is null, "retorno deixou cadastro");
        }
        finally { telaPrincipal.Free(); }
    }

    private void VerificarInicioDuplicado()
    {
        var telaPrincipal = NovaTelaPrincipal();
        AddChild(telaPrincipal);
        try
        {
            telaPrincipal.GetNode<Button>("%PlayButton").EmitSignal(BaseButton.SignalName.Pressed);
            telaPrincipal.PularCadastro();
            var inicio = telaPrincipal.GetNode<Button>("%RulesStartButton");
            inicio.EmitSignal(BaseButton.SignalName.Pressed);
            var jogo = telaPrincipal.GetNode<ControladorPartida>("GameScreen");
            var semente = jogo.SementeAtual;
            jogo._Process(1);
            inicio.EmitSignal(BaseButton.SignalName.Pressed);
            Exigir(jogo.SementeAtual == semente && jogo.GetNode<Label>("%TimeLabel").Text == "TEMPO: 44 SEG", "segundo toque reiniciou partida");
        }
        finally { telaPrincipal.Free(); }
    }

    private void VerificarGravacaoPartidas() => ComPastaTemporaria(pasta =>
    {
        var caminho = Path.Combine(pasta, "sessions.sqlite3");
        try
        {
            var telaPrincipal = NovaTelaPrincipal(caminho);
            AddChild(telaPrincipal);
            var jogo = telaPrincipal.GetNode<ControladorPartida>("GameScreen");
            void Iniciar(bool cadastrar)
            {
                telaPrincipal.GetNode<Button>("%PlayButton").EmitSignal(BaseButton.SignalName.Pressed);
                if (cadastrar)
                {
                    var formulario = telaPrincipal.GetNode<FormularioCadastro>("%FormularioCadastro");
                    Escolher(formulario, "CityInput");
                    Escolher(formulario, "SchoolInput");
                    Escolher(formulario, "EducationLevelInput");
                    Escolher(formulario, "SchoolYearInput");
                    for (var idade = 0; idade < 5; idade++)
                        formulario.AjustarIdade(1);
                    telaPrincipal.GetNode<Button>("%DataContinueButton").EmitSignal(BaseButton.SignalName.Pressed);
                }
                else
                {
                    telaPrincipal.PularCadastro();
                    telaPrincipal.GetNode<Container>("%DifficultyInput").GetChild<Button>(0).ButtonPressed = true;
                }
                telaPrincipal.GetNode<Button>("%RulesStartButton").EmitSignal(BaseButton.SignalName.Pressed);
                Exigir(jogo.EmAndamento, "partida não iniciou");
                jogo.SetProcess(false);
            }
            string Situacao(long id) => (string)Valor(caminho, $"SELECT situacao FROM partidas WHERE id = {id}")!;

            Iniciar(cadastrar: true);
            var cenario = ConteudoJogo.Carregar().Cenarios.Single(s => s.Id == "facil");
            var residuos = ResiduosPara(jogo, "facil", jogo.SementeAtual);
            foreach (var vista in cenario.Vistas)
            {
                Visitar(jogo, cenario, vista);
                foreach (var residuo in residuos.Where(residuo => residuo.IdVista == vista.Id))
                    Tocar(jogo, CentroNoInicio(residuo));
            }
            Exigir(Situacao(1) == "completou" && ValorInteiro(caminho, "SELECT participante_id FROM partidas WHERE id = 1") == 1,
                "partida completa não gravada");
            Exigir(ValorInteiro(caminho, "SELECT total_residuos FROM partidas WHERE id = 1") == 12 &&
                ValorInteiro(caminho, "SELECT residuos_coletados FROM resumo_partidas WHERE partida_id = 1") == 12 &&
                ValorInteiro(caminho, "SELECT pontuacao FROM resumo_partidas WHERE partida_id = 1") == 120 &&
                ValorInteiro(caminho, "SELECT count(*) FROM coletas WHERE partida_id = 1") == 12 &&
                ValorInteiro(caminho, "SELECT count(DISTINCT escala) FROM coletas WHERE partida_id = 1") == 4, "coletas da partida completa");
            Exigir(!jogo.GetNode<Label>("%ResultSummary").Text.Contains("SEMENTE"), "semente ainda aparece para o participante");

            // Quem joga de novo faz outro cadastro: a tela final só volta ao início.
            Exigir(jogo.GetNodeOrNull("%NewGameButton") is null, "botão de nova partida voltou à tela final");
            jogo.GetNode<Button>("%ReturnButton").EmitSignal(BaseButton.SignalName.Pressed);
            Iniciar(cadastrar: true);
            Exigir(ValorInteiro(caminho, "SELECT participante_id FROM partidas WHERE id = 2") == 2 &&
                ValorInteiro(caminho, "SELECT count(*) FROM participantes") == 2, "segunda pessoa não ganhou cadastro próprio");
            Tocar(jogo, CentroNoInicio(ResiduosPara(jogo, "facil", jogo.SementeAtual)[0]));
            jogo.GetNode<Button>("%LeaveButton").EmitSignal(BaseButton.SignalName.Pressed);
            jogo.GetNode<Button>("%LeaveConfirmButton").EmitSignal(BaseButton.SignalName.Pressed);
            Exigir(Situacao(2) == "abandonou" && ValorInteiro(caminho, "SELECT residuos_coletados FROM resumo_partidas WHERE partida_id = 2") == 1,
                "abandono não gravado");

            Iniciar(cadastrar: false);
            jogo.GetNode<Button>("%HintButton").EmitSignal(BaseButton.SignalName.Pressed);
            jogo._Process(42);
            Exigir(Situacao(3) == "tempo_esgotado" && Valor(caminho, "SELECT participante_id FROM partidas WHERE id = 3") is DBNull &&
                ValorInteiro(caminho, "SELECT dicas_usadas FROM resumo_partidas WHERE partida_id = 3") == 1 &&
                (double)Valor(caminho, "SELECT segundos_jogados FROM partidas WHERE id = 3")! == 42, "fim por tempo não gravado");
            jogo.GetNode<Button>("%ReturnButton").EmitSignal(BaseButton.SignalName.Pressed);
            Exigir(Situacao(3) == "tempo_esgotado", "voltar ao menu alterou partida encerrada");

            // Queda de energia: a aplicação some com a partida em andamento.
            Iniciar(cadastrar: false);
            telaPrincipal.Free();
            Exigir(Situacao(4) == "em_andamento", "partida em andamento não ficou registrada");
            var reaberta = NovaTelaPrincipal(caminho);
            AddChild(reaberta);
            reaberta.Free();
            Exigir(Situacao(4) == "interrompida", "reabertura não marcou interrupção");
        }
        finally
        {
            foreach (var filho in GetChildren().OfType<TelaPrincipal>()) filho.Free();
        }
    });

    private void VerificarContrasteBotoes()
    {
        var telaPrincipal = NovaTelaPrincipal();
        AddChild(telaPrincipal);
        try
        {
            foreach (var tipo in new[] { "Button", "PrimaryButton", "SecondaryButton", "SuggestionButton" })
            foreach (var estado in new[] { "normal", "hover", "pressed" })
            {
                var botao = new Button { ThemeTypeVariation = tipo };
                telaPrincipal.AddChild(botao);
                var estilo = (StyleBoxFlat)botao.GetThemeStylebox(estado);
                var texto = botao.GetThemeColor(estado == "normal" ? "font_color" : $"font_{estado}_color");
                var primeiro = Luminancia(estilo.BgColor);
                var segundo = Luminancia(texto);
                var contraste = (Math.Max(primeiro, segundo) + 0.05) / (Math.Min(primeiro, segundo) + 0.05);
                Exigir(contraste >= 4.5, $"contraste {tipo}/{estado}: {contraste:F2}");
            }
        }
        finally { telaPrincipal.Free(); }
    }

    private static double Luminancia(Color cor)
    {
        static double Linear(float canal) => canal <= 0.04045 ? canal / 12.92 : Math.Pow((canal + 0.055) / 1.055, 2.4);
        return 0.2126 * Linear(cor.R) + 0.7152 * Linear(cor.G) + 0.0722 * Linear(cor.B);
    }

    private void VerificarApresentacaoTexto()
    {
        var telaPrincipal = NovaTelaPrincipal();
        AddChild(telaPrincipal);
        try
        {
            static void Inspecionar(Node no)
            {
                var texto = no switch
                {
                    Label rotulo => rotulo.Text,
                    Button botao => botao.Text,
                    _ => ""
                };
                Exigir(!texto.Any(char.IsLower), $"texto fora de caixa alta em {no.GetPath()}");
                if (no is Control controle && no is Label or Button &&
                    controle.GetThemeColor("font_color").R > 0.9f)
                {
                    var contorno = controle.GetThemeColor("font_outline_color");
                    Exigir(controle.GetThemeConstant("outline_size") > 0 && contorno.R == 0 &&
                        contorno.G == 0 && contorno.B == 0 && contorno.A > 0,
                        $"texto claro sem contorno preto em {no.GetPath()}");
                }
                foreach (var filho in no.GetChildren()) Inspecionar(filho);
            }
            Inspecionar(telaPrincipal);
            telaPrincipal.GetNode<Button>("%PlayButton").EmitSignal(BaseButton.SignalName.Pressed);
            telaPrincipal.PularCadastro();
            telaPrincipal.GetNode<Container>("%DifficultyInput").GetChild<Button>(1).ButtonPressed = true;
            telaPrincipal.GetNode<Button>("%RulesStartButton").EmitSignal(BaseButton.SignalName.Pressed);
            Inspecionar(telaPrincipal);
        }
        finally { telaPrincipal.Free(); }
    }

    private void VerificarCamposCadastro()
    {
        var telaPrincipal = NovaTelaPrincipal();
        AddChild(telaPrincipal);
        try
        {
            var formulario = telaPrincipal.GetNode<FormularioCadastro>("%FormularioCadastro");
            var lista = telaPrincipal.GetNode<Control>("%SelectionList");
            var campos = new[] { "CityInput", "SchoolInput", "EducationLevelInput", "SchoolYearInput" };
            Exigir(formulario.GetNode<Button>("SchoolYearInput").Disabled, "ano liberado antes da etapa");
            Escolher(formulario, "EducationLevelInput");
            var alturas = new List<float>();
            foreach (var nome in campos)
            {
                var campo = formulario.GetNode<Button>(nome);
                Exigir(campo.ThemeTypeVariation == "FieldButton" && campo.CustomMinimumSize == formulario.GetNode<Button>(campos[0]).CustomMinimumSize,
                    $"{nome} fora do padrão dos campos");
                campo.EmitSignal(BaseButton.SignalName.Pressed);
                Exigir(lista.Visible && LinhasDaLista(formulario).Length >= 5, $"{nome} não abriu a lista");
                alturas.Add(lista.Size.Y);
            }
            Exigir(alturas.Distinct().Count() == 1, $"listas com alturas diferentes: {string.Join(", ", alturas)}");
            formulario.GetNode<Button>("EducationLevelInput").EmitSignal(BaseButton.SignalName.Pressed);
            Exigir(LinhasDaLista(formulario).Any(linha => linha.Text == "EDUCAÇÃO INFANTIL"), "etapa da pré-escola ausente");
            LinhasDaLista(formulario).Single(linha => linha.Text == "DOCENTE").EmitSignal(BaseButton.SignalName.Pressed);
            Exigir(formulario.GetNode<Button>("SchoolYearInput").Text == "PROFESSOR(A)", "perfil docente não foi preenchido");
            Escolher(formulario, "CityInput");
            Escolher(formulario, "SchoolInput");
            formulario.GetNode<Button>("AgeAndPlayedRow/AgeColumn/AgeInput/AgePlusButton").EmitSignal(BaseButton.SignalName.Pressed);
            Exigir(formulario.TentarLer(out var docente, out _) && docente!.EtapaEnsino == "DOCENTE" && docente.AnoSerie == "PROFESSOR(A)",
                "cadastro de docente não foi aceito");
        }
        finally { telaPrincipal.Free(); }
    }

    private async Task VerificarBotaoToque()
    {
        var telaPrincipal = NovaTelaPrincipal();
        AddChild(telaPrincipal);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var botao = telaPrincipal.GetNode<Button>("%PlayButton");
        var ponto = botao.GlobalPosition + botao.Size / 2;
        GetViewport().PushInput(new InputEventMouseMotion { Position = ponto, GlobalPosition = ponto }, true);
        GetViewport().PushInput(new InputEventScreenTouch { Index = 0, Pressed = true, Position = ponto }, true);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetViewport().PushInput(new InputEventScreenTouch { Index = 0, Position = ponto }, true);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Verificar("botão inicial responde a toque real no viewport", () =>
            Exigir(telaPrincipal.GetNode<Control>("%DataScreen").Visible, "toque não abriu cadastro"));
        telaPrincipal.Free();
    }

    // Simula o clique repetido que o Windows gera depois de cada toque.
    private async Task VerificarCliqueDoToque()
    {
        var telaPrincipal = NovaTelaPrincipal();
        AddChild(telaPrincipal);
        try
        {
            async Task Enviar(InputEvent evento)
            {
                GetViewport().PushInput(evento, true);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            async Task TocarTela(Vector2 ponto)
            {
                await Enviar(new InputEventScreenTouch { Index = 0, Pressed = true, Position = ponto });
                await Enviar(new InputEventScreenTouch { Index = 0, Position = ponto });
            }
            async Task ClicarMouse(Vector2 ponto)
            {
                await Enviar(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = ponto, GlobalPosition = ponto });
                await Enviar(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Position = ponto, GlobalPosition = ponto });
            }
            async Task EsperarJanela() => await ToSignal(GetTree().CreateTimer(
                (FiltroCliqueDoToque.JanelaMilissegundos + 200) / 1000.0), SceneTreeTimer.SignalName.Timeout);
            static Vector2 Centro(Control controle) => controle.GlobalPosition + controle.Size / 2;

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await TocarTela(Centro(telaPrincipal.GetNode<Button>("%PlayButton")));
            Exigir(telaPrincipal.GetNode<Control>("%DataScreen").Visible, "toque não abriu cadastro");
            var voltar = telaPrincipal.GetNode<Button>("%DataBackButton");
            await ClicarMouse(Centro(voltar));
            Verificar("clique repetido do toque não aciona o botão que surge embaixo", () =>
                Exigir(telaPrincipal.GetNode<Control>("%DataScreen").Visible, "clique repetido voltou ao menu"));
            await EsperarJanela();
            await ClicarMouse(Centro(voltar));
            Verificar("mouse sem toque recente continua funcionando", () =>
                Exigir(telaPrincipal.GetNode<Control>("%MenuScreen").Visible, "clique de mouse não voltou ao menu"));

            telaPrincipal.PularCadastro();
            telaPrincipal.GetNode<Container>("%DifficultyInput").GetChild<Button>(0).ButtonPressed = true;
            await TocarTela(Centro(telaPrincipal.GetNode<Button>("%RulesStartButton")));
            var jogo = telaPrincipal.GetNode<ControladorPartida>("GameScreen");
            Exigir(jogo.EmAndamento, "toque não iniciou a partida");
            jogo.SetProcess(false);
            var cenario = jogo.CenariosSelecionaveis[0];
            var residuo = ResiduosPara(jogo, cenario.Id, jogo.SementeAtual).First(item => item.IdVista == cenario.Vistas[0].Id);
            var ponto = jogo.GetGlobalTransformWithCanvas() * (CentroNoInicio(residuo) / RegrasJogo.TamanhoTela * jogo.Size);
            var placar = jogo.GetNode<Label>("%ScenarioLabel");
            var antes = placar.Text;
            await ClicarMouse(ponto);
            Verificar("clique repetido do toque não coleta resíduo ao iniciar", () =>
                Exigir(placar.Text == antes, "resíduo coletado pelo clique repetido"));
            await EsperarJanela();
            await ClicarMouse(ponto);
            Exigir(placar.Text != antes, "clique de mouse não coletou o resíduo");
        }
        finally { telaPrincipal.Free(); }
    }

    private async Task VerificarRolagemResultado()
    {
        var telaPrincipal = NovaTelaPrincipal();
        AddChild(telaPrincipal);
        var conteudo = ConteudoJogo.Carregar();
        var partida = new Partida();
        var tipos = conteudo.Coletaveis.Values.Select(item => item.Tipo).Distinct().ToArray();
        partida.Iniciar(tipos.Length);
        var indice = 0;
        foreach (var tipo in tipos) partida.Coletar(indice++, tipo);
        var jogo = telaPrincipal.GetNode<ControladorPartida>("GameScreen");
        telaPrincipal.GetNode<Control>("%MenuScreen").Hide();
        jogo.Show();
        var resultados = jogo.GetNode<PainelResultado>("%ResultPanel");
        resultados.MostrarResumo(partida, conteudo.Cenarios[0].MensagemFinal, conteudo.Mensagens);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var rolagem = resultados.GetNode<ScrollContainer>("Margin");
        var botaoVoltar = jogo.GetNode<Button>("%ReturnButton");
        rolagem.EnsureControlVisible(botaoVoltar);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Verificar("resultado com muitos tipos permanece alcançável", () =>
        {
            Exigir(resultados.Size.Y <= GetViewport().GetVisibleRect().Size.Y, "resultado ultrapassa tela");
            Exigir(botaoVoltar.GetGlobalRect().End.Y <= 720 && botaoVoltar.GetGlobalRect().Position.Y >= 0,
                "rolagem não alcançou retorno");
            resultados.Limpar();
            Exigir(rolagem.ScrollVertical == 0, "nova sessão preservou rolagem anterior");
        });
        telaPrincipal.Free();
    }

    private async Task CapturarPrevias()
    {
        Exigir(DisplayServer.GetName() != "headless", "--preview precisa de uma sessão gráfica");
        var pasta = Path.Combine(Path.GetTempPath(), "caca-residuos-previews-" + Guid.NewGuid());
        Directory.CreateDirectory(pasta);
        var telaPrincipal = NovaTelaPrincipal();
        AddChild(telaPrincipal);
        try
        {
            async Task Capturar(string nome)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var captura = GetViewport().GetTexture().GetImage();
                var caminho = Path.Combine(pasta, nome + ".png");
                Exigir(captura.SavePng(caminho) == Error.Ok, "falha ao salvar captura");
                GD.Print($"PREVIEW: {caminho}");
            }

            await Capturar("menu");
            foreach (var (cenario, indice) in ConteudoJogo.Carregar().Cenarios.Select((cenario, indice) => (cenario, indice)))
            {
                telaPrincipal.GetNode<Button>("%PlayButton").EmitSignal(BaseButton.SignalName.Pressed);
                if (cenario.Id == "facil") await Capturar("cadastro-vazio");
                telaPrincipal.PularCadastro();
                telaPrincipal.GetNode<Container>("%DifficultyInput").GetChild<Button>(indice).ButtonPressed = true;
                if (cenario.Id == "facil") await Capturar("como-jogar");
                telaPrincipal.GetNode<Button>("%RulesStartButton").EmitSignal(BaseButton.SignalName.Pressed);
                var jogo = telaPrincipal.GetNode<ControladorPartida>("GameScreen");
                jogo.IniciarComSemente(cenario.Id, 123);
                jogo.SetProcess(false);
                foreach (var vista in cenario.Vistas)
                {
                    Visitar(jogo, cenario, vista);
                    jogo._Process(0.3);
                    await Capturar($"{cenario.Id}-{vista.Id}");
                }
                jogo.IniciarComSemente(cenario.Id, 123);
                jogo.SetProcess(false);
                var residuos = ResiduosPara(jogo, cenario.Id);
                foreach (var vista in cenario.Vistas)
                {
                    Visitar(jogo, cenario, vista);
                    foreach (var residuo in residuos.Where(residuo => residuo.IdVista == vista.Id))
                        Tocar(jogo, CentroNoInicio(residuo));
                }
                Exigir(jogo.GetNode<Control>("%ResultPanel").Visible, "coleta completa não abriu resultado para captura");
                await Capturar($"{cenario.Id}-resultado");
                jogo.GetNode<Button>("%LeaveButton").EmitSignal(BaseButton.SignalName.Pressed);
                jogo.GetNode<Button>("%LeaveConfirmButton").EmitSignal(BaseButton.SignalName.Pressed);
            }
        }
        finally { telaPrincipal.Free(); }
    }
}
