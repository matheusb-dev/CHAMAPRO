-- Migration 007: índice para a FK do administrador na trilha de auditoria.
CREATE INDEX IF NOT EXISTS idx_auditoria_admin_admin
    ON auditoria_admin (admin_id);
