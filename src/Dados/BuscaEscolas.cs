using System.Globalization;
using System.Text;

namespace CacaAosResiduos.Dados;

public record SugestaoEscola(string Escola, string Cidade);

public class BuscaEscolas(CatalogoEscolas catalogo)
{
    public const string OutraCidade = "OUTRA CIDADE";
    public const string OutraEscola = "OUTRA ESCOLA";
    // Para visitantes sem escola.
    public const string ComunidadeExterna = "COMUNIDADE EXTERNA";

    public IReadOnlyList<string> Cidades() =>
        [.. catalogo.Cidades.Select(cidade => cidade.Nome)
            .OrderBy(ChaveOrdem, StringComparer.Ordinal), OutraCidade];

    public IReadOnlyList<SugestaoEscola> Escolas(string? nomeCidade = null)
    {
        var cidades = Cidades().Where(cidade => nomeCidade is null ||
            string.Equals(cidade, nomeCidade, StringComparison.OrdinalIgnoreCase));
        return [.. cidades.SelectMany(cidade => EscolasDe(cidade)
            .OrderBy(ChaveOrdem, StringComparer.Ordinal)
            .Append(ComunidadeExterna)
            .Append(OutraEscola)
            .Select(escola => new SugestaoEscola(escola, cidade)))];
    }

    private IEnumerable<string> EscolasDe(string nomeCidade) =>
        catalogo.Cidades.Where(cidade => cidade.Nome == nomeCidade)
            .SelectMany(cidade => cidade.Escolas)
            .Select(ExpandirNomeEscola);

    private static string ExpandirNomeEscola(string valor)
    {
        var expandido = TrocarSufixo(valor.Trim(), "EFM", "ENSINO FUNDAMENTAL E MÉDIO");
        return TrocarSufixo(expandido, "EJA", "EDUCAÇÃO DE JOVENS E ADULTOS");
    }

    private static string TrocarSufixo(string valor, string sufixo, string substituto) =>
        valor.EndsWith(" " + sufixo, StringComparison.OrdinalIgnoreCase)
            ? valor[..^(sufixo.Length + 1)] + " — " + substituto
            : valor;

    private static string ChaveOrdem(string valor)
    {
        var decomposto = valor.ToUpperInvariant().Normalize(NormalizationForm.FormD);
        var construtor = new StringBuilder(decomposto.Length);
        foreach (var caractere in decomposto)
            if (CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark)
                construtor.Append(caractere);
        return construtor.ToString().Trim();
    }
}
