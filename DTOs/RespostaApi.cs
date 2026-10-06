using System.Text.Json.Serialization;

namespace StrockWay.DTOs;

/// <summary>
/// Envelope padrão de todas as respostas:
///   sucesso: { "sucesso": true,  "dados": { ... } }
///   erro:    { "sucesso": false, "erro": "mensagem" }
/// </summary>
public class RespostaApi<T>
{
    public bool Sucesso { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Dados { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Erro { get; init; }
}

public static class RespostaApi
{
    public static RespostaApi<T> Ok<T>(T dados) => new() { Sucesso = true, Dados = dados };

    public static RespostaApi<object> Falha(string erro) => new() { Sucesso = false, Erro = erro };
}
