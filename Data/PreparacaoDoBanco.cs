using Microsoft.EntityFrameworkCore;

namespace StockWay.Data;

/// <summary>
/// Cria o banco e as tabelas ao iniciar a API.
///
/// O EnsureCreated() só cria tabelas quando o banco está vazio. Em um banco que já tinha
/// "produtos" e "movimentacoes" (de versões anteriores), ele não faz nada. Por isso as
/// tabelas que chegaram depois são criadas aqui com CREATE TABLE IF NOT EXISTS,
/// que pode rodar quantas vezes for preciso sem estragar nada.
///
/// O SQL abaixo precisa bater com o mapeamento do StockWayContext e com
/// Scripts/Sql/create_schema_tables.sql.
/// </summary>
public static class PreparacaoDoBanco
{
    private const string CriarTabelaUsuarios = """
        CREATE TABLE IF NOT EXISTS usuarios (
            id INT NOT NULL AUTO_INCREMENT,
            nome VARCHAR(60) NOT NULL,
            cargo VARCHAR(60) NOT NULL,
            email VARCHAR(255) NOT NULL,
            senha_hash VARCHAR(255) NOT NULL,
            ativo TINYINT(1) NOT NULL,
            criado_em DATETIME(6) NOT NULL,
            atualizado_em DATETIME(6) NOT NULL,
            ultimo_acesso_em DATETIME(6) NULL,
            CONSTRAINT pk_usuarios PRIMARY KEY (id),
            CONSTRAINT ix_usuarios_email UNIQUE (email)
        ) ENGINE = InnoDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
        """;

    private const string CriarTabelaRelatoriosEnviados = """
        CREATE TABLE IF NOT EXISTS relatorios_enviados (
            id BIGINT NOT NULL AUTO_INCREMENT,
            tipo VARCHAR(20) NOT NULL,
            data_referencia DATE NOT NULL,
            periodo_inicio DATETIME(6) NOT NULL,
            periodo_fim DATETIME(6) NOT NULL,
            destinatarios VARCHAR(2000) NOT NULL,
            quantidade_destinatarios INT NOT NULL,
            situacao VARCHAR(20) NOT NULL,
            mensagem VARCHAR(500) NULL,
            solicitado_por VARCHAR(60) NULL,
            criado_em DATETIME(6) NOT NULL,
            CONSTRAINT pk_relatorios_enviados PRIMARY KEY (id),
            INDEX ix_relatorios_enviados_data_referencia (data_referencia)
        ) ENGINE = InnoDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
        """;

    public static void Executar(StockWayContext db)
    {
        db.Database.EnsureCreated();
        db.Database.ExecuteSqlRaw(CriarTabelaUsuarios);
        db.Database.ExecuteSqlRaw(CriarTabelaRelatoriosEnviados);
    }
}
