-- Habilita extensão para geração de UUIDs nativos no PostgreSQL
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Tabela de Usuários
CREATE TABLE usuarios (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    nome VARCHAR(120) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    senha_hash VARCHAR(255) NOT NULL,
    criado_em TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Tabela de Grupos de Estudo (Aggregate Root)
CREATE TABLE grupos_estudo (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    titulo VARCHAR(150) NOT NULL,
    descricao TEXT,
    dono_id UUID NOT NULL,
    criado_em TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_grupo_dono FOREIGN KEY (dono_id) REFERENCES usuarios(id) ON DELETE RESTRICT
);

-- Tabela de Membros e Convites (Controla quem pode ver/editar o estudo)
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

-- Tabela de Tópicos / Rotina de Estudo dentro do Grupo
CREATE TABLE topicos_estudo (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    grupo_id UUID NOT NULL,
    criado_por_id UUID NOT NULL,
    titulo VARCHAR(200) NOT NULL,
    descricao TEXT,
    data_prevista DATE,
    concluido BOOLEAN NOT NULL DEFAULT FALSE,
    criado_em TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_topico_grupo FOREIGN KEY (grupo_id) REFERENCES grupos_estudo(id) ON DELETE CASCADE,
    CONSTRAINT fk_topico_criador FOREIGN KEY (criado_por_id) REFERENCES usuarios(id) ON DELETE RESTRICT
);

-- Índices para acelerar buscas frequentes da API
CREATE INDEX idx_grupos_dono ON grupos_estudo(dono_id);
CREATE INDEX idx_membros_usuario_status ON membros_grupo(usuario_id, status);
CREATE INDEX idx_topicos_grupo ON topicos_estudo(grupo_id);