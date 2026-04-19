# AuthCore Revisado

## Objetivo

Este documento substitui a spec anterior como fonte de verdade do `auth_core`.

A regra principal desta revisão é simples:

- quando a implementação atual estiver aderente ao comportamento originalmente definido, ela deve ser preservada;
- quando a implementação atual divergir do comportamento originalmente definido, a nova spec deve exigir refatoração para o desenho correto;
- o código existente não redefine o produto por conveniência.

O produto alvo continua sendo um núcleo de autenticação pronto para produção com:

- autenticação principal por sessão stateful com Redis e revogação imediata;
- autenticação alternativa por JWT + refresh token stateful para mobile e integrações;
- cadastro com verificação por e-mail via OTP;
- mensageria com outbox e worker;
- hardening mínimo de segurança e operação.

## Princípios

- O fluxo primário do produto é sessão com Redis e TTL.
- O fluxo JWT + refresh é complementar e não substitui o fluxo principal.
- Contrato público tem precedência sobre atalhos de implementação.
- Regras de negócio ficam no domínio.
- Application orquestra casos de uso sem absorver regra central.
- Api adapta HTTP sem se tornar camada de lógica.
- Infrastructure implementa Postgres, Redis, mensageria, envio de e-mail e detalhes técnicos.
- Mudanças que quebram a aderência original devem ser registradas como refatoração planejada, não como desvio aceito.

## Estado Atual do Projeto

### Implementado e Aderente

- solução em camadas com `Api`, `Application`, `Domain` e `Infrastructure`;
- Docker Compose com `api`, `postgres`, `redis` e `rabbitmq`;
- PostgreSQL com `Npgsql` e migrações com `FluentMigrator`;
- domínio de usuário, senha, tentativas de login e refresh token;
- fluxo JWT + refresh token com rotação e detecção de reuse;
- health check de banco;
- testes de domínio, aplicação e integração para partes já existentes.

### Implementado, mas Fora do Contrato Correto

- `POST /api/auth/login` hoje emite `accessToken` e `refreshToken`, mas o contrato correto exige login por sessão com cookie `sid`;
- `GET /api/users/profile` hoje cumpre papel semelhante a perfil autenticado, mas o contrato correto exige `GET /auth/me`;
- o logout atual é centrado em refresh token enviado no body, mas o contrato correto do fluxo principal exige revogação de sessão por cookie;
- o registro atual está em `POST /api/users`, mas o contrato correto exige `POST /auth/register`;
- o estado do usuário hoje é implícito em `IsActive` e `EmailVerifiedAt`, mas o contrato correto exige estado explícito.

### Preparado, mas Ainda Não Fechado

- opções de Redis registradas na configuração;
- opções de RabbitMQ registradas na configuração;
- tabela de `EmailVerificationTokens` já criada;
- estrutura para crescimento em mensageria e e-mail.

### Ainda Não Implementado

- `ISessionStore` e fluxo de sessão Redis com TTL;
- autenticação por cookie `sid`;
- `GET /auth/me`;
- `GET /auth/sessions`, `DELETE /auth/sessions/{sid}` e `POST /auth/logout-all`;
- `POST /auth/token` como endpoint oficial do modo token;
- `POST /auth/register`, `POST /auth/verify-email` e `POST /auth/resend-verification`;
- repositório de verificação de e-mail;
- outbox transacional;
- publisher de outbox;
- worker consumidor;
- `IEmailSender`;
- CSRF hardening;
- rate limiting de login;
- health checks de Redis e RabbitMQ;
- correlação de requisição, logs de segurança e métricas.

## Arquitetura Alvo

### Api

Responsabilidades:

- expor contratos HTTP canônicos;
- ler cookies, headers e payloads;
- resolver contexto autenticado;
- mapear request para command ou query;
- aplicar autenticação, autorização, CSRF e rate limiting;
- serializar responses e erros.

Capacidades esperadas:

- esquema de autenticação por sessão via cookie `sid`;
- esquema JWT Bearer para modo token;
- endpoints canônicos sem depender dos endpoints legados.

### Application

Responsabilidades:

