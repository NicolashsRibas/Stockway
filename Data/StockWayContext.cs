using Microsoft.EntityFrameworkCore;
using StockWay.Models;

namespace StockWay.Data;

/// <summary>Acesso ao banco MySQL via Entity Framework Core.</summary>
public class StockWayContext(DbContextOptions<StockWayContext> options) : DbContext(options)
{
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Movimentacao> Movimentacoes => Set<Movimentacao>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<RelatorioEnviado> RelatoriosEnviados => Set<RelatorioEnviado>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("utf8mb4_unicode_ci");
        modelBuilder.HasCharSet("utf8mb4");

        modelBuilder.Entity<Produto>(e =>
        {
            e.ToTable("produtos");
            e.HasIndex(p => p.Codigo).IsUnique();
            e.Property(p => p.Codigo).HasMaxLength(Produto.CodigoMax).IsRequired();
            e.Property(p => p.Nome).HasMaxLength(Produto.NomeMax).IsRequired();
            e.Property(p => p.Corredor).HasMaxLength(Produto.EnderecoMax).IsRequired();
            e.Property(p => p.Prateleira).HasMaxLength(Produto.EnderecoMax).IsRequired();
            e.Property(p => p.ValorUnitario).HasPrecision(18, 2);
            e.Property(p => p.PesoKg).HasPrecision(18, 3);

            // Controle de concorrência: o UPDATE só é aplicado se o saldo e a data de atualização
            // ainda forem os que foram lidos. Se outra operação (inclusive de outra instância da API)
            // mexeu no produto nesse meio-tempo, nada é gravado e a API responde 409.
            // Não cria colunas novas no banco.
            e.Property(p => p.Quantidade).IsConcurrencyToken();
            e.Property(p => p.AtualizadoEm).IsConcurrencyToken();
        });

        modelBuilder.Entity<Movimentacao>(e =>
        {
            e.ToTable("movimentacoes");
            e.Property(m => m.Tipo).HasConversion<string>().HasMaxLength(20);
            e.Property(m => m.ProdutoCodigo).HasMaxLength(Produto.CodigoMax).IsRequired();
            e.Property(m => m.ProdutoNome).HasMaxLength(Produto.NomeMax).IsRequired();
            e.Property(m => m.Responsavel).HasMaxLength(Movimentacao.ResponsavelMax).IsRequired();
            e.Property(m => m.Observacao).HasMaxLength(Movimentacao.ObservacaoMaxInterna);
            e.Property(m => m.ValorUnitario).HasPrecision(18, 2);
            e.Property(m => m.ValorTotal).HasPrecision(18, 2);
            e.HasOne<Produto>()
                .WithMany()
                .HasForeignKey(m => m.ProdutoId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(m => m.ProdutoCodigo);
            e.HasIndex(m => m.DataHora);
        });

        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("usuarios");
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Nome).HasMaxLength(Usuario.NomeMax).IsRequired();
            e.Property(u => u.Cargo).HasMaxLength(Usuario.CargoMax).IsRequired();
            e.Property(u => u.Email).HasMaxLength(Usuario.EmailMax).IsRequired();
            e.Property(u => u.SenhaHash).HasMaxLength(Usuario.SenhaHashMax).IsRequired();
        });

        modelBuilder.Entity<RelatorioEnviado>(e =>
        {
            e.ToTable("relatorios_enviados");
            e.Property(r => r.Tipo).HasConversion<string>().HasMaxLength(20);
            e.Property(r => r.Situacao).HasConversion<string>().HasMaxLength(20);
            e.Property(r => r.Destinatarios).HasMaxLength(RelatorioEnviado.DestinatariosMax).IsRequired();
            e.Property(r => r.Mensagem).HasMaxLength(RelatorioEnviado.MensagemMax);
            e.Property(r => r.SolicitadoPor).HasMaxLength(Movimentacao.ResponsavelMax);
            e.HasIndex(r => r.DataReferencia);
        });
    }
}
