# CHAMAPRO

Sistema completo de plataforma de serviços para conectar clientes e profissionais, com fluxo de pedidos, orçamentos, agendamentos, avaliações, autenticação, painel administrativo e integração com banco de dados.

## Visão geral

O CHAMAPRO é uma aplicação web para contratação de serviços, desenvolvida com arquitetura em camadas e comunicação entre frontend, API e banco de dados. A solução foi construída em .NET com ASP.NET Core, utilizando PostgreSQL no Supabase e autenticação JWT para controle de acesso por perfil.

O sistema permite:
- cadastro de clientes e profissionais
- autenticação e autorização por roles
- busca de profissionais por categoria e localização
- criação de pedidos e orçamentos
- seleção de profissional por orçamento
- agendamento de serviços
- avaliação da prestação de serviço
- verificação do perfil profissional
- painel administrativo para gestão do sistema
- documentação de endpoints via Swagger

---

## Status do projeto

Status atual: concluído e entregue.

### ✅ Implementado
- API REST em ASP.NET Core
- Controllers, Services, Repositories e DTOs
- Entities e modelagem completa do domínio
- Entity Framework Core com PostgreSQL
- AppDbContext com mapeamento das entidades
- Chaves primárias e chaves compostas
- Autenticação JWT
- Controle de acesso por perfil
- Hash de senha com BCrypt
- Tratamento centralizado de erros
- Swagger/OpenAPI
- Banco PostgreSQL no Supabase
- Scripts de criação, seed e testes
- DER e documentação técnica
- Frontend web responsivo
- Fluxos de login, cadastro, cliente, profissional e admin
- Validação de regras de negócio
- Documentação técnica final do projeto
- Versionamento com Git

---

## Arquitetura da solução

O CHAMAPRO segue uma arquitetura em camadas:

- Frontend: interface web para usuários
- API: camada de regras de negócio e endpoints
- Banco de dados: persistência e consultas

### Camadas da API
- Controllers: recebem as requisições HTTP
- Services: aplicam regras de negócio
- Repositories: acessam o banco via Entity Framework
- Entities: representam tabelas do banco
- DTOs: definem contratos de entrada e saída
- Middlewares: tratam erros, autenticação e contexto do usuário
- Data: AppDbContext e configurações do banco

---

## Tecnologias utilizadas

### Backend
- C#
- .NET
- ASP.NET Core
- Entity Framework Core
- PostgreSQL
- Supabase
- JWT
- BCrypt.Net-Next

### Frontend
- HTML
- CSS
- JavaScript
- Interface responsiva

### Documentação
- Swagger UI
- OpenAPI
- Documentação técnica em Markdown

---

## Estrutura do projeto

```text
CHAMAPRO/
├── Application/
├── Domain/
├── Infrastructure/
├── Presentation/
├── Controllers/
├── Services/
├── Repositories/
├── Models/
│   ├── Entities/
│   └── Enums/
├── DTOs/
│   ├── Requests/
│   └── Responses/
├── Data/
│   ├── Configurations/
│   └── AppDbContext.cs
├── Common/
├── Middlewares/
├── Database/
│   ├── chamapro_schema_v2.sql
│   ├── chamapro_seed_teste.sql
│   ├── chamapro_testes_sql.sql
│   └── DER.md
├── wwwroot/
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
├── Chamapro.Api.csproj
├── Chamapro.Api.slnx
├── README.md
├── README-ESQUELETO.md
├── ARQUITETURA_EM_CAMADAS.md
├── .gitignore
├── .gitattributes
├── Testes/
└── Properties/
```

---

## Funcionalidades do sistema

### Autenticação
- Cadastro de usuários
- Login
- Geração de token JWT
- Controle de acesso por role
- Proteção de endpoints
- Validação de usuário autenticado
- Perfil de cliente, profissional e administrador

### Cliente
- cadastro e edição de dados
- navegação por profissionais
- envio de pedidos
- acompanhamento de status
- visualização de orçamentos
- confirmação de agendamento
- avaliação de serviços

### Profissional
- perfil público e profissional
- definição de categorias atendidas
- definição de áreas de atendimento
- visualização de pedidos recebidos
- envio de orçamento
- aceitação de agenda e disponibilidade
- avaliação recebida

### Admin
- gerenciamento de categorias
- gestão de usuários e perfis
- monitoramento de pedidos
- controle geral do sistema
- validação administrativa

### Banco de dados
- tabelas criadas conforme modelagem
- relacionamentos mapeados no EF Core
- índices e restrições definidas
- scripts de seed e testes

---

## Modelagem do banco

A modelagem do sistema foi criada para suportar:
- usuários
- profissionais
- categorias
- áreas de atendimento
- pedidos
- destinatários do pedido
- orçamentos
- agendamentos
- avaliações
- verificações de profissional

Principais entidades:
- Usuario
- Categoria
- Profissional
- ProfissionalCategoria
- AreaAtendimento
- Pedido
- PedidoDestinatario
- Orcamento
- Agendamento
- Avaliacao
- VerificacaoProfissional

