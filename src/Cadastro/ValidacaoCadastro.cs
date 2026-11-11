namespace CacaAosResiduos.Cadastro;

public static class ValidacaoCadastro
{
    public const int IdadeMinima = 1;
    public const int IdadeMaxima = 100;

    public static string Erro(DadosCadastro cadastro)
    {
        if (string.IsNullOrWhiteSpace(cadastro.Cidade) || string.IsNullOrWhiteSpace(cadastro.Escola) ||
            string.IsNullOrWhiteSpace(cadastro.EtapaEnsino) || string.IsNullOrWhiteSpace(cadastro.AnoSerie))
            return "PREENCHA TODOS OS CAMPOS PARA CONTINUAR.";
        if (cadastro.Idade is < IdadeMinima or > IdadeMaxima)
            return $"INFORME UMA IDADE ENTRE {IdadeMinima} E {IdadeMaxima} ANOS.";
        return "";
    }
}
