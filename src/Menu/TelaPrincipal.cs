using CacaAosResiduos.Cadastro;
using CacaAosResiduos.Jogo;
using CacaAosResiduos.Persistencia;
using Godot;

namespace CacaAosResiduos.Menu;

// Troca as telas, guarda o cadastro da pessoa atual e liga a partida ao banco.
public partial class TelaPrincipal : Control
{
    // Segurar o título do cadastro por esse tempo abre o modo de teste.
    private const double SegundosToqueLongo = 3;
    [Export] public double SegundosInatividade { get; set; } = 180;
    [Export] public string CaminhoBanco { get; set; } = "user://cacaaosresiduos.sqlite3";

    private Control _menu = null!;
    private Control _telaRegras = null!;
    private Control _telaCadastro = null!;
    private ControladorPartida _jogo = null!;
    private FormularioCadastro _formularioCadastro = null!;
    private TextureRect _fundoSitio = null!;
    private Label _validacao = null!;
    private Control _selecaoNivel = null!;
    private Label _avisoBanco = null!;
    private Label _resultadoCopia = null!;
    private Godot.Timer _toqueLongo = null!;
    private readonly ButtonGroup _grupoNivel = new();
    private readonly List<Button> _cartoesNivel = [];

    private string _caminhoBanco = "";
    private FileStream? _travaBanco;
    private BancoCadastros _bancoCadastros = null!;
    private BancoPartidas _bancoPartidas = null!;
    private RegistroPartida _registroPartida = null!;

    private bool _modoTeste;
    private DadosCadastro? _cadastroPendente;
    private long? _idParticipante;
    private double _segundosInativo;