---

## Endpoints principais

### Autenticação
- POST /api/v1/auth/login
- POST /api/v1/auth/register
- POST /api/v1/auth/cliente/register
- POST /api/v1/auth/profissional/register

### Categorias
- GET /api/v1/categorias
- POST /api/v1/categorias
- PUT /api/v1/categorias/{id}
- DELETE /api/v1/categorias/{id}

### Profissionais
- GET /api/v1/profissionais
- GET /api/v1/profissionais/{id}
- PUT /api/v1/profissionais/perfil
- POST /api/v1/profissionais/servicos
- POST /api/v1/profissionais/areas

### Pedidos
- GET /api/v1/pedidos
- POST /api/v1/pedidos
- PUT /api/v1/pedidos/{id}/status
- POST /api/v1/pedidos/{id}/cancelar

### Orçamentos
- GET /api/v1/orcamentos
- POST /api/v1/orcamentos
- PUT /api/v1/orcamentos/{id}/aceitar
- PUT /api/v1/orcamentos/{id}/recusar

### Agendamentos
- GET /api/v1/agendamentos
- POST /api/v1/agendamentos
- PUT /api/v1/agendamentos/{id}

### Avaliações
- GET /api/v1/avaliacoes
- POST /api/v1/avaliacoes

### Verificações
- GET /api/v1/verificacoes
- POST /api/v1/verificacoes
- PUT /api/v1/verificacoes/{id}

### Administração
- GET /api/v1/admin
- endpoints administrativos de gestão e relatórios

---

## Autenticação e autorização

O sistema utiliza JWT e perfis de acesso.

### Perfis
- CLIENTE
- PROFISSIONAL
- ADMIN

### Segurança
- senhas armazenadas com hash BCrypt
- tokens gerados com expiração definida
- endpoints protegidos por [Authorize]
- acesso administrativo restrito a usuários com role ADMIN
- regras de negócio verificando autorização do usuário autenticado

---

## Banco e persistência

### Banco utilizado
- PostgreSQL hospedado no Supabase

### Técnicas adotadas
- Entity Framework Core
- LINQ
- mapeamento com DbContext
- relacionamento entre entidades
- chave primária e composta
- scripts de seed e testes

### Arquivos do banco
- Database/chamapro_schema_v2.sql
- Database/chamapro_seed_teste.sql
- Database/chamapro_testes_sql.sql
- Database/DER.md

---

## Frontend

O frontend do CHAMAPRO foi desenvolvido para oferecer uma experiência web responsiva com fluxos principais para:
- login
- cadastro
- cliente
- profissional
- administração

Arquivos principais:
- wwwroot/login.html
- wwwroot/cadastro.html
- wwwroot/admin.html
- wwwroot/styles.css
- wwwroot/app.js

---

## Como executar o projeto

### Requisitos
- .NET SDK instalado
- PostgreSQL configurado ou acesso ao Supabase
- Editor de código ou Visual Studio
- Git

### Passos
1. Clone o repositório
2. Configure a string de conexão do banco
3. Ajuste as variáveis de ambiente ou secrets locais
4. Execute o restore
5. Compile e rode a aplicação

```bash
dotnet restore
dotnet build
dotnet run
```

### Ambiente de desenvolvimento
O projeto pode ser acessado via:
- Swagger em `/swagger`
- API em `/api/v1/...`

---

## Configuração do ambiente

As configurações do projeto ficam em:
- appsettings.json
- appsettings.Development.json

É recomendado armazenar dados sensíveis em:
- User Secrets
- Variáveis de ambiente
- Configuração do ambiente local

---

## Testes e validação

O projeto foi validado com:
- testes de modelagem de dados
- testes SQL
- validação de autenticação
- validação de regras de negócio
- validação de estrutura do banco
- testes de endpoints e respostas

### Evidências de testes
- scripts de testes incluídos na pasta Database
- fluxo principal de autenticação validado
- schema principal do banco validado
- estruturas e relacionamentos do domínio validados

---

## Controle de versão

O projeto foi versionado com Git e mantém histórico de evolução técnica do sistema.

---

## Documentação

A documentação do projeto está disponível em:
- README.md
- README-ESQUELETO.md
- ARQUITETURA_EM_CAMADAS.md
- Database/DER.md
- Database/chamapro_schema_v2.sql
- Database/chamapro_testes_sql.sql

---

## Conclusão

O CHAMAPRO foi desenvolvido com foco em arquitetura limpa, modular e escalável, com API em .NET, banco em PostgreSQL, autenticação JWT e frontend inicial integrado.

O projeto atende aos requisitos previstos para uma plataforma de marketplace de serviços, com fluxo principal de cliente, profissional e administração funcionando dentro da estrutura planejada.

---

## Licença

Este projeto foi desenvolvido para fins acadêmicos e de apresentação de desenvolvimento de software.
