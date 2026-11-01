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

-- Taxonomia inicial do produto; não inclui contas, chamados ou profissionais de exemplo.
INSERT INTO categorias (nome, descricao, ativa, icone) VALUES
    ('Eletricista', 'Instalações, manutenção e reparos elétricos.', TRUE, 'bolt'),
    ('Encanador', 'Instalações hidráulicas, vazamentos e reparos.', TRUE, 'water'),
    ('Pedreiro', 'Alvenaria, reformas e pequenos reparos.', TRUE, 'wall'),
    ('Pintor', 'Pintura residencial e comercial.', TRUE, 'paint'),
    ('Chaveiro', 'Abertura, instalação e reparo de fechaduras.', TRUE, 'key'),
    ('Mecânico', 'Manutenção e reparo automotivo.', TRUE, 'car'),
    ('Técnico de informática', 'Suporte e manutenção de computadores e redes.', TRUE, 'computer'),
    ('Diarista', 'Limpeza residencial e organização.', TRUE, 'cleaning'),
    ('Montador de móveis', 'Montagem, desmontagem e ajuste de móveis.', TRUE, 'furniture')
ON CONFLICT (nome) DO NOTHING;

-- A API usa a conexão privada do servidor. Não exponha estas tabelas diretamente via PostgREST.
ALTER TABLE usuarios ENABLE ROW LEVEL SECURITY;
ALTER TABLE categorias ENABLE ROW LEVEL SECURITY;
ALTER TABLE profissionais ENABLE ROW LEVEL SECURITY;
ALTER TABLE profissional_categorias ENABLE ROW LEVEL SECURITY;
ALTER TABLE areas_atendimento ENABLE ROW LEVEL SECURITY;
ALTER TABLE pedidos ENABLE ROW LEVEL SECURITY;
ALTER TABLE pedido_destinatarios ENABLE ROW LEVEL SECURITY;
ALTER TABLE orcamentos ENABLE ROW LEVEL SECURITY;
ALTER TABLE agendamentos ENABLE ROW LEVEL SECURITY;
ALTER TABLE avaliacoes ENABLE ROW LEVEL SECURITY;
ALTER TABLE verificacoes_profissionais ENABLE ROW LEVEL SECURITY;
ALTER TABLE denuncias ENABLE ROW LEVEL SECURITY;
ALTER TABLE favoritos ENABLE ROW LEVEL SECURITY;
ALTER TABLE notificacoes ENABLE ROW LEVEL SECURITY;
ALTER TABLE disponibilidade_profissional ENABLE ROW LEVEL SECURITY;
ALTER TABLE arquivos_privados ENABLE ROW LEVEL SECURITY;
ALTER TABLE historico_pedidos ENABLE ROW LEVEL SECURITY;
