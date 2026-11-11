using System.Text.Json.Serialization;

namespace CacaAosResiduos.Jogo;

public class Vista
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
    [JsonPropertyName("name")]
    public string Nome { get; set; } = "";
    [JsonPropertyName("scale")]
    public int Escala { get; set; } = 1;
    [JsonPropertyName("parentId")]
    public string? IdPai { get; set; }
    [JsonPropertyName("nextId")]
    public string? IdProxima { get; set; }
    [JsonPropertyName("mapScene")]
    public string? CenaMapa { get; set; }
    [JsonPropertyName("background")]
    public string Fundo { get; set; } = "";
    [JsonPropertyName("spawns")]
    public List<RegraSorteio> RegrasSorteio { get; set; } = [];
}
