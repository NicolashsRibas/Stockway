using System.Security.Cryptography;

namespace StockWay.Common;

/// <summary>
/// Gera e confere hashes de senha com PBKDF2-HMAC-SHA512 (recomendação OWASP: 210.000 iterações).
/// Cada senha recebe um "salt" aleatório, então duas senhas iguais geram hashes diferentes.
///
/// Formato guardado no banco: "pbkdf2-sha512$210000$&lt;salt base64&gt;$&lt;hash base64&gt;".
/// As iterações ficam no próprio texto: se um dia o número aumentar, os hashes antigos continuam válidos.
/// </summary>
public static class HashDeSenha
{
    private const string Algoritmo = "pbkdf2-sha512";
    private const int Iteracoes = 210_000;
    private const int TamanhoSalt = 16;
    private const int TamanhoHash = 64;

    public static string Gerar(string senha)
    {
        var salt = RandomNumberGenerator.GetBytes(TamanhoSalt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(senha, salt, Iteracoes, HashAlgorithmName.SHA512, TamanhoHash);
        return $"{Algoritmo}${Iteracoes}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verificar(string senha, string hashGuardado)
    {
        var partes = hashGuardado.Split('$');
        if (partes.Length != 4 || partes[0] != Algoritmo ||
            !int.TryParse(partes[1], out var iteracoes) || iteracoes <= 0)
            return false;

        byte[] salt, esperado;
        try
        {
            salt = Convert.FromBase64String(partes[2]);
            esperado = Convert.FromBase64String(partes[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var calculado = Rfc2898DeriveBytes.Pbkdf2(senha, salt, iteracoes, HashAlgorithmName.SHA512, esperado.Length);
        return CryptographicOperations.FixedTimeEquals(calculado, esperado);
    }
}
