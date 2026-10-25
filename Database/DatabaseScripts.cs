using System.Collections.Generic;

namespace Chamapro.Api.Database;

public static class DatabaseScripts
{
    public const string SchemaV2 = """
CREATE TYPE tipo_usuario AS ENUM ('Cliente', 'Profissional', 'Admin');
CREATE TYPE status_pedido AS ENUM ('Pendente', 'Orcado', 'Aceito', 'Agendado', 'EmAndamento', 'Concluido', 'Cancelado');
CREATE TYPE status_agendamento AS ENUM ('Agendado', 'Concluido', 'Cancelado');
CREATE TYPE status_verificacao AS ENUM ('Pendente', 'Aprovado', 'Rejeitado');

CREATE TABLE usuarios (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(100) NOT NULL,
    email VARCHAR(120) NOT NULL UNIQUE,
    senha_hash VARCHAR(255) NOT NULL,
    tipo tipo_usuario NOT NULL,
    identidade_verificada BOOLEAN NOT NULL DEFAULT FALSE,
    ativo BOOLEAN NOT NULL DEFAULT TRUE,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE categorias (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(80) NOT NULL UNIQUE,
    descricao TEXT,
    ativa BOOLEAN NOT NULL DEFAULT TRUE,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE profissionais (
    id SERIAL PRIMARY KEY,
    usuario_id INT NOT NULL UNIQUE REFERENCES usuarios(id),
    descricao_perfil TEXT,
    preco_min NUMERIC(10,2),
    preco_max NUMERIC(10,2),
    media_avaliacao NUMERIC(2,1) NOT NULL DEFAULT 0.0
);

CREATE TABLE profissional_categorias (
    profissional_id INT NOT NULL REFERENCES profissionais(id),
    categoria_id INT NOT NULL REFERENCES categorias(id),
    PRIMARY KEY (profissional_id, categoria_id)
);

CREATE TABLE areas_atendimento (
    id SERIAL PRIMARY KEY,
    profissional_id INT NOT NULL REFERENCES profissionais(id) ON DELETE CASCADE,
    cidade VARCHAR(100) NOT NULL,
    bairro VARCHAR(100)
);

CREATE TABLE pedidos (
    id SERIAL PRIMARY KEY,
    cliente_id INT NOT NULL REFERENCES usuarios(id),
    categoria_id INT NOT NULL REFERENCES categorias(id),
    titulo VARCHAR(150) NOT NULL,
    descricao TEXT NOT NULL,
    cidade VARCHAR(100) NOT NULL,
    bairro VARCHAR(100),
    foto_urls TEXT[] NOT NULL DEFAULT '{}',
    status status_pedido NOT NULL DEFAULT 'Pendente',
    profissional_id INT REFERENCES profissionais(id),
    cancelado_por_id INT REFERENCES usuarios(id),
    motivo_cancelamento TEXT,
    cancelamento_tardio BOOLEAN NOT NULL DEFAULT FALSE,
    cancelado_em TIMESTAMPTZ,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE pedido_destinatarios (
    id SERIAL PRIMARY KEY,
    pedido_id INT NOT NULL REFERENCES pedidos(id),
    profissional_id INT NOT NULL REFERENCES profissionais(id),
    status VARCHAR(30) NOT NULL DEFAULT 'Pendente'
);

CREATE TABLE orcamentos (
    id SERIAL PRIMARY KEY,
    pedido_id INT NOT NULL REFERENCES pedidos(id),
    profissional_id INT NOT NULL REFERENCES profissionais(id),
    valor NUMERIC(10,2) NOT NULL CHECK (valor > 0),
    prazo_estimado VARCHAR(50) NOT NULL,
    condicoes TEXT,
    valido_ate TIMESTAMPTZ NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'Enviado',
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE agendamentos (
    id SERIAL PRIMARY KEY,
    pedido_id INT NOT NULL UNIQUE REFERENCES pedidos(id),
    data_hora TIMESTAMPTZ NOT NULL,
    status status_agendamento NOT NULL DEFAULT 'Agendado'
);

CREATE TABLE avaliacoes (
    id SERIAL PRIMARY KEY,
    pedido_id INT NOT NULL UNIQUE REFERENCES pedidos(id),
    avaliador_id INT NOT NULL REFERENCES usuarios(id),
    avaliado_id INT NOT NULL REFERENCES usuarios(id),
    nota INT NOT NULL CHECK (nota BETWEEN 1 AND 5),
    comentario TEXT,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE verificacoes_profissionais (
    id SERIAL PRIMARY KEY,
    profissional_id INT NOT NULL REFERENCES profissionais(id),
    tipo_documento VARCHAR(100) NOT NULL,
    url_documento TEXT NOT NULL,
    status status_verificacao NOT NULL DEFAULT 'Pendente',
    observacao TEXT,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_pedidos_cliente ON pedidos(cliente_id);
CREATE INDEX idx_pedidos_status ON pedidos(status);
CREATE INDEX idx_profissionais_usuario ON profissionais(usuario_id);
CREATE INDEX idx_areas_profissional ON areas_atendimento(profissional_id);
""";

    public const string SeedTeste = """
INSERT INTO categorias (nome, descricao, ativa) VALUES
('Eletricista', 'Serviços elétricos residenciais e comerciais', TRUE),
('Encanador', 'Instalações hidráulicas e reparos', TRUE),
('Pintor', 'Pintura residencial e comercial', TRUE);

INSERT INTO usuarios (nome, email, senha_hash, tipo, identidade_verificada, ativo) VALUES
('Administrador Geral', 'admin@chamapro.com', '$2a$12$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQJjzGv0/vD4mB7w1KXN2', 'Admin', TRUE, TRUE),
('Cliente Exemplo', 'cliente@chamapro.com', '$2a$12$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQJjzGv0/vD4mB7w1KXN2', 'Cliente', FALSE, TRUE),
('Profissional Exemplo', 'profissional@chamapro.com', '$2a$12$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36WQJjzGv0/vD4mB7w1KXN2', 'Profissional', FALSE, TRUE);

INSERT INTO profissionais (usuario_id, descricao_perfil, preco_min, preco_max, media_avaliacao)
VALUES (3, 'Especialista em instalação elétrica e manutenção residencial.', 120.00, 350.00, 4.8);

INSERT INTO profissional_categorias (profissional_id, categoria_id)
VALUES (1, 1), (1, 2);

INSERT INTO areas_atendimento (profissional_id, cidade, bairro)
VALUES (1, 'São Paulo', 'Centro');
""";
}