- orquestrar casos de uso;
- consultar repositórios e stores definidos no domínio;
- controlar transações;
- publicar eventos no outbox;
- devolver resultados específicos de aplicação.

### Domain

Responsabilidades:

- modelar agregados e invariantes;
- centralizar contratos;
- representar estado explícito do usuário;
- representar sessão e verificação de e-mail como conceitos de negócio;
- definir contratos de persistência e integração usados pela Application.

### Infrastructure

Responsabilidades:

- implementar repositórios PostgreSQL;
- implementar `SessionStore` em Redis;
- implementar outbox em Postgres;
- implementar publisher e worker;
- implementar envio de e-mail;
- expor health checks e configurações técnicas.

## Modelo de Domínio Alvo

### User

O usuário deve ter estado explícito.

Campos mínimos:

- `Id`
- `UserIdentifier`
- `Email`
- `FirstName`
- `LastName`
- `FullName`
- `Contact`
- `Role`
- `Status`
- `CreatedAt`
- `UpdatedAt`

Estado oficial:

- `PendingEmailVerification`
- `Active`
- `Blocked`

Regras:

- usuário recém-criado entra como `PendingEmailVerification`;
- usuário só autentica quando estiver `Active`;
- usuário `Blocked` não autentica;
- verificação de e-mail promove `PendingEmailVerification` para `Active`;
- mudança de e-mail retorna o usuário para `PendingEmailVerification`.

### Session

Sessão é o conceito principal do fluxo stateful.

Campos mínimos:

- `Sid`
- `UserId`
- `CreatedAtUtc`
- `ExpiresAtUtc`
- `RevokedAtUtc`
- `Ip`
- `UserAgent`
- `LastSeenAtUtc`

Persistência Redis:

- `session:{sid}`
- `user:sessions:{userId}`

Regras:

- `session:{sid}` recebe TTL automático;
- `user:sessions:{userId}` mantém o índice de devices;
- ao listar sessões, qualquer `sid` sem chave correspondente deve ser removido do índice;
- revogação de sessão invalida a próxima request imediatamente;
- o projeto deve suportar TTL fixo e opção de sliding TTL configurável.

### Email Verification

Verificação por e-mail usa OTP.

Campos mínimos:

- `Id`
- `UserId`
- `Email`
- `CodeHash`
- `ExpiresAtUtc`
- `ConsumedAtUtc`
- `RevokedAtUtc`
- `Attempts`
- `MaxAttempts`
- `CooldownUntilUtc`
- `CreatedAtUtc`

Regras:

- o código nunca é persistido em claro;
- a verificação falha quando o código expira;
- a verificação falha quando o limite de tentativas é atingido;
- `resend-verification` respeita cooldown;
- um novo código revoga o anterior ainda ativo.

## Contratos Obrigatórios do Domínio

Interfaces obrigatórias:

- `IUserRepository`
- `IUserReadRepository`
- `IPasswordHasher`
- `IPasswordRepository`
- `ISessionStore`
- `IRefreshTokenRepository`
- `IEmailVerificationRepository`
- `IOutboxRepository`
- `IEmailSender`
- `IAccessTokenGenerator`

Observações de refatoração:

- `IPasswordEncripter` deve ser substituída por `IPasswordHasher` como nome canônico;
- contratos já existentes que forem semanticamente válidos podem ser reaproveitados após alinhamento de nome e responsabilidade.

## Contratos HTTP Canônicos

Os contratos abaixo são a referência pública final.

### Sessão Stateful

#### POST /auth/login

Objetivo:

- autenticar por e-mail e senha;
- criar sessão Redis;
- emitir cookie `sid`.

Request:

```json
{
  "email": "user@x.com",
  "password": "ValidPassword#2026"
}
```

Response 200:

```json
{
  "userId": "guid",
  "email": "user@x.com"
}
```

Efeitos:

- cria `session:{sid}`;
- adiciona `sid` em `user:sessions:{userId}`;
- emite cookie `sid` com `HttpOnly`, `Secure`, `SameSite=Lax`, `Path=/`.

Erros:

