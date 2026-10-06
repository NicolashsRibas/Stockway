namespace StrockWay.Models;

/// <summary>Situação do saldo em relação aos indicadores de mínimo e máximo.</summary>
public enum StatusEstoque
{
    Zerado,        // quantidade == 0
    Baixo,         // 0 < quantidade <= estoque mínimo
    Normal,        // mínimo < quantidade <= máximo
    AcimaMaximo    // quantidade > estoque máximo
}

/// <summary>Situação da data de validade.</summary>
public enum StatusValidade
{
    NaoPerecivel,  // sem data de validade
    Ok,            // vence em mais de 30 dias
    VenceEmBreve,  // vence em até 30 dias
    Vencido        // a data já passou
}

/// <summary>Tipos de registro no histórico (log) de movimentações.</summary>
public enum TipoMovimentacao
{
    Cadastro,
    Entrada,
    Saida,
    Ajuste,
    Edicao,
    Remocao
}
