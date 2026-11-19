# MeuEstudo API
>>>>>>> 5895960 (atualizacao para o singular para organizacao)

Backend em **.NET 10** para organização de estudos em quadros **Kanban**, com grupos
de estudo, convites entre usuários, perfis com foto e atualização do quadro em
tempo real via **SignalR**.

A API segue uma separação em camadas no estilo Clean Architecture, com regras de
negócio isoladas no domínio e persistência em **PostgreSQL**.

---

## Tecnologias

| Camada / recurso | Tecnologia |
| --- | --- |
| Runtime / framework | .NET 10, ASP.NET Core |
| Persistência | EF Core 10 + Npgsql (`UseSnakeCaseNamingConvention`) |
| Banco de dados | PostgreSQL 16 (Docker) |
| Autenticação | JWT Bearer (HS256, validade de 2 horas) |
| Tempo real | SignalR (`/hubs/quadro`) |
| Hash de senha | PBKDF2 (`Pbkdf2PasswordHasher`) |
| Documentação da API | OpenAPI + Swagger UI |
| Testes | xUnit |
| Infra local | Docker Compose |

---

## Estrutura da solução

```
<<<<<<< HEAD
MeuEstudos.slnx
├── MeuEstudos.Domain/          Entidades, enums, exceções e contratos de repositório
├── MeuEstudos.Application/     Casos de uso (services), DTOs e interfaces
├── MeuEstudos.Infrastructure/  EF Core, mapeamentos e repositórios
├── MeuEstudos.Api/             Controllers, hub SignalR, middlewares e serviços HTTP
├── MeuEstudos.Tests/           Testes unitários (xUnit) com fakes
=======
MeuEstudo.slnx
├── MeuEstudo.Domain/          Entidades, enums, exceções e contratos de repositório
├── MeuEstudo.Application/     Casos de uso (services), DTOs e interfaces
├── MeuEstudo.Infrastructure/  EF Core, mapeamentos e repositórios
├── MeuEstudo.Api/             Controllers, hub SignalR, middlewares e serviços HTTP
├── MeuEstudo.Tests/           Testes unitários (xUnit) com fakes
>>>>>>> 5895960 (atualizacao para o singular para organizacao)
├── docker-compose.yml          PostgreSQL 16 para desenvolvimento
└── init.sql                    Schema inicial do banco
```

Direção das dependências: `Api → Infrastructure/Application → Domain`.
O domínio não referencia nenhum projeto.

---

## Pré-requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Docker](https://docs.docker.com/get-docker/) com Docker Compose
- (Opcional) Rider, Visual Studio ou VS Code

---

## Como executar

```bash
# 1. Suba o PostgreSQL (cria as tabelas via init.sql no primeiro start)
docker compose up -d

# 2. Restaure as dependências
dotnet restore

# 3. Execute a API
<<<<<<< HEAD
dotnet run --project MeuEstudos.Api
=======
dotnet run --project MeuEstudo.Api
>>>>>>> 5895960 (atualizacao para o singular para organizacao)
```

A API fica disponível em:

- HTTP: `http://localhost:5123`
- HTTPS: `https://localhost:7120` (perfil `https`)
- Swagger UI: `http://localhost:5123/swagger` (apenas em Development)
- OpenAPI JSON: `http://localhost:5123/openapi/v1.json`

> O `init.sql` é executado **somente na criação do volume** do Postgres. Se você
> alterar o schema, recrie o volume:
>
> ```bash
> docker compose down -v && docker compose up -d
> ```

---

## Configuração

As configurações de desenvolvimento ficam em
<<<<<<< HEAD
`MeuEstudos.Api/appsettings.Development.json`:
=======
`MeuEstudo.Api/appsettings.Development.json`:
>>>>>>> 5895960 (atualizacao para o singular para organizacao)

| Chave | Descrição |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | Conexão com o PostgreSQL local |
| `Jwt:Key` | Chave simétrica — **mínimo de 32 caracteres** (validado no startup) |
| `Jwt:Issuer` / `Jwt:Audience` | Emissor e público do token |

O projeto também aceita sobrescrita por variáveis de ambiente, por exemplo:

```bash
export ConnectionStrings__DefaultConnection="Host=...;Database=...;Username=...;Password=..."
export Jwt__Key="sua-chave-de-producao-com-no-minimo-32-caracteres"
```

> ⚠️ A chave JWT e as credenciais presentes no repositório são **exclusivas para
> desenvolvimento local**. Use secrets/variáveis de ambiente em outros ambientes.

O CORS está liberado para o frontend local em `http://localhost:5173` (Vite).

---

## Banco de dados

O schema é criado pelo `init.sql`, que define:

- `usuarios` — dados de perfil e arrays de tópicos de interesse/ajuda;
- `grupos_estudo` — quadros pessoais (`PESSOAL`) ou acadêmicos (`TRABALHO_ACADEMICO`);
- `membros_grupo` — convites e vínculos (`Pendente`, `Aceito`, `Recusado`);
- `colunas_kanban` — colunas do quadro;
- `topicos_estudo` — cards do Kanban (prioridade, responsável, data prevista, conclusão).

