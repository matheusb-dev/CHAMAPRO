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

ALTER TABLE recuperacoes_senha ENABLE ROW LEVEL SECURITY;
REVOKE ALL PRIVILEGES ON TABLE recuperacoes_senha FROM PUBLIC, anon, authenticated;
REVOKE ALL PRIVILEGES ON SEQUENCE recuperacoes_senha_id_seq FROM PUBLIC, anon, authenticated;