- `401` para credenciais inválidas;
- `403` para usuário `PendingEmailVerification` ou `Blocked`.

#### GET /auth/me

Autenticação:

- cookie `sid`.

Response 200:

```json
{
  "userId": "guid",
  "email": "user@x.com"
}
```

Erros:

- `401` para ausência de sessão, sessão inválida, expirada ou revogada.

#### POST /auth/logout

Autenticação:

- cookie `sid`.

Response 204:

- sem body.

Efeitos:

- revoga a sessão Redis;
- remove o cookie `sid`.

#### GET /auth/sessions

Autenticação:

- cookie `sid`.

Response 200:

```json
{
  "currentSid": "string",
  "sessions": [
    {
      "sid": "string",
      "createdAtUtc": "2026-04-18T10:00:00Z",
      "lastSeenAtUtc": "2026-04-18T10:30:00Z",
      "ip": "127.0.0.1",
      "userAgent": "Mozilla/5.0",
      "expiresAtUtc": "2026-04-18T12:00:00Z"
    }
  ]
}
```

#### DELETE /auth/sessions/{sid}

Autenticação:

- cookie `sid`.

Response 204:

- sem body.

Erros:

- `404` quando a sessão não pertence ao usuário autenticado.

#### POST /auth/logout-all

Autenticação:

- cookie `sid`.

Response 204:

- sem body.

Efeitos:

- revoga todas as sessões do usuário;
- remove o cookie atual.

### Modo Token

#### POST /auth/token

Objetivo:

- autenticar clientes API, mobile ou integração;
- emitir `accessToken`;
- emitir `refreshToken`.

Request:

```json
{
  "email": "user@x.com",
  "password": "ValidPassword#2026"
}
```

Response 200:

```json
{
  "accessToken": "jwt",
  "accessTokenExpiresAtUtc": "2026-04-18T11:15:00Z",
  "refreshToken": "opaque-token",
  "refreshTokenExpiresAtUtc": "2026-04-25T11:00:00Z"
}
```

Regras:

- o refresh token pode ser retornado no body para clientes não browser;
- suporte futuro a cookie de refresh pode ser adicionado sem alterar o contrato base do modo API.

#### POST /auth/refresh

Request:

```json
{
  "refreshToken": "opaque-token"
}
```

Response 200:

```json
{
  "accessToken": "jwt",
  "accessTokenExpiresAtUtc": "2026-04-18T11:30:00Z",
  "refreshToken": "new-opaque-token",
  "refreshTokenExpiresAtUtc": "2026-04-25T11:15:00Z"
}
```

Regras:

- deve rotacionar o refresh token;
- deve invalidar reuse;
- deve revogar a família em caso de replay detectado.

#### POST /auth/token/logout

Request:

```json
{
  "refreshToken": "opaque-token"
}
```

Response 204:

- sem body.

#### POST /auth/revoke-tokens

Autenticação:

- reservado para operação administrativa ou de segurança.

Objetivo:

- revogar refresh tokens ativos de um usuário.

### Registro e Verificação de E-mail

#### POST /auth/register

Request:

```json
{
  "email": "user@x.com",
  "password": "ValidPassword#2026"
}
```

Response 201:

```json
{
  "userId": "guid"
}
```

Efeitos:

- cria usuário como `PendingEmailVerification`;
- cria OTP;
- grava mensagem na outbox.

#### POST /auth/verify-email

Request:

```json
{
  "email": "user@x.com",
  "code": "123456"
}
```

Response 204:

- sem body.

#### POST /auth/resend-verification

Request:

```json
{
  "email": "user@x.com"
}
```

Response 204:

- sem body.

## Endpoints Legados

Os contratos abaixo deixam de ser canônicos e passam a ser legados durante a migração:

- `POST /api/auth/login`
- `POST /api/auth/logout`
- `GET /api/users/profile`
- `POST /api/users`

Regra:

- a implementação pode manter compatibilidade temporária;
- a spec não considera esses endpoints como contrato final;
- qualquer compatibilidade deve ser explicitamente marcada como ponte de migração.

## Configuração Oficial

Configurações mínimas obrigatórias:

- `ConnectionStrings__PostgreSql`
- `Redis__ConnectionString`
- `Redis__KeyPrefix`
- `Auth__Session__TtlMinutes`
- `Auth__Session__SlidingTtl`
- `Auth__Cookie__Secure`
- `Auth__Csrf__AllowedOrigins`
- `Authentication__Jwt__Issuer`
- `Authentication__Jwt__Audience`
- `Authentication__Jwt__SigningKey`
- `Authentication__Jwt__AccessTokenLifetimeMinutes`
- `Authentication__Jwt__RefreshTokenLifetimeDays`
- `RabbitMq__Host`
- `RabbitMq__Port`
- `RabbitMq__Username`
- `RabbitMq__Password`
- `RabbitMq__EmailVerificationQueue`

## Segurança e Operação

### CSRF

Regras mínimas:

- endpoints mutáveis autenticados por cookie devem validar `Origin` ou `Referer`;
- a lista de origens permitidas é configurável;
- a base mínima é `SameSite=Lax`, mas isso não substitui validação de origem.

Endpoints cobertos:

- `POST /auth/logout`
- `DELETE /auth/sessions/{sid}`
- `POST /auth/logout-all`

### Rate Limiting

Regras mínimas:

- limitar `POST /auth/login` por IP;
- limitar `POST /auth/login` por e-mail;
- bloquear abuso com resposta explícita;
- registrar ocorrência de limitação.

### Observabilidade

Obrigatório:

- correlation id por request;
- logs estruturados por requisição;
- logs de tentativa de login falha;
- logs de revogação de sessão;
- logs de revogação de refresh token;
- logs de reuse detectado;
- métricas de login, refresh, logout e latência Redis.

### Health Checks

Obrigatório:

- health check de Postgres;
- health check de Redis;
- health check de RabbitMQ ou do worker quando aplicável.

## Tarefas de Entrega

### Tarefa 0

Objetivo:

- consolidar baseline técnico e configuração.

Entrega:

- compose funcional;
- config por environment;
- health check de Postgres, Redis e RabbitMQ;
- documentação dos contratos finais e legados.

### Tarefa 1

Objetivo:

- entregar o fluxo principal correto de sessão com Redis.

Entrega:

- `POST /auth/login`
- `GET /auth/me`
- `POST /auth/logout`
- `GET /auth/sessions`
- `DELETE /auth/sessions/{sid}`
- `POST /auth/logout-all`

### Tarefa 2

Objetivo:

- reposicionar o modo JWT + refresh sob contrato correto.

Entrega:

- `POST /auth/token`
- `POST /auth/refresh`
- `POST /auth/token/logout`
- `POST /auth/revoke-tokens`

### Tarefa 3

Objetivo:

- entregar registro e verificação de e-mail ponta a ponta.

Entrega:

- `POST /auth/register`
- `POST /auth/verify-email`
- `POST /auth/resend-verification`
- OTP persistido;
- outbox;
- publisher;
- worker;
- envio de e-mail.

### Tarefa 4

Objetivo:

- concluir hardening mínimo de segurança e operação.

Entrega:

- CSRF;
- rate limiting;
- observabilidade;
- telemetria de segurança.

## Backlog Executável

### Épico 1. Correção de Contrato Público

#### Feature 1.1. Realinhar autenticação por sessão como fluxo principal

Objetivo:

- substituir o contrato atual de login token-based pelo login stateful correto.

Dependências:

- modelagem de sessão;
- store Redis;
- autenticação por cookie.

Tasks:

- criar contracts `RequestSessionLoginJson` e `ResponseSessionUserJson`;
- redefinir `POST /auth/login` como login por sessão;
- criar `GET /auth/me`;
- redefinir `POST /auth/logout` para sessão por cookie;
- marcar `POST /api/auth/login` como legado;
- marcar `POST /api/auth/logout` como legado.

Critérios de aceite:

- login cria cookie `sid`;
- `GET /auth/me` autentica com cookie;
- logout invalida a próxima request.

Marco funcional:

- usuário autentica com cookie e consulta `/auth/me`.