Ao criar um grupo, as colunas **A Fazer**, **Em Andamento**, **Revisão** e
**Concluído** são geradas automaticamente. O EF Core usa mapeamentos explícitos
com nomenclatura *snake_case*; não há migrations — o `init.sql` é a fonte de verdade
do schema.

---

## Endpoints

Todas as rotas (exceto as de autenticação) exigem `Authorization: Bearer <token>`.

### Autenticação

| Método | Rota | Descrição |
| --- | --- | --- |
| `POST` | `/api/auth/registrar` | Cadastra usuário e retorna o token JWT |
| `POST` | `/api/auth/login` | Autentica e retorna o token JWT |

### Perfis

| Método | Rota | Descrição |
| --- | --- | --- |
| `GET` | `/api/users/me/profile` | Perfil do usuário autenticado |
| `PUT` | `/api/users/me/profile` | Atualiza o próprio perfil |
| `POST` | `/api/users/me/profile/foto` | Upload da foto (multipart; JPEG, PNG ou WebP, até 2 MB) |
| `GET` | `/api/users/{id}/profile` | Perfil público de outro usuário |

### Grupos e Kanban

| Método | Rota | Descrição |
| --- | --- | --- |
| `POST` | `/api/grupos` | Cria um quadro |
| `GET` | `/api/grupos` | Lista os quadros do usuário |
| `GET` | `/api/grupos/{id}` | Detalhes do quadro com colunas e cards |
| `GET` | `/api/grupos/convites` | Convites pendentes recebidos |
| `POST` | `/api/grupos/{id}/convites` | Convida um usuário por e-mail |
| `PATCH` | `/api/grupos/{id}/convites/responder` | Aceita ou recusa um convite |
| `POST` | `/api/grupos/{id}/topicos` | Adiciona um card |
| `PATCH` | `/api/grupos/{id}/topicos/{topicoId}/concluir` | Alterna a conclusão do card |
| `PATCH` | `/api/topicos/{id}/mover` | Move o card entre colunas/posições |
| `PATCH` | `/api/topicos/{id}/responsavel` | Altera o responsável do card |

### Tempo real (SignalR)

- Hub: `/hubs/quadro` (autenticado por JWT)
- Evento enviado aos membros aceitos do grupo: `TopicoAtualizado`
- Payload: `{ grupoId, topico }`

Como navegadores não enviam o header `Authorization` no handshake do WebSocket, o
token deve ser passado na query string:

```js
new HubConnectionBuilder()
  .withUrl("http://localhost:5123/hubs/quadro", {
    accessTokenFactory: () => token
  })
  .build();
```

### Exemplo rápido

```bash
# Registrar
curl -X POST http://localhost:5123/api/auth/registrar \
  -H "Content-Type: application/json" \
  -d '{"nome":"Arthur","email":"arthur@exemplo.com","senha":"senha123"}'

# Login
curl -X POST http://localhost:5123/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"arthur@exemplo.com","senha":"senha123"}'

# Criar quadro
curl -X POST http://localhost:5123/api/grupos \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"titulo":"Cálculo I","descricao":"Revisão para a prova","tipo":"PESSOAL"}'
```

<<<<<<< HEAD
Há também requisições de exemplo em `MeuEstudos.Api/MeuEstudos.Api.http`.
=======
Há também requisições de exemplo em `MeuEstudo.Api/MeuEstudo.Api.http`.
>>>>>>> 5895960 (atualizacao para o singular para organizacao)

---

## Testes

```bash
dotnet test
```

<<<<<<< HEAD
A suíte (`MeuEstudos.Tests`) cobre as regras de domínio do quadro (colunas, cards,
=======
A suíte (`MeuEstudo.Tests`) cobre as regras de domínio do quadro (colunas, cards,
>>>>>>> 5895960 (atualizacao para o singular para organizacao)
responsáveis, conclusão) e os casos de uso da aplicação, usando implementações
*fake* dos repositórios e do notificador em tempo real.

---

## Tratamento de erros

- Exceções de domínio (`DomainException`) e demais erros são tratados pelo
  `GlobalExceptionHandler`, retornando respostas no padrão **Problem Details**.
<<<<<<< HEAD
- Arquivos enviados ao perfil ficam em `MeuEstudos.Api/wwwroot/uploads/perfis`
=======
- Arquivos enviados ao perfil ficam em `MeuEstudo.Api/wwwroot/uploads/perfis`
>>>>>>> 5895960 (atualizacao para o singular para organizacao)
  (ignorados pelo Git) e são servidos como arquivos estáticos.

---

## Versionamento

O `.gitignore` cobre artefatos de build (`bin/`, `obj/`), configurações de IDE
(`.idea/`, `.vs/`), arquivos de ambiente (`.env`), uploads e pastas locais de
ferramentas/documentação (`.claude/`, `docs/`, `.superpowers/`).
