erDiagram
    USUARIOS ||--o{ PEDIDOS : realiza
    USUARIOS ||--o{ AVALIACOES : avalia
    USUARIOS ||--|| PROFISSIONAIS : possui
    CATEGORIAS ||--o{ PEDIDOS : classifica
    CATEGORIAS ||--o{ PROFISSIONAL_CATEGORIAS : relaciona
    PROFISSIONAIS ||--o{ PROFISSIONAL_CATEGORIAS : atua_em
    PROFISSIONAIS ||--o{ AREAS_ATENDIMENTO : atende
    PROFISSIONAIS ||--o{ ORCAMENTOS : envia
    PEDIDOS ||--o{ ORCAMENTOS : recebe
    PEDIDOS ||--o| AGENDAMENTOS : agenda
    PEDIDOS ||--o| AVALIACOES : avalia
    PEDIDOS ||--o{ PEDIDO_DESTINATARIOS : possui
    PROFISSIONAIS ||--o{ PEDIDO_DESTINATARIOS : recebe
    PROFISSIONAIS ||--o{ VERIFICACOES_PROFISSIONAIS : envia_documentos

    USUARIOS {
        int id PK
        string nome
        string email
        string senha_hash
        enum tipo
        bool identidade_verificada
        bool ativo
        datetime criado_em
    }

    PROFISSIONAIS {
        int id PK
        int usuario_id FK
        text descricao_perfil
        decimal preco_min
        decimal preco_max
        decimal media_avaliacao
    }

    CATEGORIAS {
        int id PK
        string nome
        string descricao
        bool ativa
        datetime criado_em
    }

    PEDIDOS {
        int id PK
        int cliente_id FK
        int categoria_id FK
        string titulo
        string descricao
        string cidade
        string bairro
        text[] foto_urls
        enum status
        int profissional_id FK
        int cancelado_por_id FK
        string motivo_cancelamento
        bool cancelamento_tardio
        datetime cancelado_em
        datetime criado_em
    }

    ORCAMENTOS {
        int id PK
        int pedido_id FK
        int profissional_id FK
        decimal valor
        string prazo_estimado
        text condicoes
        datetime valido_ate
        string status
        datetime criado_em
    }

    AGENDAMENTOS {
        int id PK
        int pedido_id FK
        datetime data_hora
        enum status
    }

    AVALIACOES {
        int id PK
        int pedido_id FK
        int avaliador_id FK
        int avaliado_id FK
        int nota
        text comentario
    }

    VERIFICACOES_PROFISSIONAIS {
        int id PK
        int profissional_id FK
        string tipo_documento
        string url_documento
        enum status
        text observacao
        datetime criado_em
    }
