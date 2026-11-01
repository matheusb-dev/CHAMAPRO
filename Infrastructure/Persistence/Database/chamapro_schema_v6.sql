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

ALTER TABLE auditoria_admin ENABLE ROW LEVEL SECURITY;
REVOKE ALL PRIVILEGES ON TABLE auditoria_admin FROM PUBLIC, anon, authenticated;
REVOKE ALL PRIVILEGES ON SEQUENCE auditoria_admin_id_seq FROM PUBLIC, anon, authenticated;
