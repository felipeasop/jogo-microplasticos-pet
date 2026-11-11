using System.Text.Json.Serialization;

namespace CacaAosResiduos.Dados;

public class CidadeCatalogo
{
    [JsonPropertyName("name")]
    public string Nome { get; set; } = "";

    [JsonPropertyName("schools")]
    public List<string> Escolas { get; set; } = [];
}