    // Roda uma vez por execução, não a cada tela criada.
    static TelaPrincipal()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, evento) =>
            GD.PushError($"Exceção não tratada: {evento.ExceptionObject}");
        TaskScheduler.UnobservedTaskException += (_, evento) =>
            GD.PushError($"Exceção não observada em tarefa: {evento.Exception}");
    }

    public override void _Ready()
    {
        Theme = TemaJogo.Criar();
        AddChild(new FiltroCliqueDoToque { Name = "FiltroCliqueDoToque" });
        DisplayServer.WindowSetTitle("CAÇA AOS RESÍDUOS: DO MACRO AO MICRO");

        _menu = GetNode<Control>("%MenuScreen");
        _telaRegras = GetNode<Control>("%RulesScreen");
        _telaCadastro = GetNode<Control>("%DataScreen");
        _jogo = GetNode<ControladorPartida>("GameScreen");
        _formularioCadastro = GetNode<FormularioCadastro>("%FormularioCadastro");
        _fundoSitio = GetNode<TextureRect>("SiteBackground");
        _validacao = GetNode<Label>("%ValidationMessage");
        _selecaoNivel = GetNode<Control>("%DifficultySelection");
        _avisoBanco = GetNode<Label>("%DatabaseWarning");
        _resultadoCopia = GetNode<Label>("%CopyDataResult");
        CriarCartoesNivel(GetNode<Container>("%DifficultyInput"));
        PreencherRegras();
        PrepararToqueLongo(GetNode<Control>("%DataTitle"));

        _caminhoBanco = ProjectSettings.GlobalizePath(CaminhoBanco);
        _travaBanco = TravaBanco.Tentar(_caminhoBanco, out var travou);
        if (!travou)
        {
            GD.PushError("Outra cópia do jogo já está aberta; esta será fechada.");
            GetTree().Quit(1);
            return;
        }
        _bancoCadastros = new BancoCadastros(_caminhoBanco);
        _bancoPartidas = new BancoPartidas(_caminhoBanco);
        _registroPartida = new RegistroPartida(_bancoPartidas);
        PrepararBanco();

        GetNode<Button>("%PlayButton").Pressed += MostrarCadastro;
        GetNode<Button>("%RulesBackButton").Pressed += MostrarCadastro;
        GetNode<Button>("%RulesStartButton").Pressed += IniciarPartida;
        GetNode<Button>("%DataBackButton").Pressed += MostrarMenu;
        GetNode<Button>("%DataContinueButton").Pressed += MostrarRegras;
        GetNode<Button>("%CopyDataButton").Pressed += CopiarDados;
        _jogo.VoltarSolicitado += MostrarMenu;
        _jogo.ResiduoColetado += _registroPartida.RegistrarColeta;
        _jogo.DicaUsada += _registroPartida.RegistrarDica;
        _jogo.PartidaEncerrada += _registroPartida.Encerrar;
        MostrarMenu();
    }

    public override void _ExitTree() => _travaBanco?.Dispose();

    public override void _Input(InputEvent evento)
    {
        if (evento is InputEventMouseButton or InputEventScreenTouch or InputEventScreenDrag or InputEventKey)
            _segundosInativo = 0;
    }

    // Sessão parada volta ao início para não mostrar dados ao próximo visitante.
    public override void _Process(double delta)
    {
        if (_menu.Visible || SegundosInatividade <= 0)
            return;
        _segundosInativo += delta;
        if (_segundosInativo >= SegundosInatividade)
            MostrarMenu();
    }

    private void IniciarPartida()
    {
        var nivel = _cartoesNivel.FindIndex(cartao => cartao.ButtonPressed);
        if (_jogo.EmAndamento || (_modoTeste && nivel < 0) || (!_modoTeste && _cadastroPendente is null))
            return;
        if (_cadastroPendente is not null && _idParticipante is null)
        {
            try
            {
                // Repete a preparação do banco caso tenha falhado na abertura.
                _bancoCadastros.Inicializar();
                _idParticipante = _bancoCadastros.Salvar(_cadastroPendente);
            }
            catch (Exception erro) when (BancoLocal.EhFalhaDeGravacao(erro))
            {
                GD.PushError($"Falha ao salvar o cadastro local: {erro.GetType().Name}");
                _avisoBanco.Visible = true;
                MostrarErroCadastro("NÃO FOI POSSÍVEL SALVAR. TENTE DE NOVO; SE CONTINUAR, PEÇA AJUDA À EQUIPE.");
                return;
            }
        }
        try
        {
            var idCenario = _modoTeste
                ? _jogo.CenariosSelecionaveis[nivel].Id
                : RegrasJogo.CenarioPorIdade(_cadastroPendente!.Idade);
            _jogo.IniciarPartida(idCenario);
            var versaoJogo = ProjectSettings.GetSetting("application/config/version").AsString();
            _registroPartida.Iniciar(_idParticipante, idCenario, versaoJogo, _jogo.VersaoConteudo, _jogo.TotalResiduos);
            MostrarTela(_jogo);
        }
        catch (InvalidOperationException erro)
        {
            GD.PushError($"Falha ao preparar o cenário: {erro.GetType().Name}");
            _jogo.PararELimpar();
            MostrarErroCadastro("NÃO FOI POSSÍVEL ABRIR A PARTIDA. PEÇA AJUDA À EQUIPE.");
        }
    }

    private void MostrarErroCadastro(string mensagem)
    {
        MostrarCadastro();
        _validacao.Text = mensagem;
        _validacao.Visible = true;
    }

    private void MostrarRegras()
    {
        if (!_formularioCadastro.TentarLer(out _cadastroPendente, out var erro))
        {
            _validacao.Text = erro;
            _validacao.Visible = true;
            return;
        }
        _validacao.Visible = false;
        _modoTeste = false;
        _selecaoNivel.Visible = false;
        MostrarTela(_telaRegras);
    }

    // Modo de teste: sem cadastro, com escolha manual da fase. A partida fica sem participante.
    public void PularCadastro()
    {
        _cadastroPendente = null;
        _idParticipante = null;
        _validacao.Visible = false;
        _modoTeste = true;
        _selecaoNivel.Visible = true;
        MostrarTela(_telaRegras);
    }

    // Mantém o que já foi preenchido.
    private void MostrarCadastro()
    {
        _modoTeste = false;
        MostrarTela(_telaCadastro);
    }

    private void PrepararToqueLongo(Control titulo)
    {
        _toqueLongo = new Godot.Timer { OneShot = true, WaitTime = SegundosToqueLongo };
        _toqueLongo.Timeout += () =>
        {
            if (_telaCadastro.Visible)
                PularCadastro();
        };
        AddChild(_toqueLongo);
        titulo.MouseFilter = MouseFilterEnum.Stop;
        titulo.GuiInput += evento =>
        {
            bool? pressionado = evento switch
            {
                InputEventScreenTouch toque => toque.Pressed && !toque.Canceled,
                InputEventMouseButton botao when botao.ButtonIndex == MouseButton.Left => botao.Pressed,
                _ => null
            };
            if (pressionado == true && _toqueLongo.IsStopped())
                _toqueLongo.Start();
            else if (pressionado == false)
                _toqueLongo.Stop();
        };
    }

    // Limpa a sessão inteira para a próxima pessoa.
    private void MostrarMenu()
    {
        // Sair no meio da partida, por botão ou inatividade, conta como abandono.
        if (_jogo.EmAndamento)
            _registroPartida.Encerrar(SituacaoPartida.Abandonou, _jogo.SegundosJogados);
        _cadastroPendente = null;
        _idParticipante = null;
        _modoTeste = false;
        _cartoesNivel[0].ButtonPressed = true;
        _validacao.Visible = false;
        _validacao.Text = "";
        _resultadoCopia.Text = "";
        _formularioCadastro.Limpar();
        _jogo.PararELimpar();
        GetViewport().GuiReleaseFocus();
        MostrarTela(_menu);
    }

    private void PrepararBanco()
    {
        try
        {
            if (_bancoCadastros.Inicializar() is { } guardado)
                GD.PushWarning($"Banco de outra versão guardado como {Path.GetFileName(guardado)}; um banco novo foi criado.");
            if (!BancoLocal.VerificacaoRapida(_caminhoBanco))
            {
                GD.PushError("A verificação rápida do banco local encontrou problemas.");
                _avisoBanco.Visible = true;
            }
            var interrompidas = _bancoPartidas.MarcarInterrompidas();
            if (interrompidas > 0)
                GD.Print($"Partidas interrompidas marcadas na abertura: {interrompidas}");
        }
        catch (Exception erro) when (BancoLocal.EhFalhaDeGravacao(erro))
        {
            GD.PushError($"Falha ao preparar o banco local: {erro.GetType().Name}");
            _avisoBanco.Visible = true;
        }
    }

    private void MostrarTela(Control tela)
    {
        _segundosInativo = 0;
        _toqueLongo.Stop();
        _menu.Visible = tela == _menu;
        _telaRegras.Visible = tela == _telaRegras;
        _telaCadastro.Visible = tela == _telaCadastro;
        _jogo.Visible = tela == _jogo;
        _fundoSitio.Visible = !_jogo.Visible;
    }

    // Os textos da cena são só prévia no editor; os números vêm de RegrasJogo.
    private void PreencherRegras()
    {
        var cartoes = _telaRegras.GetNode("Panel/Margin/Content/RoundInfo");
        cartoes.GetNode<Label>("TimeCard/Text").Text = $"{RegrasJogo.LimiteSegundos} SEGUNDOS";
        cartoes.GetNode<Label>("PointsCard/Text").Text = $"{RegrasJogo.PontosPorColeta} PONTOS POR COLETA";
        cartoes.GetNode<Label>("HintsCard/Text").Text =
            $"{RegrasJogo.MaximoDicas} DICAS · CADA UMA\nCUSTA {RegrasJogo.PenalidadeDicaSegundos} SEGUNDOS";
    }

    // A API de backup do SQLite copia com segurança mesmo com o jogo aberto.
    private void CopiarDados()
    {
        var destino = Path.Combine(Path.GetDirectoryName(_caminhoBanco)!, "copias",
            $"cacaaosresiduos-{DateTime.Now:yyyy-MM-dd-HHmmss}.sqlite3");
        try
        {
            BancoLocal.FazerBackup(_caminhoBanco, destino);
            if (!BancoLocal.VerificacaoRapida(destino))
                throw new InvalidOperationException("Cópia não passou na verificação de integridade.");
            var (participantes, partidas) = BancoLocal.Contagens(destino);
            _resultadoCopia.Text = $"CÓPIA ÍNTEGRA: {participantes} CADASTROS, {partidas} PARTIDAS.\n{destino}";
        }
        catch (Exception erro) when (BancoLocal.EhFalhaDeGravacao(erro))
        {
            GD.PushError($"Falha ao copiar os dados: {erro.GetType().Name}");
            _resultadoCopia.Text = "NÃO FOI POSSÍVEL COPIAR OS DADOS.";
        }
    }

    // Um cartão por cenário selecionável do game.json, na mesma ordem.
    private void CriarCartoesNivel(Container destino)
    {
        foreach (var _ in _jogo.CenariosSelecionaveis)
        {
            var cartao = new Button
            {
                ToggleMode = true,
                ButtonGroup = _grupoNivel,
                ThemeTypeVariation = "LevelCard",
                CustomMinimumSize = new Vector2(0, 124),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                Alignment = HorizontalAlignment.Center
            };
            cartao.Toggled += _ => AtualizarTextosNivel();
            destino.AddChild(cartao);
            _cartoesNivel.Add(cartao);
        }
        _cartoesNivel[0].ButtonPressed = true;
        AtualizarTextosNivel();
    }

    private void AtualizarTextosNivel()
    {
        for (var i = 0; i < _cartoesNivel.Count; i++)
        {
            var cenario = _jogo.CenariosSelecionaveis[i];
            var estado = _cartoesNivel[i].ButtonPressed ? "NÍVEL ESCOLHIDO" : "TOQUE PARA ESCOLHER";
            _cartoesNivel[i].Text = $"{cenario.Nome}\n{cenario.Local}\n{estado}";
        }
    }
}
