using System.Text.Json.Serialization;

namespace CacaAosResiduos.Jogo;

public class Cenario
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
    [JsonPropertyName("name")]
    public string Nome { get; set; } = "";
    [JsonPropertyName("place")]
    public string Local { get; set; } = "";
    [JsonPropertyName("selectable")]
    public bool Selecionavel { get; set; }
    [JsonPropertyName("spawnGap")]
    public float EspacamentoSorteio { get; set; } = 18;
    // Amplia todos os resíduos do cenário; deixa a fase mais fácil.
    [JsonPropertyName("sizeMultiplier")]
    public float MultiplicadorTamanho { get; set; } = 1;
    [JsonPropertyName("closingMessage")]
    public string MensagemFinal { get; set; } = "";
    [JsonPropertyName("views")]
    public List<Vista> Vistas { get; set; } = [];
}
