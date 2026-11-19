# MeuEstudo API

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
| Autenticação | JWT Bearer (HS256, validade configurável, padrão de 2 horas) |
| Tempo real | SignalR (`/hubs/quadro`) |
| Hash de senha | PBKDF2-HMAC-SHA256 (600.000 iterações, rehash transparente no login) |
| Documentação da API | OpenAPI + Swagger UI (somente em Development) |
| Testes | xUnit + testes de integração (`WebApplicationFactory`) |
| Infra local | Docker Compose |

---

## Estrutura da solução

```
MeuEstudo.slnx
├── MeuEstudo.Domain/          Entidades, enums, exceções e contratos de repositório
├── MeuEstudo.Application/     Casos de uso (services), DTOs e interfaces
├── MeuEstudo.Infrastructure/  EF Core, mapeamentos e repositórios
├── MeuEstudo.Api/             Controllers, hub SignalR, middlewares e serviços HTTP
├── MeuEstudo.Tests/           Testes unitários, de autorização e de integração
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
# 1. Crie o arquivo de ambiente local (NUNCA versione o .env)
cp .env.example .env

# 2. Gere segredos fortes e cole no .env
openssl rand -base64 32 | tr -dc 'A-Za-z0-9'   # POSTGRES_PASSWORD
openssl rand -base64 64                        # Jwt__Key

# 3. Suba o PostgreSQL (cria as tabelas via init.sql no primeiro start)
docker compose up -d

# 4. Carregue as variáveis no shell e execute a API
set -a; source .env; set +a
dotnet restore
dotnet run --project MeuEstudo.Api
```

A API fica disponível em:

- HTTP: `http://localhost:5123`
- HTTPS: `https://localhost:7120` (perfil `https`)
- Swagger UI: `http://localhost:5123/swagger` (apenas em Development)
- OpenAPI JSON: `http://localhost:5123/openapi/v1.json`

> O `init.sql` é executado **somente na criação do volume** do Postgres. Se você
> alterar o schema, recrie o volume com `docker compose down -v && docker compose up -d`.

### API em container (opcional)

```bash
docker compose --profile app up -d --build
```

O profile `api` usa `ASPNETCORE_ENVIRONMENT=Development` e serve **apenas para
desenvolvimento local**. Não use essa configuração em produção.

---

## Configuração e segredos

- `MeuEstudo.Api/appsettings.json` contém apenas configurações **não secretas**
  (`Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiresMinutes`, origens de CORS e logging).
- `MeuEstudo.Api/appsettings.Development.json` não contém senhas nem chaves.
- Segredos vêm de **variáveis de ambiente** (ou do secret manager da plataforma).
- O `.env` é ignorado pelo Git. O `.env.example` documenta as variáveis sem valores reais.

### Variáveis necessárias

| Variável | Uso |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | Conexão com o PostgreSQL |
| `Jwt__Key` | Chave simétrica HS256 com **no mínimo 32 bytes** |
| `Jwt__Issuer` | Emissor (padrão `MeuEstudo.Api`) |
| `Jwt__Audience` | Público (padrão `MeuEstudo.Frontend`) |
| `Jwt__ExpiresMinutes` | Validade do token (5–1440; padrão 120) |
| `Cors__AllowedOrigins__0`... | Origens do frontend, uma por índice |
| `ASPNETCORE_ENVIRONMENT` | `Production` em produção |

Para o Docker Compose, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB` e
`Jwt__Key` também são lidos do `.env`.

A API **recusa iniciar** se:

- `Jwt:Key` estiver ausente ou tiver menos de 32 bytes;
- `Jwt:Issuer`/`Jwt:Audience` estiverem vazios;
- a chave JWT ou a senha do PostgreSQL corresponderem às credenciais que foram
  expostas no histórico público deste repositório;
- `Cors:AllowedOrigins` contiver `*` ou uma origem inválida.

### Produção (Render e similares)

Configure as variáveis acima no painel de secrets da plataforma:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Host=...;Port=5432;Database=...;Username=...;Password=...;SSL Mode=Require
Jwt__Key=<chave nova, aleatória, exclusiva de produção>
Jwt__Issuer=MeuEstudo.Api
Jwt__Audience=MeuEstudo.Frontend
Cors__AllowedOrigins__0=https://meu-estudo-frontend.vercel.app
```

