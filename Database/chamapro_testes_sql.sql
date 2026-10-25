-- Script de testes SQL para validar regras de integridade do CHAMAPRO
-- Execute após a estrutura principal.

-- CT-01: usuário sem e-mail inválido
SELECT * FROM usuarios WHERE email IS NULL;

-- CT-02: categoria duplicada
SELECT nome, COUNT(*) FROM categorias GROUP BY nome HAVING COUNT(*) > 1;

-- CT-03: profissional sem usuário
SELECT * FROM profissionais p LEFT JOIN usuarios u ON u.id = p.usuario_id WHERE u.id IS NULL;

-- CT-04: orçamento com valor <= 0
SELECT * FROM orcamentos WHERE valor <= 0;

-- CT-15: verificação de documento rejeitada ou pendente
SELECT * FROM verificacoes_profissionais WHERE status IN ('Pendente', 'Rejeitado');
