-- Instalação local vazia. Recusa bancos com tabelas públicas; nunca sobrescreve objetos.
BEGIN;
DO $$ BEGIN IF EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'public') THEN RAISE EXCEPTION 'O esquema public deve estar vazio. Inspecione o banco antes de continuar.'; END IF; END $$;
-- Script v2 do banco CHAMAPRO
-- Base de tabelas para o MVP

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
    pedido_id INT NOT NULL REFERENCES pedidos(id),
    profissional_id INT NOT NULL REFERENCES profissionais(id),
    status VARCHAR(30) NOT NULL DEFAULT 'Pendente',
    PRIMARY KEY (pedido_id, profissional_id)
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

-- Migration 003: complementa o esquema v2 para os fluxos da API.
-- Aplicar somente em uma instalação derivada de chamapro_schema_v2.sql.

ALTER TYPE status_agendamento ADD VALUE IF NOT EXISTS 'Confirmado';
ALTER TYPE status_verificacao ADD VALUE IF NOT EXISTS 'EmAnalise';
ALTER TYPE status_verificacao ADD VALUE IF NOT EXISTS 'DocumentacaoPendente';
ALTER TYPE status_verificacao ADD VALUE IF NOT EXISTS 'Suspenso';

ALTER TABLE usuarios
    ADD COLUMN IF NOT EXISTS telefone VARCHAR(20),
    ADD COLUMN IF NOT EXISTS cidade VARCHAR(100),
    ADD COLUMN IF NOT EXISTS bairro VARCHAR(100),
    ADD COLUMN IF NOT EXISTS endereco_principal VARCHAR(250),
    ADD COLUMN IF NOT EXISTS preferencia_contato VARCHAR(20),
    ADD COLUMN IF NOT EXISTS foto_url VARCHAR(500),
    ADD COLUMN IF NOT EXISTS whatsapp_ativo BOOLEAN NOT NULL DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS mensagem_inicial_whatsapp TEXT;

CREATE UNIQUE INDEX IF NOT EXISTS ux_usuarios_email_lower
    ON usuarios (lower(email));

ALTER TABLE categorias
    ADD COLUMN IF NOT EXISTS icone VARCHAR(80);

ALTER TABLE profissionais
    ADD COLUMN IF NOT EXISTS foto_url VARCHAR(500),
    ADD COLUMN IF NOT EXISTS fotos_trabalhos TEXT[] NOT NULL DEFAULT '{}',
    ADD COLUMN IF NOT EXISTS distancia_max_km NUMERIC(7,2),
    ADD COLUMN IF NOT EXISTS experiencia_anos SMALLINT,
    ADD COLUMN IF NOT EXISTS atende_urgencia BOOLEAN NOT NULL DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS atende_domicilio BOOLEAN NOT NULL DEFAULT TRUE,
    ADD COLUMN IF NOT EXISTS atende_estabelecimento BOOLEAN NOT NULL DEFAULT FALSE;

ALTER TABLE profissional_categorias
    ADD COLUMN IF NOT EXISTS faixa_preco_min NUMERIC(10,2),
    ADD COLUMN IF NOT EXISTS faixa_preco_max NUMERIC(10,2);

ALTER TABLE pedido_destinatarios
    ADD COLUMN IF NOT EXISTS motivo_recusa TEXT;

ALTER TABLE pedidos
    ADD COLUMN IF NOT EXISTS endereco VARCHAR(250),
    ADD COLUMN IF NOT EXISTS tipo_atendimento VARCHAR(20) NOT NULL DEFAULT 'Imediato',
    ADD COLUMN IF NOT EXISTS data_desejada TIMESTAMPTZ;

CREATE UNIQUE INDEX IF NOT EXISTS ux_pedido_destinatarios_pedido_profissional
    ON pedido_destinatarios (pedido_id, profissional_id);

ALTER TABLE agendamentos
    ADD COLUMN IF NOT EXISTS profissional_id INTEGER REFERENCES profissionais(id),
    ADD COLUMN IF NOT EXISTS lembrete_enviado_em TIMESTAMPTZ;

