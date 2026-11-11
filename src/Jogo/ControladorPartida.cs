using Godot;

namespace CacaAosResiduos.Jogo;

public partial class ControladorPartida : Control
{
    private const int TentativasSorteio = 20;
    // A dica pulsa perto do resíduo, nunca em cima dele.
    private static readonly Vector2[] s_deslocamentosDica =
    [
        new(36, -28), new(-32, 25), new(28, 32), new(-38, -22), new(25, -35)
    ];

    public delegate void ResiduoColetadoHandler(string tipo, int escala, int pontos, double segundoColeta);
    public delegate void PartidaEncerradaHandler(SituacaoPartida situacao, double segundosJogados);

    public event Action? VoltarSolicitado;
    public event ResiduoColetadoHandler? ResiduoColetado;
    public event Action<double>? DicaUsada;
    public event PartidaEncerradaHandler? PartidaEncerrada;

    // Resíduo recém-coletado que some aos poucos para confirmar o toque.
    private class EfeitoColeta(Residuo residuo, Vector2 centro)
    {
        public Residuo Residuo => residuo;
        public Vector2 Centro => centro;
        public double SegundosRestantes { get; set; } = RegrasJogo.EfeitoColetaSegundos;
    }

    private ConteudoJogo _conteudo = null!;
    private readonly Partida _partida = new();
    private readonly GestoToque _gesto = new();
    private readonly List<Residuo> _residuos = [];
    private readonly List<EfeitoColeta> _efeitosColeta = [];
    private readonly Dictionary<string, ArteColetavel> _arteColetaveis = [];
    private readonly Dictionary<string, Texture2D> _fundos = [];
    private readonly Dictionary<string, MapaVista> _mapas = [];

    private Cenario? _cenario;
    private Vista? _vista;

    private Label _rotuloTempo = null!;
    private Label _rotuloPontuacao = null!;
    private Label _rotuloEscala = null!;
    private Label _rotuloResiduos = null!;
    private Label _rotuloAviso = null!;
    private Control _painelAviso = null!;
    private PainelResultado _painelResultado = null!;
    private Button _botaoDica = null!;
    private Control _confirmacaoSaida = null!;
    private Tween? _tweenPontuacao;
    private Tween? _tweenAviso;

    private Vector2? _centroDica;
    private string _idVistaDica = "";
    private double _segundosPulsoDica;
    private double _segundosTransicao;

    public long SementeAtual { get; private set; }
    public bool EmAndamento => _partida.EmAndamento;
    public int TotalResiduos => _partida.TotalResiduos;
    public string VersaoConteudo => _conteudo.Versao;
    public double SegundosJogados => _partida.SegundosDecorridos;
    public IReadOnlyList<Cenario> CenariosSelecionaveis { get; private set; } = [];

    public override void _Ready()
    {
        _rotuloTempo = GetNode<Label>("%TimeLabel");
        _rotuloPontuacao = GetNode<Label>("%ScoreLabel");
        _rotuloEscala = GetNode<Label>("%ScaleLabel");
        _rotuloResiduos = GetNode<Label>("%ScenarioLabel");
        _rotuloAviso = GetNode<Label>("%LayerNotice");
        _painelAviso = GetNode<Control>("%NoticePanel");
        _painelResultado = GetNode<PainelResultado>("%ResultPanel");
        _botaoDica = GetNode<Button>("%HintButton");
        _confirmacaoSaida = GetNode<Control>("%LeaveConfirm");

        _botaoDica.Pressed += UsarDica;
        // Sair no meio da partida pede confirmação.
        GetNode<Button>("%LeaveButton").Pressed += () =>
        {
            if (_partida.EmAndamento)
                _confirmacaoSaida.Show();
            else
                VoltarSolicitado?.Invoke();
        };
        GetNode<Button>("%LeaveCancelButton").Pressed += () => _confirmacaoSaida.Hide();
        GetNode<Button>("%LeaveConfirmButton").Pressed += () =>
        {
            _confirmacaoSaida.Hide();
            VoltarSolicitado?.Invoke();
        };
        GetNode<Button>("%ReturnButton").Pressed += () => VoltarSolicitado?.Invoke();

        _conteudo = ConteudoJogo.Carregar();
        CenariosSelecionaveis = [.. _conteudo.Cenarios.Where(cenario => cenario.Selecionavel)];
        foreach (var (id, item) in _conteudo.Coletaveis)
            _arteColetaveis[id] = new ArteColetavel(item.Textura);
        SetProcess(false);
        SetProcessInput(false);
    }