Recomendações:

- Use um usuário de banco **dedicado à aplicação**, sem superuser/createdb e sem
  permissão de DDL em produção.
- Exija TLS na conexão com o Postgres (`SSL Mode=Require` ou superior).
- Mantenha Swagger desabilitado (`ASPNETCORE_ENVIRONMENT=Production`).
- Restrinja `AllowedHosts` ao domínio da API, se a plataforma permitir.

---

## Segurança implementada

### Autenticação e JWT

- Assinatura HS256 com chave exclusiva por ambiente (mínimo de 32 bytes).
- Validação de assinatura, algoritmo (`ValidAlgorithms = [HS256]`), emissor,
  audiência, expiração e `notBefore`.
- Tokens incluem `jti` para rastreabilidade; e-mail e hash de senha **não** entram no JWT.
- Senhas usam PBKDF2-HMAC-SHA256 com 600.000 iterações; hashes antigos são
  atualizados automaticamente após um login bem-sucedido.
- Mensagens de login são idênticas para e-mail inexistente e senha incorreta, e
  um hash descartável é executado para reduzir a diferença de tempo.
- **Limitação:** não há refresh token nem revogação server-side; o logout é local.
  Um token roubado continua válido até expirar (padrão: 2 horas). Para revogação
  imediata, seria necessário um store de tokens/refresh tokens (Redis/Postgres).

**Avaliação de cookies HttpOnly:** o SPA está em `vercel.app` e a API em
`onrender.com` (sites diferentes). Cookies `SameSite=None` nesse cenário são
bloqueados por Safari/ITP e pela eliminação de cookies de terceiros no Chrome, o
que quebraria o login para parte dos usuários. A migração não foi feita porque o
deploy atual é cross-site. Ela é recomendada quando frontend e API estiverem no
mesmo site (ex.: `app.exemplo.com` + `api.exemplo.com` com domínio pai comum),
junto com CSRF token, CORS com credenciais e refresh token rotativo. Enquanto
isso, o token fica em `localStorage` e o risco de XSS é mitigado por CSP,
sanitização de URLs e ausência de `dangerouslySetInnerHTML`.

### Autorização (IDOR / broken access control)

Toda decisão é tomada no **backend**, a partir do usuário do token — nunca de IDs
enviados no corpo:

- somente o dono convida, remove membros e exclui o quadro;
- apenas dono ou membro com convite **aceito** lê/edita o quadro;
- convite só pode ser respondido pelo destinatário;
- responsável de um card precisa pertencer ao grupo;
- exclusão de publicação só pelo autor;
- perfil só pode ser alterado pelo próprio usuário;
- uploads são gravados no diretório do usuário autenticado.

Os testes `*AutorizacaoTests` cobrem usuário A acessando/alterando dados de B,
membro comum tentando ação de dono, convite aceito por terceiro e acesso por quem
não participa do grupo.

### Rate limiting

| Política | Limite | Chave |
| --- | --- | --- |
| Global | 600 requisições/min | usuário ou IP |
| `auth` (login/cadastro) | 10 requisições/min | IP |
| `convite` | 20 convites/10 min | usuário ou IP |
| `upload` | 20 uploads/10 min | usuário ou IP |

Os limites são **em memória por instância**. Em múltiplas réplicas, use um store
distribuído (ex.: Redis) para um limite global. Não há bloqueio permanente de
conta, evitando que um atacante cause negação de serviço contra usuários legítimos.

### Uploads

- Limite de tamanho: 2 MB para foto, 10 MB para anexo (e limite global de corpo de 12 MB).
- Validação real do conteúdo: assinatura de PDF, PNG, JPEG, WebP e OLE, estrutura
  OOXML (`[Content_Types].xml` + `word/`/`ppt/`) e texto UTF-8 sem bytes binários.
