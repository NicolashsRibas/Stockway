using Microsoft.EntityFrameworkCore;
using StrockWay.Models;

namespace StrockWay.Data;

/// <summary>Acesso ao banco SQLite (arquivo strockway.db) via Entity Framework Core.</summary>
public class StrockWayContext(DbContextOptions<StrockWayContext> options) : DbContext(options)
{
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Movimentacao> Movimentacoes => Set<Movimentacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Produto>(e =>
        {
            e.ToTable("produtos");
            e.HasIndex(p => p.Codigo).IsUnique();
            e.Property(p => p.Codigo).HasMaxLength(Produto.CodigoMax).IsRequired();
            e.Property(p => p.Nome).HasMaxLength(Produto.NomeMax).IsRequired();
            e.Property(p => p.Corredor).HasMaxLength(Produto.EnderecoMax).IsRequired();
            e.Property(p => p.Prateleira).HasMaxLength(Produto.EnderecoMax).IsRequired();
        });

        modelBuilder.Entity<Movimentacao>(e =>
        {
            e.ToTable("movimentacoes");
            e.Property(m => m.Tipo).HasConversion<string>().HasMaxLength(20);
            e.Property(m => m.ProdutoCodigo).HasMaxLength(Produto.CodigoMax).IsRequired();
            e.Property(m => m.ProdutoNome).HasMaxLength(Produto.NomeMax).IsRequired();
            e.Property(m => m.Responsavel).HasMaxLength(Movimentacao.ResponsavelMax).IsRequired();
            e.Property(m => m.Observacao).HasMaxLength(Movimentacao.ObservacaoMaxInterna);
            e.HasIndex(m => m.ProdutoCodigo);
            e.HasIndex(m => m.DataHora);
        });
    }
}
