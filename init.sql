-- Habilita extensão para geração de UUIDs nativos no PostgreSQL
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Tabela de Usuários (Sprint 1 já inclusa)
CREATE TABLE usuarios (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    nome VARCHAR(120) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    senha_hash VARCHAR(255) NOT NULL,
    foto_url VARCHAR(500),
    bio VARCHAR(500),
    curso_ou_area VARCHAR(120),
    instituicao VARCHAR(150),
    topicos_interesse TEXT[] NOT NULL DEFAULT '{}',
    topicos_ajuda TEXT[] NOT NULL DEFAULT '{}',
    criado_em TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Tabela de Quadros / Grupos (Reaproveita grupos_estudo para Kanban Pessoal e Trabalho Acadêmico)
CREATE TABLE grupos_estudo (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    titulo VARCHAR(150) NOT NULL,
    descricao TEXT,
    tipo VARCHAR(30) NOT NULL DEFAULT 'TRABALHO_ACADEMICO', -- 'PESSOAL' | 'TRABALHO_ACADEMICO'
    dono_id UUID NOT NULL,
    criado_em TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_grupo_dono FOREIGN KEY (dono_id) REFERENCES usuarios(id) ON DELETE RESTRICT,
    CONSTRAINT chk_tipo_grupo CHECK (tipo IN ('PESSOAL', 'TRABALHO_ACADEMICO'))
);

-- Tabela de Membros e Convites (Controla quem pode ver/editar quadros compartilhados)
CREATE TABLE membros_grupo (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    grupo_id UUID NOT NULL,
    usuario_id UUID NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'Pendente', -- 'Pendente', 'Aceito', 'Recusado'
    convidado_em TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    respondido_em TIMESTAMP WITH TIME ZONE,
    CONSTRAINT fk_membro_grupo FOREIGN KEY (grupo_id) REFERENCES grupos_estudo(id) ON DELETE CASCADE,
    CONSTRAINT fk_membro_usuario FOREIGN KEY (usuario_id) REFERENCES usuarios(id) ON DELETE CASCADE,
    CONSTRAINT uq_grupo_usuario UNIQUE (grupo_id, usuario_id),
    CONSTRAINT chk_status_membro CHECK (status IN ('Pendente', 'Aceito', 'Recusado'))
);

-- Tabela de Colunas do Kanban (Criadas automaticamente ao criar um quadro: A Fazer, Em Andamento, Revisão, Concluído)
CREATE TABLE colunas_kanban (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    grupo_id UUID NOT NULL,
    titulo VARCHAR(80) NOT NULL,
    ordem INTEGER NOT NULL DEFAULT 0,
    criado_em TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_coluna_grupo FOREIGN KEY (grupo_id) REFERENCES grupos_estudo(id) ON DELETE CASCADE
);

-- Tabela de Cards / Tópicos (Reaproveita topicos_estudo evoluído para cards Kanban com responsável)
CREATE TABLE topicos_estudo (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    grupo_id UUID NOT NULL,
    coluna_id UUID NOT NULL,
    criado_por_id UUID NOT NULL,
    responsavel_id UUID, -- Nulo se sem dono ou no modo PESSOAL
    titulo VARCHAR(200) NOT NULL,
    descricao TEXT,
    prioridade VARCHAR(20) NOT NULL DEFAULT 'MEDIA', -- 'BAIXA' | 'MEDIA' | 'ALTA'
    ordem INTEGER NOT NULL DEFAULT 0,
    data_prevista DATE,
    concluido BOOLEAN NOT NULL DEFAULT FALSE,
    criado_em TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_topico_grupo FOREIGN KEY (grupo_id) REFERENCES grupos_estudo(id) ON DELETE CASCADE,
    CONSTRAINT fk_topico_coluna FOREIGN KEY (coluna_id) REFERENCES colunas_kanban(id) ON DELETE CASCADE,
    CONSTRAINT fk_topico_criador FOREIGN KEY (criado_por_id) REFERENCES usuarios(id) ON DELETE RESTRICT,
    CONSTRAINT fk_topico_responsavel FOREIGN KEY (responsavel_id) REFERENCES usuarios(id) ON DELETE SET NULL,
    CONSTRAINT chk_prioridade_topico CHECK (prioridade IN ('BAIXA', 'MEDIA', 'ALTA'))
);

-- Índices para acelerar buscas frequentes da API
CREATE INDEX idx_grupos_dono ON grupos_estudo(dono_id);
CREATE INDEX idx_grupos_tipo ON grupos_estudo(tipo);
CREATE INDEX idx_membros_usuario_status ON membros_grupo(usuario_id, status);
CREATE INDEX idx_colunas_grupo_ordem ON colunas_kanban(grupo_id, ordem);
CREATE INDEX idx_topicos_grupo ON topicos_estudo(grupo_id);
CREATE INDEX idx_topicos_coluna_ordem ON topicos_estudo(coluna_id, ordem);
CREATE INDEX idx_topicos_responsavel ON topicos_estudo(responsavel_id);

-- Comunidade: publicações do feed (pedido de estudo, aula, link ou documento)
CREATE TABLE IF NOT EXISTS publicacoes (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    autor_id UUID NOT NULL,
    tipo VARCHAR(20) NOT NULL DEFAULT 'ESTUDO', -- 'ESTUDO' | 'AULA' | 'LINK' | 'DOCUMENTO'
    titulo VARCHAR(150),
    conteudo VARCHAR(1000) NOT NULL,
    link_url VARCHAR(500),
    arquivo_url VARCHAR(500),
    arquivo_nome VARCHAR(255),
    criado_em TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_publicacao_autor FOREIGN KEY (autor_id) REFERENCES usuarios(id) ON DELETE RESTRICT,
    CONSTRAINT chk_tipo_publicacao CHECK (tipo IN ('ESTUDO', 'AULA', 'LINK', 'DOCUMENTO'))
);

-- Comunidade: comentários das publicações
CREATE TABLE IF NOT EXISTS comentarios_publicacao (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    publicacao_id UUID NOT NULL,
    autor_id UUID NOT NULL,
    conteudo VARCHAR(500) NOT NULL,
    criado_em TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_comentario_publicacao FOREIGN KEY (publicacao_id) REFERENCES publicacoes(id) ON DELETE CASCADE,
    CONSTRAINT fk_comentario_autor FOREIGN KEY (autor_id) REFERENCES usuarios(id) ON DELETE RESTRICT
);

-- Comunidade: curtidas (uma por usuário em cada publicação)
CREATE TABLE IF NOT EXISTS curtidas_publicacao (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    publicacao_id UUID NOT NULL,
    usuario_id UUID NOT NULL,
    criado_em TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_curtida_publicacao FOREIGN KEY (publicacao_id) REFERENCES publicacoes(id) ON DELETE CASCADE,
    CONSTRAINT fk_curtida_usuario FOREIGN KEY (usuario_id) REFERENCES usuarios(id) ON DELETE CASCADE,
    CONSTRAINT uq_publicacao_usuario UNIQUE (publicacao_id, usuario_id)
);

CREATE INDEX IF NOT EXISTS idx_publicacoes_autor ON publicacoes(autor_id);
CREATE INDEX IF NOT EXISTS idx_publicacoes_criado_em ON publicacoes(criado_em);
CREATE INDEX IF NOT EXISTS idx_comentarios_publicacao ON comentarios_publicacao(publicacao_id, criado_em);
CREATE INDEX IF NOT EXISTS idx_curtidas_usuario ON curtidas_publicacao(usuario_id);