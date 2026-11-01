-- Migration 004: índices para as chaves estrangeiras apontadas pelo advisor.
-- Aplicar somente depois de chamapro_schema_v2.sql e chamapro_schema_v3.sql.

CREATE INDEX IF NOT EXISTS idx_pedidos_cancelado_por_id
    ON pedidos (cancelado_por_id);

CREATE INDEX IF NOT EXISTS idx_pedidos_categoria_id
    ON pedidos (categoria_id);

CREATE INDEX IF NOT EXISTS idx_pedidos_profissional_id
    ON pedidos (profissional_id);