    public void IniciarPartida(string idCenario) => IniciarComSemente(idCenario, Random.Shared.NextInt64(1, long.MaxValue));

    public void IniciarComSemente(string idCenario, long semente)
    {
        var cenario = _conteudo.Cenarios.FirstOrDefault(cenario => cenario.Id == idCenario)
            ?? throw new InvalidOperationException($"Cenário inexistente: {idCenario}.");
        PararELimpar();
        _cenario = cenario;
        _vista = cenario.Vistas[0];
        foreach (var vista in cenario.Vistas)
        {
            _fundos[vista.Id] = CarregarTextura(vista.Fundo);
            if (vista.CenaMapa is null)
                continue;
            var cena = GD.Load<PackedScene>(vista.CenaMapa)
                ?? throw new InvalidOperationException($"Mapa não encontrado: {vista.CenaMapa}.");
            var mapa = cena.Instantiate<MapaVista>();
            AddChild(mapa);
            _mapas[vista.Id] = mapa;
            mapa.ValidarPara(vista, cenario, _conteudo.Coletaveis);
        }

        // Se um resíduo grande não couber (raro), tenta a semente seguinte.
        List<ResiduoSorteado>? sorteados = null;
        for (var tentativa = 0; sorteados is null; tentativa++)
        {
            try { sorteados = SorteioResiduos.Sortear(cenario, _mapas, _conteudo.Coletaveis, (ulong)semente); }
            catch (InvalidOperationException) when (tentativa < TentativasSorteio)
            {
                GD.PushWarning($"Sorteio sem espaço em {cenario.Id}; tentando outra semente.");
                semente = semente == long.MaxValue ? 1 : semente + 1;
            }
        }
        SementeAtual = semente;
        foreach (var residuo in sorteados)
            _residuos.Add(new Residuo(residuo, _conteudo.Coletaveis[residuo.IdItem].Tipo, _arteColetaveis[residuo.IdItem]));

        _partida.Iniciar(_residuos.Count);
        AtualizarHud();
        SetProcess(true);
        SetProcessInput(true);
        QueueRedraw();
    }

    public void PararELimpar()
    {
        _partida.Limpar();
        SetProcess(false);
        SetProcessInput(false);
        _residuos.Clear();
        _efeitosColeta.Clear();
        LimparMapas();
        _fundos.Clear();
        _cenario = null;
        _vista = null;
        SementeAtual = 0;
        _centroDica = null;
        _idVistaDica = "";
        _segundosPulsoDica = 0;
        _segundosTransicao = 0;
        _tweenPontuacao?.Kill();
        _tweenPontuacao = null;
        _rotuloPontuacao.Scale = Vector2.One;
        _tweenAviso?.Kill();
        _tweenAviso = null;
        _rotuloAviso.Text = "";
        _painelAviso.Hide();
        _confirmacaoSaida.Hide();
        _painelResultado.Limpar();
        _rotuloEscala.Text = "";
        _rotuloResiduos.Text = "";
        _rotuloTempo.Text = "";
        _rotuloPontuacao.Text = "";
        _gesto.Limpar();
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        // O tempo para enquanto a confirmação de saída está aberta.
        if (!_partida.EmAndamento || _confirmacaoSaida.Visible)
            return;

        _partida.Avancar(delta);
        foreach (var efeito in _efeitosColeta)
            efeito.SegundosRestantes -= delta;
        _efeitosColeta.RemoveAll(efeito => efeito.SegundosRestantes <= 0);
        _segundosPulsoDica = Math.Max(0, _segundosPulsoDica - delta);
        _segundosTransicao = Math.Max(0, _segundosTransicao - delta);
        if (_segundosPulsoDica <= 0)
            _centroDica = null;

        _rotuloTempo.Text = TextoTempo();
        QueueRedraw();
        if (!_partida.EmAndamento)
            EncerrarPartida();
    }