#### Feature 1.2. Separar explicitamente o modo token

Objetivo:

- mover o fluxo JWT + refresh para contrato próprio.

Dependências:

- reaproveitamento dos casos de uso já existentes;
- ajuste de rotas e contratos.

Tasks:

- criar `POST /auth/token`;
- adaptar response token-based para contrato canônico;
- manter `POST /auth/refresh`;
- criar `POST /auth/token/logout`;
- definir `POST /auth/revoke-tokens`;
- marcar `GET /api/users/profile` como legado em relação a `GET /auth/me`.

Critérios de aceite:

- cliente API obtém token por endpoint dedicado;
- refresh continua rotacionando corretamente;
- logout token-based revoga refresh.

Marco funcional:

- cliente API obtém `accessToken` e renova com `refresh`.

### Épico 2. Sessão Redis com TTL

#### Feature 2.1. Contratos e modelo de sessão

Objetivo:

- consolidar o conceito de sessão no domínio.

Tasks:

- criar `SessionRecord` ou tipo equivalente;
- criar `ISessionStore`;
- definir payload e serialização da sessão;
- definir política de TTL fixo;
- definir política opcional de sliding TTL.

Critérios de aceite:

- domínio e aplicação conseguem depender apenas do contrato do store.

Marco funcional:

- store e aplicação falam a mesma linguagem de sessão.

#### Feature 2.2. Infraestrutura Redis

Objetivo:

- implementar persistência real das sessões.

Tasks:

- adicionar dependência de Redis ao projeto;
- implementar `RedisSessionStore`;
- persistir `session:{sid}`;
- persistir `user:sessions:{userId}`;
- implementar limpeza de órfãos;
- registrar store na DI.

Critérios de aceite:

- criar, obter, revogar, listar e revogar todas as sessões.

Marco funcional:

- Redis passa a sustentar autenticação stateful real.

#### Feature 2.3. Autenticação baseada em cookie

Objetivo:

- autenticar requests via `sid`.

Tasks:

- criar auth handler de sessão;
- ler cookie `sid`;
- consultar `ISessionStore`;
- carregar usuário autenticado;
- montar claims mínimas;
- aplicar sliding TTL quando habilitado.

Critérios de aceite:

- endpoint protegido autentica apenas com `sid` válido.

Marco funcional:

- request autenticada depende de Redis e não de JWT.

#### Feature 2.4. Sessões por dispositivo

Objetivo:

- permitir visibilidade e revogação granular de devices.

Tasks:

- implementar `GET /auth/sessions`;
- implementar `DELETE /auth/sessions/{sid}`;
- implementar `POST /auth/logout-all`;
- validar propriedade da sessão antes de revogar;
- limpar cookie atual ao revogar sessão ativa.

Critérios de aceite:

- usuário lista devices e revoga um específico;
- `logout-all` encerra todas as sessões.

Marco funcional:

- usuário vê devices e revoga sessão específica imediatamente.

### Épico 3. Realinhamento do Domínio de Usuário

#### Feature 3.1. Consolidar status explícito

Objetivo:

- abandonar estado implícito e adotar `UserStatus`.

Tasks:

- criar enum `UserStatus`;
- atualizar agregado `User`;
- revisar `CanSignIn`;
- atualizar persistência de usuário;
- criar migração para coluna de status;
- migrar dados existentes para status consistente.

Critérios de aceite:

- usuário possui estado explícito persistido;
- comportamento de autenticação deriva do estado oficial.

Marco funcional:

- domínio de usuário fica aderente ao contrato do produto.

#### Feature 3.2. Ajustar regras de login

Objetivo:

- alinhar códigos de erro e semântica de autenticação.

Tasks:

- retornar `403` para `PendingEmailVerification`;
- retornar `403` para `Blocked`;
- manter `401` para credenciais inválidas;
- revisar exceções de domínio e mapping HTTP;
- atualizar testes HTTP e de aplicação.

Critérios de aceite:

- API diferencia corretamente credencial inválida de usuário impedido.

Marco funcional:

- erros de autenticação refletem o contrato correto.

### Épico 4. JWT + Refresh Token

