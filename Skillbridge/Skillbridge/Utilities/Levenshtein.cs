namespace Skillbridge.Utilities;

public class Levenshtein
{
    private readonly static int _LIMIT = 3;
    public static bool Contem(string texto, string pesquisa)
    {
        if (texto.Contains(pesquisa, StringComparison.OrdinalIgnoreCase))
            return true;

        var palavras = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return palavras.Any(p => Verify(p.ToLower(), pesquisa.ToLower()) <= _LIMIT);
    }

    private static int Verify(string a, string b)
    {
        int[,] d = new int[a.Length + 1, b.Length + 1];

        for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) d[0, j] = j;

        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                int custo = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + custo
                );
            }
        }

        return d[a.Length, b.Length];
    }
}