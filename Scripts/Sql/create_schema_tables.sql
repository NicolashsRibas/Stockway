CREATE DATABASE IF NOT EXISTS stockway
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE stockway;

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
