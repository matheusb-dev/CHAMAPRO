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
