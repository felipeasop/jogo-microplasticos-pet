using System.Text.Json;
using System.Text.Json.Serialization;

namespace CacaAosResiduos.Dados;

public class CatalogoEscolas
{
    private const string CaminhoCatalogo = "res://content/schools.json";
    private static readonly JsonSerializerOptions s_opcoesLeitura = new() { PropertyNameCaseInsensitive = true };

    [JsonPropertyName("cities")]
    public List<CidadeCatalogo> Cidades { get; set; } = [];

    public static CatalogoEscolas Carregar()
    {
        using var arquivo = Godot.FileAccess.Open(CaminhoCatalogo, Godot.FileAccess.ModeFlags.Read)
            ?? throw new InvalidOperationException($"Não foi possível abrir {CaminhoCatalogo}.");
        return JsonSerializer.Deserialize<CatalogoEscolas>(arquivo.GetAsText(), s_opcoesLeitura)
            ?? throw new InvalidOperationException("Catálogo de escolas inválido.");
    }
}