#### Feature 4.1. Consolidar fluxo token-based aderente à spec

Objetivo:

- reutilizar o que já existe sob contrato correto.

Tasks:

- separar o caso de uso atual de login token-based do login por sessão;
- adaptar endpoint para `POST /auth/token`;
- revisar contratos `Request...Json` e `Response...Json`;
- manter rotação e reuse detection;
- alinhar documentação e testes.

Critérios de aceite:

- JWT continua funcional após separação dos dois fluxos.

Marco funcional:

- fluxo JWT permanece funcional e aderente ao contrato revisado.

#### Feature 4.2. Revogação e administração

Objetivo:

- centralizar revogação e segurança do modo token.

Tasks:

- criar `POST /auth/token/logout`;
- criar `POST /auth/revoke-tokens`;
- revisar revogação em troca de senha;
- revisar revogação em exclusão de usuário.

Critérios de aceite:

- refresh tokens ativos podem ser revogados por logout e por operação administrativa.

Marco funcional:

- superfície de revogação do modo token fica completa.

### Épico 5. Cadastro e Verificação de E-mail

#### Feature 5.1. Registro alinhado à spec

Objetivo:

- reposicionar o cadastro sob o módulo de autenticação.

Tasks:

- criar `POST /auth/register`;
- registrar usuário como `PendingEmailVerification`;
- gerar OTP;
- persistir hash do código;
- devolver `201` com `userId`;
- marcar `POST /api/users` como legado ou compatibilidade temporária.

Critérios de aceite:

- usuário recém-criado não autentica até verificar o e-mail.

Marco funcional:

- registro oficial passa a existir sob contrato correto.

#### Feature 5.2. Verificação e reenvio

Objetivo:

- concluir ativação do usuário por OTP.

Tasks:

- implementar `POST /auth/verify-email`;
- implementar `POST /auth/resend-verification`;
- validar expiração;
- validar tentativas máximas;
- validar cooldown;
- ativar usuário após OTP válido;
- revogar OTP anterior ao emitir novo.

Critérios de aceite:

- OTP expira;
- tentativas são limitadas;
- resend respeita cooldown.

Marco funcional:

- usuário recém-cadastrado consegue ativar a conta.

#### Feature 5.3. Persistência de verificação

Objetivo:

- alinhar a tabela atual à necessidade real do fluxo.

Tasks:

- revisar `EmailVerificationTokens`;
- incluir `Attempts`;
- incluir `MaxAttempts`;
- incluir `CooldownUntilUtc`;
- criar `IEmailVerificationRepository`;
- implementar repositório PostgreSQL.

Critérios de aceite:

- persistência suporta todo o ciclo do OTP.

Marco funcional:

- fluxo de verificação deixa de depender de estrutura parcial.

### Épico 6. Outbox, Mensageria e Worker

#### Feature 6.1. Outbox

Objetivo:

- garantir publicação resiliente do evento de verificação.

Tasks:

- criar tabela `OutboxMessages`;
- criar `IOutboxRepository`;
- persistir `EmailVerificationRequested` na mesma transação do registro;
- definir formato de payload do evento;
- registrar metadata mínima do evento.

Critérios de aceite:

- registro do usuário grava a mensagem de outbox na mesma unidade transacional.

Marco funcional:

- emissão de evento passa a ser confiável.

#### Feature 6.2. Publicação e worker

Objetivo:

- enviar e-mail a partir do outbox.

Tasks:

- criar publisher da outbox;
- integrar RabbitMQ;
- criar worker consumidor;
- criar `IEmailSender`;
- implementar sender inicial;
- registrar retry mínimo;
- registrar falhas operacionais.

Critérios de aceite:

- registro produz evento;
- evento é publicado;
- worker tenta envio do e-mail.

Marco funcional:

- registro gera evento, evento é publicado e worker tenta envio do e-mail.

### Épico 7. Hardening e Segurança

#### Feature 7.1. CSRF

Objetivo:

- proteger endpoints mutáveis autenticados por cookie.

Tasks:

