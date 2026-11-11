using System.Text.Json;
using System.Text.Json.Serialization;

namespace CacaAosResiduos.Jogo;

public class ConteudoJogo
{
    private const string CaminhoConteudo = "res://content/game.json";
    private static readonly JsonSerializerOptions s_opcoesLeitura = new() { PropertyNameCaseInsensitive = true };

    [JsonPropertyName("version")]
    public string Versao { get; set; } = "";
    [JsonPropertyName("collectibles")]
    public Dictionary<string, TipoColetavel> Coletaveis { get; set; } = [];
    [JsonPropertyName("messages")]
    public Dictionary<string, string> Mensagens { get; set; } = [];
    [JsonPropertyName("scenarios")]
    public List<Cenario> Cenarios { get; set; } = [];

    public static ConteudoJogo Carregar()
    {
        using var arquivo = Godot.FileAccess.Open(CaminhoConteudo, Godot.FileAccess.ModeFlags.Read)
            ?? throw new InvalidOperationException("Conteúdo do jogo não encontrado.");
        var conteudo = JsonSerializer.Deserialize<ConteudoJogo>(arquivo.GetAsText(), s_opcoesLeitura)
            ?? throw new InvalidOperationException("Conteúdo do jogo vazio.");

        conteudo.Validar();
        return conteudo;
    }

    // Recusa o conteúdo inteiro se algum vínculo estiver quebrado.
    public void Validar()
    {
        if (string.IsNullOrWhiteSpace(Versao) || Cenarios is null || Cenarios.Count == 0 ||
            Coletaveis is null || Mensagens is null)
            throw new InvalidOperationException("Conteúdo sem versão, cenários ou resíduos.");
        if (Cenarios.Any(cenario => cenario is null || string.IsNullOrWhiteSpace(cenario.Id)) ||
            Cenarios.Select(cenario => cenario.Id).Distinct().Count() != Cenarios.Count)
            throw new InvalidOperationException("Identificadores de cenário vazios ou repetidos.");
        if (!Cenarios.Any(cenario => cenario.Selecionavel) ||
            Cenarios.Any(cenario => cenario.Selecionavel && (string.IsNullOrWhiteSpace(cenario.Nome) || string.IsNullOrWhiteSpace(cenario.Local))))
            throw new InvalidOperationException("Nenhum cenário selecionável, ou cenário selecionável sem nome e local.");
        foreach (var (id, item) in Coletaveis)
        {
            if (item is null || string.IsNullOrWhiteSpace(item.Textura) || string.IsNullOrWhiteSpace(item.Tipo) ||
                item.Escala is < 1 or > RegrasJogo.QuantidadeEscalas ||
                !item.Textura.Contains($"/escala-{item.Escala}/", StringComparison.Ordinal) ||
                !float.IsFinite(item.Tamanho) || item.Tamanho <= 0 ||
                !Mensagens.TryGetValue(item.Tipo, out var mensagem) || string.IsNullOrWhiteSpace(mensagem))
                throw new InvalidOperationException($"Resíduo {id} sem imagem, escala, tamanho ou mensagem válidos.");
        }
        foreach (var cenario in Cenarios)
        {
            if (cenario.Vistas is null || cenario.Vistas.Count == 0 ||
                cenario.Vistas.Any(vista => vista is null || string.IsNullOrWhiteSpace(vista.Id)) ||
                cenario.Vistas.Select(vista => vista.Id).Distinct().Count() != cenario.Vistas.Count ||
                !float.IsFinite(cenario.EspacamentoSorteio) || cenario.EspacamentoSorteio < 0 ||
                !float.IsFinite(cenario.MultiplicadorTamanho) || cenario.MultiplicadorTamanho <= 0)
                throw new InvalidOperationException($"Vistas ou espaçamento inválidos em {cenario.Id}.");
            var vistas = cenario.Vistas.ToDictionary(vista => vista.Id);
            if (cenario.Vistas[0].IdPai is not null || cenario.Vistas[0].Escala != 1)
                throw new InvalidOperationException($"Visão geral inválida em {cenario.Id}.");
            foreach (var vista in cenario.Vistas)
            {
                if (vista.Escala is < 1 or > RegrasJogo.QuantidadeEscalas || string.IsNullOrWhiteSpace(vista.Fundo) ||
                    vista.RegrasSorteio is null)
                    throw new InvalidOperationException($"Escala ou imagem inválidas em {vista.Id}.");
                if (vista.IdPai is not null && (!vistas.TryGetValue(vista.IdPai, out var pai) || pai.Escala != vista.Escala - 1))
                    throw new InvalidOperationException($"Vista pai inválida em {vista.Id}.");
                if (vista.Escala > 1 && vista.IdPai is null)
                    throw new InvalidOperationException($"Vista sem caminho de volta em {vista.Id}.");
                if (vista.IdProxima is not null && (!vistas.TryGetValue(vista.IdProxima, out var proxima) || proxima.IdPai != vista.Id || proxima.Escala != vista.Escala + 1))
                    throw new InvalidOperationException($"Destino de zoom inválido em {vista.Id}.");
                foreach (var regra in vista.RegrasSorteio)
                    if (regra is null || string.IsNullOrWhiteSpace(vista.CenaMapa) || string.IsNullOrWhiteSpace(regra.Zona) ||
                        regra.Quantidade <= 0 || regra.Itens is null || regra.Itens.Count == 0 ||
                        regra.Itens.Any(idItem => !Coletaveis.TryGetValue(idItem, out var item) || item.Escala != vista.Escala))
                        throw new InvalidOperationException($"Regra de geração ou escala inválida em {vista.Id}.");
            }
        }
    }
}
