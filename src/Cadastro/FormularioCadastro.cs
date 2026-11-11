using CacaAosResiduos.Dados;
using Godot;

namespace CacaAosResiduos.Cadastro;

public partial class FormularioCadastro : VBoxContainer
{
    private const string TextoVazio = "TOQUE PARA ESCOLHER";
    private const int AlturaLinha = 56;
    private const int SeparacaoLinhas = 4;
    private const int LinhasVisiveis = 5;
    // O primeiro toque no + já começa na idade do público, para poupar toques.
    private const int IdadeInicialSeletor = 8;

    private static readonly (string Etapa, string[] Anos)[] s_etapas =
    [
        ("ENSINO FUNDAMENTAL", [.. Enumerable.Range(1, 9).Select(ano => $"{ano}º ANO")]),
        ("ENSINO MÉDIO", ["1º ANO", "2º ANO", "3º ANO"]),
        ("ENSINO SUPERIOR", [.. Enumerable.Range(1, 10).Select(periodo => $"{periodo}º PERÍODO")]),
        ("EJA", ["ENSINO FUNDAMENTAL", "ENSINO MÉDIO"]),
        ("EDUCAÇÃO INFANTIL", ["PRÉ-ESCOLA"]),
        ("DOCENTE", ["PROFESSOR(A)"]),
        ("OUTRO", ["OUTRO"])
    ];

    private record Opcao(string Texto, Action Escolher);

    // Botão que abre a lista e guarda o valor escolhido.
    private class Campo(Button botao)
    {
        public Button Botao => botao;
        public string Valor { get; private set; } = "";

        public void Definir(string valor)
        {
            Valor = valor;
            Botao.Text = valor.Length > 0 ? valor : TextoVazio;
        }
    }

    private BuscaEscolas _busca = null!;
    private Campo _cidade = null!;
    private Campo _escola = null!;
    private Campo _etapa = null!;
    private Campo _ano = null!;
    private PanelContainer _lista = null!;
    private ScrollContainer _rolagem = null!;
    private VBoxContainer _linhas = null!;
    private Label _rotuloIdade = null!;
    private Button _menosIdade = null!;
    private Button _maisIdade = null!;
    private Button _jaJogou = null!;
    private int _idade;
    private int _toqueRolagem = -1;
    private float _inicioToqueRolagem;
    private int _inicioRolagem;
    private bool _arrastando;

    public override void _Ready()
    {
        _busca = new BuscaEscolas(CatalogoEscolas.Carregar());
        _cidade = new Campo(GetNode<Button>("CityInput"));
        _escola = new Campo(GetNode<Button>("SchoolInput"));
        _etapa = new Campo(GetNode<Button>("EducationLevelInput"));
        _ano = new Campo(GetNode<Button>("SchoolYearInput"));
        _lista = GetNode<PanelContainer>("%SelectionList");
        _rolagem = _lista.GetNode<ScrollContainer>("Scroll");
        _linhas = _rolagem.GetNode<VBoxContainer>("Options");
        _linhas.AddThemeConstantOverride("separation", SeparacaoLinhas);
        _rolagem.GetVScrollBar().CustomMinimumSize = new Vector2(28, 0);
        _rotuloIdade = GetNode<Label>("AgeAndPlayedRow/AgeColumn/AgeInput/AgeValue");
        _menosIdade = GetNode<Button>("AgeAndPlayedRow/AgeColumn/AgeInput/AgeMinusButton");
        _maisIdade = GetNode<Button>("AgeAndPlayedRow/AgeColumn/AgeInput/AgePlusButton");
        _jaJogou = GetNode<Button>("AgeAndPlayedRow/PlayedBeforeColumn/PlayedBeforeInput");

        _cidade.Botao.Pressed += () => AbrirLista(_cidade, OpcoesCidade());
        _escola.Botao.Pressed += () => AbrirLista(_escola, OpcoesEscola());
        _etapa.Botao.Pressed += () => AbrirLista(_etapa, OpcoesEtapa());
        _ano.Botao.Pressed += () => AbrirLista(_ano, OpcoesAno());
        _menosIdade.Pressed += () => AjustarIdade(-1);
        _maisIdade.Pressed += () => AjustarIdade(1);
        _jaJogou.Toggled += pressionado => _jaJogou.Text = pressionado ? "SIM" : "NÃO";

        Limpar();
    }

    public void Limpar()
    {
        FecharLista();
        _cidade.Definir("");
        _escola.Definir("");
        DefinirEtapa("");
        DefinirIdade(0);
        _jaJogou.ButtonPressed = false;
        _jaJogou.Text = "NÃO";
    }

    public bool TentarLer(out DadosCadastro? cadastro, out string erro)
    {
        var lido = new DadosCadastro(_cidade.Valor, _escola.Valor, _etapa.Valor, _ano.Valor, _idade, _jaJogou.ButtonPressed);
        erro = ValidacaoCadastro.Erro(lido);
        cadastro = erro.Length == 0 ? lido : null;
        return cadastro is not null;
    }

