CREATE DATABASE IF NOT EXISTS stockway
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE stockway;

-- Usuários do login (Models/Usuario.cs). Substitui a antiga tabela `user`.
-- A senha nunca é guardada: senha_hash tem o hash PBKDF2 ("pbkdf2-sha512$iteracoes$salt$hash").
-- nome tem até 60 caracteres porque vira o "responsavel" das movimentações.
CREATE TABLE IF NOT EXISTS usuarios (
    id INT NOT NULL AUTO_INCREMENT,
    nome VARCHAR(60) NOT NULL,
    cargo VARCHAR(60) NOT NULL,
    email VARCHAR(255) NOT NULL,
    senha_hash VARCHAR(255) NOT NULL,
    ativo BOOLEAN NOT NULL DEFAULT TRUE,
    criado_em DATETIME(6) NOT NULL,
    atualizado_em DATETIME(6) NOT NULL,
    ultimo_acesso_em DATETIME(6) NULL,

    CONSTRAINT pk_usuarios PRIMARY KEY (id),
    CONSTRAINT ix_usuarios_email UNIQUE (email)
) ENGINE = InnoDB;

CREATE TABLE IF NOT EXISTS produtos (
    id INT NOT NULL AUTO_INCREMENT,
    codigo VARCHAR(30) NOT NULL,
    nome VARCHAR(100) NOT NULL,
    quantidade INT NOT NULL,
    valor_unitario DECIMAL(18, 2) NOT NULL,
    peso_kg DECIMAL(18, 3) NOT NULL,
    validade DATE NULL,
    corredor VARCHAR(10) NOT NULL,
    prateleira VARCHAR(10) NOT NULL,
    estoque_minimo INT NOT NULL,
    estoque_maximo INT NOT NULL,
    ativo BOOLEAN NOT NULL DEFAULT TRUE,
    criado_em DATETIME(6) NOT NULL,
    atualizado_em DATETIME(6) NOT NULL,

    CONSTRAINT pk_produtos PRIMARY KEY (id),
    CONSTRAINT uq_produtos_codigo UNIQUE (codigo)
) ENGINE = InnoDB;

CREATE TABLE IF NOT EXISTS movimentacoes (
    id BIGINT NOT NULL AUTO_INCREMENT,
    tipo VARCHAR(20) NOT NULL,
    produto_id INT NOT NULL,
    produto_codigo VARCHAR(30) NOT NULL,
    produto_nome VARCHAR(100) NOT NULL,
    quantidade INT NOT NULL,
    saldo_anterior INT NOT NULL,
    saldo_atual INT NOT NULL,
    valor_unitario DECIMAL(18, 2) NOT NULL,
    valor_total DECIMAL(18, 2) NOT NULL,
    responsavel VARCHAR(60) NOT NULL,
    observacao VARCHAR(500) NULL,
    data_hora DATETIME(6) NOT NULL,

    CONSTRAINT pk_movimentacoes PRIMARY KEY (id),
    CONSTRAINT fk_movimentacoes_produtos
        FOREIGN KEY (produto_id) REFERENCES produtos (id),
    INDEX ix_movimentacoes_produto_codigo (produto_codigo),
    INDEX ix_movimentacoes_data_hora (data_hora)
) ENGINE = InnoDB;

-- Histórico do relatório diário enviado por e-mail aos gerentes (Models/RelatorioEnviado.cs).
-- tipo: Automatico | Manual. situacao: Enviado | Falhou | SemDestinatarios.
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
) ENGINE = InnoDB;
