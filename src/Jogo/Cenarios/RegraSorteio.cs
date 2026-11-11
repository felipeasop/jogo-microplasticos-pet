using System.Text.Json.Serialization;

namespace CacaAosResiduos.Jogo;

public class RegraSorteio
{
    [JsonPropertyName("zone")]
    public string Zona { get; set; } = "";
    [JsonPropertyName("items")]
    public List<string> Itens { get; set; } = [];
    [JsonPropertyName("count")]
    public int Quantidade { get; set; }
}