    public override void _Input(InputEvent entrada)
    {
        if (!_lista.IsVisibleInTree())
            return;
        // O projeto desliga o mouse emulado, então a rolagem da lista por toque é feita aqui.
        if (entrada is InputEventScreenTouch toque)
        {
            if (toque.Pressed && _toqueRolagem == -1 && _rolagem.GetGlobalRect().HasPoint(toque.Position))
            {
                _toqueRolagem = toque.Index;
                _inicioToqueRolagem = toque.Position.Y;
                _inicioRolagem = _rolagem.ScrollVertical;
                _arrastando = false;
            }
            else if (!toque.Pressed && toque.Index == _toqueRolagem)
            {
                if (_arrastando || toque.Canceled)
                {
                    _linhas.PropagateNotification((int)NotificationScrollBegin);
                    _linhas.PropagateNotification((int)NotificationScrollEnd);
                    GetViewport().SetInputAsHandled();
                }
                _toqueRolagem = -1;
                _arrastando = false;
            }
        }
        else if (entrada is InputEventScreenDrag arraste && arraste.Index == _toqueRolagem)
        {
            var distancia = _inicioToqueRolagem - arraste.Position.Y;
            if (_arrastando || Math.Abs(distancia) > _rolagem.ScrollDeadzone)
            {
                if (!_arrastando)
                    _linhas.PropagateNotification((int)NotificationScrollBegin);
                _arrastando = true;
                _rolagem.ScrollVertical = _inicioRolagem + (int)distancia;
                GetViewport().SetInputAsHandled();
            }
        }

        if (entrada is InputEventScreenTouch { Pressed: true } toqueFora &&
            !_lista.GetGlobalRect().HasPoint(toqueFora.Position))
            Callable.From(FecharLista).CallDeferred();
    }

    private IEnumerable<Opcao> OpcoesCidade() =>
        _busca.Cidades().Select(cidade => new Opcao(cidade, () =>
        {
            _cidade.Definir(cidade);
            _escola.Definir("");
        }));

    // Sem cidade escolhida, a escola mostra a cidade junto e preenche as duas.
    private IEnumerable<Opcao> OpcoesEscola() =>
        _busca.Escolas(_cidade.Valor.Length > 0 ? _cidade.Valor : null).Select(item => new Opcao(
            _cidade.Valor.Length > 0 ? item.Escola : $"{item.Escola} · {item.Cidade}",
            () =>
            {
                _escola.Definir(item.Escola.ToUpperInvariant());
                _cidade.Definir(item.Cidade);
            }));

    private IEnumerable<Opcao> OpcoesEtapa() =>
        s_etapas.Select(etapa => new Opcao(etapa.Etapa, () => DefinirEtapa(etapa.Etapa)));

    private IEnumerable<Opcao> OpcoesAno() =>
        s_etapas.First(etapa => etapa.Etapa == _etapa.Valor).Anos.Select(ano => new Opcao(ano, () => _ano.Definir(ano)));

    private void DefinirEtapa(string etapa)
    {
        _etapa.Definir(etapa);
        _ano.Definir("");
        _ano.Botao.Disabled = etapa.Length == 0;
        // Etapa com uma só opção já vem preenchida.
        var anos = s_etapas.FirstOrDefault(item => item.Etapa == etapa).Anos;
        if (anos is { Length: 1 })
            _ano.Definir(anos[0]);
    }

    private void AbrirLista(Campo campo, IEnumerable<Opcao> opcoes)
    {
        FecharLista();
        foreach (var filho in _linhas.GetChildren())
        {
            _linhas.RemoveChild(filho);
            filho.QueueFree();
        }
        foreach (var opcao in opcoes)
        {
            var linha = new Button
            {
                Text = opcao.Texto.ToUpperInvariant(), ThemeTypeVariation = "SuggestionButton",
                CustomMinimumSize = new Vector2(0, AlturaLinha), SizeFlagsHorizontal = SizeFlags.ExpandFill,
                Alignment = HorizontalAlignment.Left, ClipText = true,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis, MouseFilter = MouseFilterEnum.Pass
            };
            linha.Pressed += () =>
            {
                opcao.Escolher();
                FecharLista();
            };
            _linhas.AddChild(linha);
        }

        var quantidade = Math.Min(_linhas.GetChildCount(), LinhasVisiveis);
        var margens = _lista.GetThemeStylebox("panel").GetMinimumSize().Y;
        var tela = _lista.GetParent<Control>();
        var botao = campo.Botao;
        var altura = quantidade * AlturaLinha + (quantidade - 1) * SeparacaoLinhas + margens;
        var abaixo = botao.GlobalPosition.Y - tela.GlobalPosition.Y + botao.Size.Y + 4;
        var topo = abaixo + altura <= tela.Size.Y ? abaixo : botao.GlobalPosition.Y - tela.GlobalPosition.Y - altura - 4;
        _lista.Position = new Vector2(botao.GlobalPosition.X - tela.GlobalPosition.X, topo);
        _lista.Size = new Vector2(botao.Size.X, altura);
        _rolagem.ScrollVertical = 0;
        _lista.Show();
    }

    private void FecharLista()
    {
        _lista.Hide();
        _toqueRolagem = -1;
        _arrastando = false;
    }

    // Zero significa idade ainda não informada.
    private void DefinirIdade(int idade)
    {
        _idade = idade == 0 ? 0 : Math.Clamp(idade, ValidacaoCadastro.IdadeMinima, ValidacaoCadastro.IdadeMaxima);
        _rotuloIdade.Text = _idade == 0 ? "TOQUE NO +" : $"{_idade} ANOS";
        _menosIdade.Disabled = _idade <= ValidacaoCadastro.IdadeMinima;
        _maisIdade.Disabled = _idade >= ValidacaoCadastro.IdadeMaxima;
    }

    internal void AjustarIdade(int variacao) =>
        DefinirIdade(_idade == 0 && variacao > 0 ? IdadeInicialSeletor : _idade + variacao);
}
