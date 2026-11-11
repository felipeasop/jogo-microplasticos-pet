using Godot;

namespace CacaAosResiduos.Jogo;

public partial class PainelResultado : PanelContainer
{
    private Label _titulo = null!;
    private Label _resumo = null!;
    private Label _textoEducativo = null!;
    private Label _instrucao = null!;
    private GridContainer _tipos = null!;
    private ScrollContainer _rolagem = null!;

    public override void _Ready()
    {
        _titulo = GetNode<Label>("%ResultTitle");
        _resumo = GetNode<Label>("%ResultSummary");
        _textoEducativo = GetNode<Label>("%EducationText");
        _instrucao = GetNode<Label>("Margin/Content/Instruction");
        _tipos = GetNode<GridContainer>("%ResultTypes");
        _rolagem = GetNode<ScrollContainer>("Margin");
    }

    // Cada tipo coletado vira um botão que mostra a frase educativa dele.
    public void MostrarResumo(Partida partida, string mensagemFinal, IReadOnlyDictionary<string, string> mensagens)
    {
        Limpar();
        _titulo.Text = partida.MotivoFim == SituacaoPartida.Completou
            ? "VOCÊ ENCONTROU TODOS OS RESÍDUOS!" : "O TEMPO TERMINOU";
        _resumo.Text = $"{partida.Pontuacao} PONTOS · {partida.ResiduosColetados} DE {partida.TotalResiduos} RESÍDUOS · {partida.DicasUsadas} DICAS";
        _textoEducativo.Text = mensagemFinal.ToUpperInvariant();
        _instrucao.Visible = partida.ColetadosPorTipo.Count > 0;
        foreach (var (tipo, quantidade) in partida.ColetadosPorTipo.OrderBy(par => par.Key))
        {
            var botao = new Button
            {
                Text = $"{tipo}: {quantidade}".ToUpperInvariant(),
                CustomMinimumSize = new Vector2(330, 52)
            };
            botao.Pressed += () => _textoEducativo.Text = mensagens[tipo].ToUpperInvariant();
            _tipos.AddChild(botao);
        }
        Show();
    }

    public void Limpar()
    {
        Hide();
        _rolagem.ScrollVertical = 0;
        _titulo.Text = "";
        _resumo.Text = "";
        _textoEducativo.Text = "";
        foreach (var filho in _tipos.GetChildren())
        {
            _tipos.RemoveChild(filho);
            filho.QueueFree();
        }
    }
}