UPDATE agendamentos a
SET profissional_id = p.profissional_id
FROM pedidos p
WHERE p.id = a.pedido_id AND a.profissional_id IS NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ux_agendamentos_pedido
    ON agendamentos (pedido_id);

CREATE UNIQUE INDEX IF NOT EXISTS ux_agendamentos_profissional_horario_ativo
    ON agendamentos (profissional_id, data_hora)
    WHERE status <> 'Cancelado' AND profissional_id IS NOT NULL;

CREATE TABLE IF NOT EXISTS denuncias (
    id SERIAL PRIMARY KEY,
    denunciante_id INTEGER NOT NULL REFERENCES usuarios(id) ON DELETE RESTRICT,
    denunciado_id INTEGER NOT NULL REFERENCES usuarios(id) ON DELETE RESTRICT,
    tipo_entidade VARCHAR(40) NOT NULL,
    entidade_id INTEGER NOT NULL,
    motivo VARCHAR(120) NOT NULL,
    descricao TEXT NOT NULL,
    evidencias TEXT,
    status VARCHAR(30) NOT NULL DEFAULT 'Pendente',
    observacao_admin TEXT,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    atualizado_em TIMESTAMPTZ,
    CONSTRAINT ck_denuncias_nao_autodenuncia CHECK (denunciante_id <> denunciado_id)
);

CREATE TABLE IF NOT EXISTS favoritos (
    cliente_id INTEGER NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
    profissional_id INTEGER NOT NULL REFERENCES profissionais(id) ON DELETE CASCADE,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (cliente_id, profissional_id)
);