    public override void _Input(InputEvent evento)
    {
        if (!_partida.EmAndamento || !IsVisibleInTree() || _confirmacaoSaida.Visible)
            return;

        switch (evento)
        {
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp } roda when EhAreaJogo(roda.Position):
                TentarAmpliar();
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelDown } roda when EhAreaJogo(roda.Position):
                Reduzir();
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } clique:
                TratarToque(clique.Position);
                break;
            case InputEventScreenTouch toque:
                if (toque.Pressed && EhAreaJogo(toque.Position))
                    _gesto.Pressionar(toque.Index, toque.Position);
                else if (!toque.Pressed && _gesto.Soltar(toque.Index, toque.Position, toque.Canceled) is { } toqueRapido)
                    TratarToque(toqueRapido);
                break;
            case InputEventScreenDrag arraste:
                _gesto.Arrastar(arraste.Index, arraste.Position, TentarAmpliar, Reduzir);
                break;
        }
    }

    // Tudo é desenhado numa tela lógica de 1280 × 720 e escalado para o tamanho real.
    public override void _Draw()
    {
        if (_cenario is null)
            return;

        DrawSetTransform(Vector2.Zero, 0, Size / RegrasJogo.TamanhoTela);
        var fundo = _fundos[_vista!.Id];
        DrawTextureRectRegion(fundo, new Rect2(Vector2.Zero, RegrasJogo.TamanhoTela), RecorteParaCobrir(fundo.GetSize()));
        foreach (var residuo in _residuos)
            if (residuo.IdVista == _vista.Id && !residuo.Coletado)
                DesenharArte(residuo, residuo.CentroEm(_partida.SegundosDecorridos));
        foreach (var efeito in _efeitosColeta)
            if (efeito.Residuo.IdVista == _vista.Id)
                DesenharEfeitoColeta(efeito);

        if (_centroDica is not null && _idVistaDica == _vista.Id && _segundosPulsoDica > 0)
        {
            var pulso = 90f + (float)Math.Sin(_segundosPulsoDica * 4) * 10f;
            DrawArc(_centroDica.Value, pulso, 0, Mathf.Tau, 48, new Color("ffd34f"), 6f);
            DrawArc(_centroDica.Value, pulso + 8, 0, Mathf.Tau, 48, new Color("173c32"), 3f);
        }
        if (_segundosTransicao > 0)
            DrawRect(new Rect2(Vector2.Zero, RegrasJogo.TamanhoTela),
                new Color(1f, 1f, 1f, (float)_segundosTransicao * 0.5f));
        DrawSetTransform(Vector2.Zero);
    }

    private void TratarToque(Vector2 posicaoTela)
    {
        if (!EhAreaJogo(posicaoTela))
            return;
        var posicao = ParaPontoTela(posicaoTela);
        var residuo = _residuos.LastOrDefault(item =>
            item.IdVista == _vista!.Id && !item.Coletado &&
            item.CentroEm(_partida.SegundosDecorridos).DistanceTo(posicao) <= item.RaioToque);
        if (residuo is null || !_partida.Coletar(_residuos.IndexOf(residuo), residuo.Tipo))
            return;

        residuo.Coletado = true;
        _efeitosColeta.Add(new EfeitoColeta(residuo, residuo.CentroEm(_partida.SegundosDecorridos)));
        ResiduoColetado?.Invoke(residuo.Tipo, _vista!.Escala, RegrasJogo.PontosPorColeta, _partida.SegundosDecorridos);
        AtualizarHud();
        PulsarPontuacao();
        QueueRedraw();
        if (!_partida.EmAndamento)
            EncerrarPartida();
    }

    // Prefere um resíduo da vista atual; se não houver, orienta a ampliar ou reduzir.
    private void UsarDica()
    {
        if (!_partida.EmAndamento || _partida.DicasRestantes <= 0)
            return;

        var residuo = _residuos.Where(item => !item.Coletado)
            .OrderBy(item => item.IdVista == _vista!.Id ? 0 : 1)
            .ThenBy(item => item.Centro.DistanceTo(RegrasJogo.TamanhoTela / 2))
            .FirstOrDefault();
        if (residuo is null)
            return;

        _centroDica = residuo.Centro + s_deslocamentosDica[_partida.DicasUsadas % s_deslocamentosDica.Length];
        _idVistaDica = residuo.IdVista;
        _segundosPulsoDica = RegrasJogo.PulsoDicaSegundos;
        MostrarDirecaoDica(residuo.IdVista);

        _partida.UsarDica();
        DicaUsada?.Invoke(_partida.SegundosDecorridos);
        AtualizarHud();
        QueueRedraw();
        if (!_partida.EmAndamento)
            EncerrarPartida();
    }

    private void MostrarDirecaoDica(string idDestino)
    {
        if (idDestino == _vista!.Id)
        {
            MostrarAviso("OBSERVE A REGIÃO QUE PULSA: HÁ UM RESÍDUO POR PERTO.");
            return;
        }
        var destino = _cenario!.Vistas.First(vista => vista.Id == idDestino);
        MostrarAviso(destino.Escala > _vista.Escala
            ? "AMPLIE O CENÁRIO. OBSERVE A REGIÃO QUE PULSA."
            : "REDUZA A ESCALA.");
    }

    private void TrocarVista(string idVista)
    {
        if (!_partida.EmAndamento || _vista!.Id == idVista)
            return;
        _vista = _cenario!.Vistas.First(vista => vista.Id == idVista);
        _tweenAviso?.Kill();
        _painelAviso.Hide();
        if (_centroDica is not null && _idVistaDica == _vista.Id)
            _segundosPulsoDica = RegrasJogo.PulsoDicaSegundos;
        _segundosTransicao = RegrasJogo.TransicaoSegundos;
        AtualizarHud();
        QueueRedraw();
    }

    // Devolve false na última escala, para a pinça poder tentar de novo no mesmo gesto.
    private bool TentarAmpliar()
    {
        if (_vista!.IdProxima is null)
            return false;
        TrocarVista(_vista.IdProxima);
        return true;
    }

    private void Reduzir()
    {
        if (_vista?.IdPai is not null)
            TrocarVista(_vista.IdPai);
    }

    private Vector2 ParaPontoTela(Vector2 posicaoTela) =>
        (GetGlobalTransformWithCanvas().AffineInverse() * posicaoTela) / Size * RegrasJogo.TamanhoTela;

    private bool EhAreaJogo(Vector2 posicaoTela)
    {
        if (Size.X <= 0 || Size.Y <= 0)
            return false;
        if (_painelAviso.Visible && _painelAviso.GetGlobalRect().HasPoint(posicaoTela))
            return false;
        var ponto = ParaPontoTela(posicaoTela);
        return ponto.X >= 0 && ponto.X <= RegrasJogo.TamanhoTela.X &&
            ponto.Y >= RegrasJogo.AreaJogoTopo && ponto.Y <= RegrasJogo.AreaJogoBase;
    }

    private void LimparMapas()
    {
        foreach (var mapa in _mapas.Values)
        {
            RemoveChild(mapa);
            mapa.QueueFree();
        }
        _mapas.Clear();
    }

    private void EncerrarPartida()
    {
        SetProcess(false);
        SetProcessInput(false);
        _gesto.Limpar();
        PartidaEncerrada?.Invoke(_partida.MotivoFim!.Value, _partida.SegundosDecorridos);
        _painelResultado.MostrarResumo(_partida, _cenario!.MensagemFinal, _conteudo.Mensagens);
    }

    private void AtualizarHud()
    {
        _rotuloTempo.Text = TextoTempo();
        _rotuloPontuacao.Text = $"PONTOS: {_partida.Pontuacao}";
        _rotuloEscala.Text = $"ESCALA {_vista!.Escala}/{_cenario!.Vistas.Max(vista => vista.Escala)} · {_vista.Nome}".ToUpperInvariant();
        _rotuloResiduos.Text = $"{_partida.ResiduosColetados}/{_partida.TotalResiduos} RESÍDUOS ENCONTRADOS";
        _botaoDica.Text = _partida.DicasRestantes > 0
            ? $"DICA ({_partida.DicasRestantes}) · −{RegrasJogo.PenalidadeDicaSegundos} SEG"
            : "SEM DICAS";
        _botaoDica.Disabled = !_partida.EmAndamento || _partida.DicasRestantes <= 0;
    }

    private void DesenharArte(Residuo residuo, Vector2 centro, float ampliacao = 1, float opacidade = 1)
    {
        var origem = residuo.Arte.RetanguloVisivel;
        var tamanho = origem.Size * (residuo.TamanhoExibido / Math.Max(origem.Size.X, origem.Size.Y)) * ampliacao;
        var escalaTela = Size / RegrasJogo.TamanhoTela;
        DrawSetTransform(centro * escalaTela, 0, new Vector2(residuo.Espelhado ? -1 : 1, 1) * escalaTela);
        DrawTextureRectRegion(residuo.Arte.Textura, new Rect2(-tamanho / 2, tamanho), origem, new Color(1, 1, 1, opacidade));
        DrawSetTransform(Vector2.Zero, 0, escalaTela);
    }

    // Não depende só de cor: o objeto cresce e some, um anel se abre e "+10" sobe.
    private void DesenharEfeitoColeta(EfeitoColeta efeito)
    {
        var progresso = (float)(1 - efeito.SegundosRestantes / RegrasJogo.EfeitoColetaSegundos);
        var opacidade = 1 - progresso;
        DesenharArte(efeito.Residuo, efeito.Centro, 1 + 0.5f * progresso, opacidade);

        var raio = Math.Max(efeito.Residuo.RaioToque, 30) + 30 * progresso;
        DrawArc(efeito.Centro, raio, 0, Mathf.Tau, 40, new Color(0.09f, 0.24f, 0.19f, opacidade), 7f);
        DrawArc(efeito.Centro, raio, 0, Mathf.Tau, 40, new Color(1, 1, 1, opacidade), 4f);

        const int tamanhoFonte = 34;
        const float largura = 120;
        var fonte = GetThemeDefaultFont();
        var texto = $"+{RegrasJogo.PontosPorColeta}";
        var posicao = efeito.Centro + new Vector2(-largura / 2, -raio - 8 - 40 * progresso);
        DrawStringOutline(fonte, posicao, texto, HorizontalAlignment.Center, largura, tamanhoFonte, 8,
            new Color(0, 0, 0, opacidade));
        DrawString(fonte, posicao, texto, HorizontalAlignment.Center, largura, tamanhoFonte,
            new Color(1f, 0.83f, 0.31f, opacidade));
    }

    private void MostrarAviso(string texto)
    {
        _tweenAviso?.Kill();
        _rotuloAviso.Text = texto;
        _painelAviso.Visible = texto != "";
        _painelAviso.Modulate = Colors.White;
        if (texto == "")
            return;
        _tweenAviso = CreateTween();
        _tweenAviso.TweenInterval(RegrasJogo.AvisoSegundos);
        _tweenAviso.TweenProperty(_painelAviso, "modulate:a", 0f, 0.4)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        _tweenAviso.TweenCallback(Callable.From(_painelAviso.Hide));
    }

    private void PulsarPontuacao()
    {
        _tweenPontuacao?.Kill();
        _rotuloPontuacao.PivotOffset = _rotuloPontuacao.Size / 2;
        _rotuloPontuacao.Scale = Vector2.One * 1.3f;
        _tweenPontuacao = CreateTween();
        _tweenPontuacao.TweenProperty(_rotuloPontuacao, "scale", Vector2.One, 0.35)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
    }

    private string TextoTempo() => $"TEMPO: {Math.Ceiling(_partida.SegundosRestantes)} SEG";

    private static Texture2D CarregarTextura(string caminho) =>
        GD.Load<Texture2D>(caminho) ?? throw new InvalidOperationException($"Não foi possível carregar: {caminho}");

    // Recorta a foto no centro para cobrir 16:9 sem distorcer.
    private static Rect2 RecorteParaCobrir(Vector2 tamanhoImagem)
    {
        var proporcaoTela = RegrasJogo.TamanhoTela.X / RegrasJogo.TamanhoTela.Y;
        if (tamanhoImagem.X / tamanhoImagem.Y > proporcaoTela)
        {
            var largura = tamanhoImagem.Y * proporcaoTela;
            return new Rect2((tamanhoImagem.X - largura) / 2, 0, largura, tamanhoImagem.Y);
        }
        var altura = tamanhoImagem.X / proporcaoTela;
        return new Rect2(0, (tamanhoImagem.Y - altura) / 2, tamanhoImagem.X, altura);
    }
}