- definir `AllowedOrigins`;
- validar `Origin` ou `Referer`;
- aplicar proteção em logout, revoke e logout-all;
- documentar comportamento por ambiente.

Critérios de aceite:

- mutações por cookie rejeitam origem inválida.

Marco funcional:

- endpoints por sessão ficam protegidos contra CSRF básico.

#### Feature 7.2. Rate limiting

Objetivo:

- reduzir abuso no login.

Tasks:

- limitar login por IP;
- limitar login por e-mail;
- devolver resposta padronizada;
- registrar eventos de limitação.

Critérios de aceite:

- abuso repetitivo do login é contido.

Marco funcional:

- login fica protegido contra abuso básico.

#### Feature 7.3. Cookies e transporte

Objetivo:

- formalizar atributos de segurança dos cookies.

Tasks:

- padronizar `HttpOnly`;
- padronizar `Secure`;
- padronizar `SameSite=Lax`;
- definir comportamento por ambiente de execução.

Critérios de aceite:

- cookies emitidos seguem política consistente.

Marco funcional:

- sessão passa a operar com política segura e previsível.

### Épico 8. Observabilidade e Operação

#### Feature 8.1. Health checks

Objetivo:

- refletir a saúde real das dependências.

Tasks:

- manter health check de Postgres;
- adicionar health check de Redis;
- adicionar health check de RabbitMQ ou worker.

Critérios de aceite:

- `/health` reflete dependências críticas do produto.

Marco funcional:

- operação mínima do ambiente fica observável.

#### Feature 8.2. Logs e métricas

Objetivo:

- suportar rastreio e resposta operacional.

Tasks:

- adicionar correlation id;
- registrar logs de login falho;
- registrar logs de revogação;
- registrar logs de reuse detectado;
- expor métricas de login, refresh, logout e latência Redis.

Critérios de aceite:

- operações sensíveis deixam rastro operacional suficiente.

Marco funcional:

- operação mínima observável em produção.

## Estratégia de Migração

### Etapa 1

- introduzir contratos canônicos sem remover imediatamente os legados;
- documentar rotas legadas como ponte temporária;
- manter testes existentes enquanto novos contratos são adicionados.

### Etapa 2

- concluir o fluxo principal de sessão Redis;
- migrar consumidores internos e testes para `POST /auth/login`, `GET /auth/me` e `POST /auth/logout`.

### Etapa 3

- mover o fluxo JWT para `POST /auth/token`;
- ajustar documentação e testes para o novo contrato.

### Etapa 4

- introduzir registro, OTP, outbox e worker;
- migrar o cadastro principal para `POST /auth/register`.

### Etapa 5

- remover dependência funcional dos endpoints legados;
- manter compatibilidade apenas se houver consumidor explícito;
- caso não haja necessidade, descontinuar endpoints legados após cobertura de teste equivalente.

## Testes Obrigatórios

### Domínio

- `UserStatus` e `CanSignIn`;
- criação, ativação e bloqueio de usuário;
- regras de sessão;
- regras de OTP;
- cooldown e limite de tentativas.

### Application

- login por sessão;
- login por token;
- refresh com rotação;
- verify-email;
- resend-verification;
- logout-all;
- revoke-tokens.

### Integração

- Redis session create, get, revoke, list e revoke-all;
- refresh token persistence e revogação;
- email verification persistence;
- outbox persistence;
- publisher e worker.

### E2E

- `login sessão -> me -> logout -> me(401)`;
- `login sessão -> sessions -> revoke sid -> me(401)`;
- `token -> refresh -> refresh antigo inválido`;
- `register -> login(403) -> verify-email -> login(200)`.

### Segurança

- CSRF para endpoints mutáveis por cookie;
- rate limiting de login;
- rejeição de sessão revogada;
- rejeição de refresh token reutilizado.

## Definição de Conclusão

Uma feature só pode ser considerada concluída quando:

- o contrato HTTP canônico estiver implementado;
- o comportamento estiver aderente a este documento;
- os testes relevantes da camada alterada estiverem criados ou atualizados;
- a implementação não depender do endpoint legado para funcionar;
- a documentação do contrato estiver coerente com o código.