- Extensões executáveis/ativas e `text/html`, `image/svg+xml` são bloqueadas.
- Nome do arquivo é um GUID aleatório; o nome original é apenas exibido.
- Remoção valida o caminho resolvido e nunca apaga fora do diretório de uploads.
- Respostas usam `X-Content-Type-Options: nosniff`; documentos são servidos com
  `Content-Disposition: attachment`; imagens ficam inline.
- Fotos e anexos são servidos como arquivos estáticos a partir de URLs com GUID
  (funcionam como capability URLs). Elas não exigem autenticação para download.

### Cabeçalhos, CORS e proxy

- `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'; ...`
- `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`.
- `Permissions-Policy` restritivo e `Cross-Origin-Opener-Policy: same-origin`.
- `Strict-Transport-Security` em HTTPS nos ambientes Production/Staging.
- `Cache-Control: no-store` em todas as respostas de `/api`.
- CORS com origens explícitas e credenciais; `*` é rejeitado no startup.
- `ForwardedHeaders` interpreta `X-Forwarded-For/Proto` do proxy (Render). Em
  ambientes onde a API é exposta diretamente, restrinja `KnownProxies/KnownIPNetworks`.

### SignalR

- Hub `QuadroHub` exige `[Authorize]` e valida que o token identifica um usuário.
- O cliente **não** escolhe grupos: o servidor envia apenas para dono e membros
  aceitos (`Clients.Users`), derivados do banco.
- O token em query string é aceito **somente** em `/hubs/quadro` (necessário para
  WebSocket no browser). Evite registrar a query string em logs/proxies.
- `CloseOnAuthenticationExpiration` derruba conexões quando o token expira.

### Banco de dados

- Todas as consultas usam LINQ/EF Core parametrizado; não há SQL concatenado.
- Erros internos são logados e devolvidos ao cliente como mensagem genérica.
- Comandos SQL do EF Core ficam em nível `Warning` para reduzir exposição em logs.
- Recomenda-se usuário de banco com privilégio mínimo e backups criptografados.

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
| `POST` | `/api/users/me/profile/foto` | Upload da foto (JPEG, PNG ou WebP, até 2 MB) |
| `GET` | `/api/users` | Diretório de perfis públicos |
| `GET` | `/api/users/{id}/profile` | Perfil público de outro usuário |

### Grupos e Kanban

| Método | Rota | Descrição |
| --- | --- | --- |
| `POST` | `/api/grupos` | Cria um quadro |
| `GET` | `/api/grupos` | Lista os quadros do usuário |
| `GET` | `/api/grupos/{id}` | Detalhes do quadro com colunas e cards |
| `GET` | `/api/grupos/convites` | Convites pendentes recebidos |
| `POST` | `/api/grupos/{id}/convites` | Convida um usuário por e-mail (dono) |
| `PATCH` | `/api/grupos/{id}/convites/responder` | Aceita ou recusa o próprio convite |
| `DELETE` | `/api/grupos/{id}/membros/{membroId}` | Remove membro/convite (dono) |
| `DELETE` | `/api/grupos/{id}` | Exclui o quadro (dono) |
| `POST` | `/api/grupos/{id}/topicos` | Adiciona um card |
| `PATCH` | `/api/grupos/{id}/topicos/{topicoId}/concluir` | Alterna a conclusão |
| `PATCH` | `/api/topicos/{id}/mover` | Move o card entre colunas/posições |
| `PATCH` | `/api/topicos/{id}/responsavel` | Altera o responsável do card |

### Comunidade

| Método | Rota | Descrição |
| --- | --- | --- |
| `GET` | `/api/publicacoes` | Feed da comunidade |
| `POST` | `/api/publicacoes` | Cria publicação (multipart, anexo opcional) |
| `PATCH` | `/api/publicacoes/{id}/curtida` | Curte/descurte |
| `POST` | `/api/publicacoes/{id}/comentarios` | Comenta |
| `DELETE` | `/api/publicacoes/{id}` | Exclui a própria publicação |

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

---