CREATE TABLE IF NOT EXISTS notificacoes (
    id BIGSERIAL PRIMARY KEY,
    usuario_id INTEGER NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
    tipo VARCHAR(60) NOT NULL,
    titulo VARCHAR(160) NOT NULL,
    mensagem TEXT NOT NULL,
    dados JSONB NOT NULL DEFAULT '{}'::jsonb,
    criada_em TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    lida_em TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS disponibilidade_profissional (
    profissional_id INTEGER NOT NULL REFERENCES profissionais(id) ON DELETE CASCADE,
    dia_semana SMALLINT NOT NULL CHECK (dia_semana BETWEEN 0 AND 6),
    hora_inicio TIME NOT NULL,
    hora_fim TIME NOT NULL,
    atende_imediato BOOLEAN NOT NULL DEFAULT FALSE,
    atende_agendado BOOLEAN NOT NULL DEFAULT TRUE,
    PRIMARY KEY (profissional_id, dia_semana, hora_inicio),
    CONSTRAINT ck_disponibilidade_intervalo CHECK (hora_fim > hora_inicio)
);

CREATE TABLE IF NOT EXISTS arquivos_privados (
    nome_arquivo VARCHAR(80) PRIMARY KEY,
    usuario_id INTEGER NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS historico_pedidos (
    id BIGSERIAL PRIMARY KEY,
    pedido_id INTEGER NOT NULL REFERENCES pedidos(id) ON DELETE CASCADE,
    ator_id INTEGER REFERENCES usuarios(id) ON DELETE SET NULL,
    status_anterior VARCHAR(30),
    status_novo VARCHAR(30) NOT NULL,
    detalhe TEXT,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_notificacoes_usuario_data
    ON notificacoes (usuario_id, criada_em DESC);
CREATE INDEX IF NOT EXISTS idx_historico_pedidos_pedido_data
    ON historico_pedidos (pedido_id, criado_em);
CREATE INDEX IF NOT EXISTS idx_denuncias_status_data
    ON denuncias (status, criado_em);
CREATE INDEX IF NOT EXISTS idx_disponibilidade_dia
    ON disponibilidade_profissional (dia_semana, hora_inicio, hora_fim);

-- Índices para as FKs que não são cobertas pelo prefixo de uma chave/índice existente.
CREATE INDEX IF NOT EXISTS idx_profissional_categorias_categoria
    ON profissional_categorias (categoria_id);
CREATE INDEX IF NOT EXISTS idx_pedido_destinatarios_profissional
    ON pedido_destinatarios (profissional_id);
CREATE INDEX IF NOT EXISTS idx_orcamentos_pedido_data
    ON orcamentos (pedido_id, criado_em DESC);
CREATE INDEX IF NOT EXISTS idx_orcamentos_profissional
    ON orcamentos (profissional_id);
CREATE INDEX IF NOT EXISTS idx_agendamentos_profissional
    ON agendamentos (profissional_id, data_hora);
CREATE INDEX IF NOT EXISTS idx_avaliacoes_avaliador_data
    ON avaliacoes (avaliador_id, criado_em DESC);
CREATE INDEX IF NOT EXISTS idx_avaliacoes_avaliado_data
    ON avaliacoes (avaliado_id, criado_em DESC);
CREATE INDEX IF NOT EXISTS idx_verificacoes_profissional_status
    ON verificacoes_profissionais (profissional_id, status, criado_em DESC);
CREATE INDEX IF NOT EXISTS idx_denuncias_denunciante
    ON denuncias (denunciante_id, criado_em DESC);
CREATE INDEX IF NOT EXISTS idx_denuncias_denunciado
    ON denuncias (denunciado_id, criado_em DESC);
CREATE INDEX IF NOT EXISTS idx_favoritos_profissional
    ON favoritos (profissional_id);
CREATE INDEX IF NOT EXISTS idx_historico_pedidos_ator
    ON historico_pedidos (ator_id, criado_em DESC);

CREATE INDEX IF NOT EXISTS idx_arquivos_privados_usuario_data
    ON arquivos_privados (usuario_id, criado_em DESC);


-- Migration 004: índices para as chaves estrangeiras apontadas pelo advisor.
-- Aplicar somente depois de chamapro_schema_v2.sql e chamapro_schema_v3.sql.

CREATE INDEX IF NOT EXISTS idx_pedidos_cancelado_por_id
    ON pedidos (cancelado_por_id);

CREATE INDEX IF NOT EXISTS idx_pedidos_categoria_id
    ON pedidos (categoria_id);

CREATE INDEX IF NOT EXISTS idx_pedidos_profissional_id
    ON pedidos (profissional_id);

-- Migration 006: trilha persistente de ações administrativas.
CREATE TABLE IF NOT EXISTS auditoria_admin (
    id BIGSERIAL PRIMARY KEY,
    admin_id INTEGER NOT NULL REFERENCES usuarios(id) ON DELETE RESTRICT,
    acao VARCHAR(80) NOT NULL,
    entidade VARCHAR(60) NOT NULL,
    entidade_id INTEGER NOT NULL,
    estado_anterior VARCHAR(120),
    estado_novo VARCHAR(120),
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_auditoria_admin_criado_em
    ON auditoria_admin (criado_em DESC);
CREATE INDEX IF NOT EXISTS idx_auditoria_admin_entidade
    ON auditoria_admin (entidade, entidade_id, criado_em DESC);


-- Migration 007: índice para a FK do administrador na trilha de auditoria.
CREATE INDEX IF NOT EXISTS idx_auditoria_admin_admin
    ON auditoria_admin (admin_id);

-- Migration 008: tokens privados e de uso único para recuperação de senha.
CREATE TABLE recuperacoes_senha (
    id SERIAL PRIMARY KEY,
    usuario_id INTEGER NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
    token_hash VARCHAR(64) NOT NULL,
    expira_em TIMESTAMPTZ NOT NULL,
    usado_em TIMESTAMPTZ NULL
);

CREATE UNIQUE INDEX ux_recuperacoes_senha_token_hash
    ON recuperacoes_senha (token_hash);
CREATE INDEX idx_recuperacoes_senha_usuario_expira
    ON recuperacoes_senha (usuario_id, expira_em);


COMMIT;
