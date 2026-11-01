-- Migration 005: a API usa uma conexão PostgreSQL privada; não expor tabelas
-- nem sequências públicas para anon/authenticated ou para o papel PUBLIC.
REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA public FROM PUBLIC, anon, authenticated;
REVOKE ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public FROM PUBLIC, anon, authenticated;

-- Mantém o mesmo padrão restrito para novas tabelas/sequências criadas pelo
-- proprietário do projeto em futuras migrações.
ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA public
    REVOKE ALL ON TABLES FROM PUBLIC, anon, authenticated;
ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA public
    REVOKE ALL ON SEQUENCES FROM PUBLIC, anon, authenticated;
