namespace CacaAosResiduos.Persistencia;

// Impede duas cópias do jogo abertas no mesmo banco.
public static class TravaBanco
{
    // O sistema libera o arquivo quando o processo termina, mesmo em queda de energia.
    // Sem permissão para criar a trava, segue sem ela.
    public static FileStream? Tentar(string caminhoBanco, out bool travou)
    {
        travou = true;
        try
        {
            return new FileStream(caminhoBanco + ".trava", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (Exception erro) when (erro is UnauthorizedAccessException or DirectoryNotFoundException)
        {
            return null;
        }
        catch (IOException)
        {
            travou = false;
            return null;
        }
    }
}
