using System.Text.Json.Serialization;

namespace CacaAosResiduos.Jogo;

public class TipoColetavel
{
    [JsonPropertyName("type")]
    public string Tipo { get; set; } = "";
    [JsonPropertyName("texture")]
    public string Textura { get; set; } = "";
    [JsonPropertyName("scale")]
    public int Escala { get; set; }
    // Maior lado em px na referência da escala; a zona do mapa ajusta pela perspectiva da foto.
    [JsonPropertyName("size")]
    public float Tamanho { get; set; }
}