## Testes

```bash
dotnet test
```

A suíte cobre:

- regras de domínio do Kanban (colunas, cards, responsáveis, conclusão);
- **autorização/IDOR**: usuário A não acessa nem altera dados de B, membro comum
  não executa ações de dono, convite não pode ser aceito por terceiros;
- **JWT**: token válido, expirado, assinatura incorreta, emissor/audiência errados,
  algoritmo diferente de HS256 e token sem assinatura;
- **uploads**: extensão executável, conteúdo disfarçado, OOXML inválido, limite de
  tamanho, nome aleatório e path traversal na remoção;
- **integração HTTP**: 401 sem token/token expirado/assinatura errada, negociação
  SignalR sem token, cabeçalhos de segurança, CORS e 429 após excesso de logins.

---

## Rotação de credenciais expostas (ação manual obrigatória)

A antiga chave JWT de desenvolvimento e a senha do PostgreSQL ficaram expostas no
histórico público deste repositório. **Remover do código não invalida a credencial.**
Antes de considerar o incidente encerrado:

1. Gere uma nova `Jwt__Key` forte e exclusiva para **cada** ambiente e atualize o
   secret manager/Render. Tokens emitidos com a chave antiga continuam válidos até
   expirar; a rotação os invalida para novas emissões, mas não revoga os existentes.
2. Troque a senha do usuário do PostgreSQL em todos os ambientes e atualize a
   connection string.
3. Verifique se a chave/senha antigas foram reutilizadas em outros serviços,
   bancos, scripts ou ambientes.
4. Audite logs de acesso do banco e da API desde a publicação do segredo.
5. Considere limpar o histórico Git (git filter-repo/BFG). Isso exige
   `force-push`, reescrita de histórico e coordenação com todos os clones —
   **não foi feito** sem autorização explícita.
6. O código agora recusa iniciar com a chave/senha antigas (comparação por hash).

---

## Limitações e pendências conhecidas

- Token no `localStorage` (ver avaliação de cookies acima) e ausência de refresh/revogação.
- Não há MFA nem verificação de e-mail. O cadastro informa quando um e-mail já existe
  (enumeração de contas); o rate limit reduz, mas não elimina, esse vetor.
- Rate limit é por IP/usuário e em memória por instância. Ataques distribuídos
  (botnets/proxies) exigem WAF/Cloudflare e, idealmente, MFA.
- Anexos e fotos em `wwwroot/uploads` são servidos por URL estática com GUID; não há
  autorização por requisição no download.
- A validação de anexos é estrutural (magic bytes/OOXML/texto), não há antivírus ou
  sandbox. Para ambientes de alto risco, integre um scanner (ex.: ClamAV) e/ou
  armazenamento de objetos com URLs assinadas.
- A política de senha exige apenas 8 caracteres; não há checagem de senhas vazadas
  (HIBP), MFA ou limite de senhas comuns. Recomenda-se elevar para 12+ e oferecer MFA.
- Não há endpoint de troca/recuperação de senha; se adicionado, exige tokens de uso
  único, expiração curta e rate limiting próprio. Enquanto não existir, uma senha
  comprometida só pode ser trocada diretamente no banco (com novo hash PBKDF2).
- Rate limit em memória por instância; em escala horizontal, migrar para store distribuído.
- A CSP do frontend (`vercel.json`) aponta para o domínio atual da API. Ao trocar
  `VITE_API_URL`, atualize `connect-src`/`img-src`.
- O token do SignalR viaja na query string e pode aparecer em logs de proxy; mantenha
  o acesso à API apenas pelo proxy/plataforma e evite logar query strings.
- Configuração real de produção (Render/Postgres) não é verificável a partir do
  repositório; revise secrets, TLS e permissões do banco no painel.

---

## Versionamento

O `.gitignore` cobre artefatos de build (`bin/`, `obj/`), configurações de IDE
(`.idea/`, `.vs/`), arquivos de ambiente (`.env`), uploads e pastas locais de
ferramentas/documentação (`.claude/`, `docs/`, `.superpowers/`).
