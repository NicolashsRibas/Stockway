using Microsoft.EntityFrameworkCore;
using StrockWay.Data;
using StrockWay.DTOs;

namespace StrockWay.Services;

/// <summary>Cadastra produtos de exemplo (dotnet run -- --exemplo) quando o banco está vazio.</summary>
public static class DadosExemplo
{
    public static async Task CarregarAsync(StrockWayContext db, EstoqueService servico, ILogger logger)
    {
        if (await db.Produtos.AnyAsync())
        {
            logger.LogInformation("--exemplo ignorado: já existem produtos cadastrados.");
            return;
        }

        const string sistema = "Sistema (dados de exemplo)";
        var hoje = Relogio.Hoje;

        var exemplos = new[]
        {
            Novo("PAR-001", "Parafuso sextavado M8 x 40mm", 450, 0.35m, 0.025m, null, "A", "01", 200, 2000),
            Novo("LUV-010", "Luva nitrílica de proteção (par)", 18, 4.90m, 0.030m, hoje.AddDays(400), "B", "03", 30, 300),
            Novo("FIT-002", "Fita isolante 19mm x 20m", 0, 6.50m, 0.060m, null, "B", "01", 20, 200),
            Novo("CAB-025", "Cabo flexível 2,5mm² (metro)", 820, 3.20m, 0.030m, null, "C", "02", 300, 1500),
            Novo("DIS-020", "Disjuntor bipolar 20A", 12, 38.90m, 0.250m, null, "C", "05", 10, 80),
            Novo("ALC-070", "Álcool 70% 1 litro", 45, 9.80m, 0.900m, hoje.AddDays(20), "D", "02", 20, 120),
            Novo("PAP-A4", "Papel sulfite A4 (resma 500 folhas)", 64, 27.50m, 2.300m, null, "D", "01", 40, 200)
        };

        foreach (var exemplo in exemplos)
            await servico.CadastrarAsync(exemplo);

        await servico.RegistrarSaidaAsync(new SaidaRequest
        {
            Codigo = "LUV-010", Quantidade = 6, Responsavel = "João Silva",
            Observacao = "Requisição - manutenção predial"
        });
        await servico.RegistrarEntradaAsync(new EntradaRequest
        {
            Codigo = "PAP-A4", Quantidade = 20, ValorUnitario = 26.90m, Responsavel = "Maria Souza",
            Observacao = "NF 4521"
        });
        await servico.RegistrarSaidaAsync(new SaidaRequest
        {
            Codigo = "DIS-020", Quantidade = 3, Responsavel = "Carlos Lima",
            Observacao = "OS 1187 - quadro elétrico bloco B"
        });

        logger.LogInformation("Produtos de exemplo cadastrados.");

        ProdutoCriarRequest Novo(string codigo, string nome, int quantidade, decimal valor, decimal peso,
            DateOnly? validade, string corredor, string prateleira, int minimo, int maximo) => new()
        {
            Codigo = codigo, Nome = nome, Quantidade = quantidade, ValorUnitario = valor, PesoKg = peso,
            Validade = validade, Endereco = new EnderecoRequest { Corredor = corredor, Prateleira = prateleira },
            EstoqueMinimo = minimo, EstoqueMaximo = maximo, Responsavel = sistema
        };
    }
}
