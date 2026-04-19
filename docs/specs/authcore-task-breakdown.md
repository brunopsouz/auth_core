# Tarefas derivadas da authcore-revised-spec

Para detalhes de regra, contrato e contexto, consultar [authcore-revised-spec.md](./authcore-revised-spec.md).

Revisei [authcore-revised-spec.md](./authcore-revised-spec.md). A spec já traz épicos e Tarefas; abaixo está uma divisão mais operacional em tarefas, na ordem mais segura de execução.

## Tarefa 0

1. Consolidar contratos canônicos e marcar contratos legados.
- Documentar `POST /auth/login`, `GET /auth/me`, `POST /auth/logout`, `POST /auth/token`, `POST /auth/register` e demais endpoints finais.
- Marcar `POST /api/auth/login`, `POST /api/auth/logout`, `GET /api/users/profile` e `POST /api/users` como ponte de migração.
2. Fechar baseline de configuração.
- Validar `Redis`, `RabbitMQ`, `JWT`, `Cookie`, `CSRF` e `Session` no configuration binding.
- Padronizar variáveis obrigatórias por ambiente.
3. Completar health checks básicos.
- Manter Postgres.
- Adicionar Redis.
- Adicionar RabbitMQ ou health do worker.

## Tarefa 1

4. Modelar sessão no domínio.
- Criar tipo de sessão (`SessionRecord` ou equivalente).
- Criar `ISessionStore`.
- Definir payload, serialização, TTL fixo e sliding TTL opcional.
5. Implementar `RedisSessionStore`.
- Persistir `session:{sid}` com TTL.
- Persistir índice `user:sessions:{userId}`.
- Implementar get, list, revoke, revoke-all e limpeza de órfãos.
6. Criar autenticação por cookie `sid`.
- Implementar auth handler.
- Ler cookie `sid`.
- Consultar `ISessionStore`.
- Carregar usuário e claims mínimas.
7. Entregar login stateful.
- Criar `RequestSessionLoginJson` e `ResponseSessionUserJson`.
- Redefinir `POST /auth/login` para criar sessão Redis e emitir cookie seguro.
- Ajustar respostas `401` e `403`.
8. Entregar leitura da sessão autenticada.
- Criar `GET /auth/me`.
- Responder com `userId` e `email`.
9. Entregar logout por sessão.
- Redefinir `POST /auth/logout` para revogar sessão via cookie.
- Remover cookie `sid`.
10. Entregar gestão de sessões por dispositivo.
- Criar `GET /auth/sessions`.
- Criar `DELETE /auth/sessions/{sid}`.
- Criar `POST /auth/logout-all`.
- Validar propriedade da sessão antes de revogar.

## Tarefa 1.5

11. Realinhar domínio do usuário para estado explícito.
- Criar `UserStatus`.
- Atualizar agregado `User` e `CanSignIn`.
- Substituir dependência de `IsActive` e `EmailVerifiedAt` como regra implícita.
12. Atualizar persistência e migração do usuário.
- Adicionar coluna de status.
- Migrar dados existentes para estado consistente.
13. Revisar mapping de erros de autenticação.
- `401` para credencial inválida.
- `403` para `PendingEmailVerification` e `Blocked`.

## Tarefa 2

14. Separar o fluxo token do fluxo de sessão.
- Desacoplar o caso de uso atual de login token-based do novo login stateful.
- Criar `POST /auth/token`.
- Ajustar request/response canônicos do modo token.
15. Preservar refresh token aderente.
- Manter `POST /auth/refresh`.
- Garantir rotação, reuse detection e revogação da família em replay.
16. Completar revogação do modo token.
- Criar `POST /auth/token/logout`.
- Criar `POST /auth/revoke-tokens`.
- Revisar revogação em troca de senha e exclusão de usuário.

## Tarefa 3

17. Reposicionar registro sob autenticação.
- Criar `POST /auth/register`.
- Registrar usuário como `PendingEmailVerification`.
- Retornar `201` com `userId`.
18. Fechar modelo de verificação por e-mail.
- Revisar `EmailVerificationTokens`.
- Adicionar `Attempts`, `MaxAttempts` e `CooldownUntilUtc`.
- Criar `IEmailVerificationRepository`.
19. Implementar OTP ponta a ponta.
- Gerar código.
- Persistir apenas `CodeHash`.
- Revogar código anterior ativo ao reenviar.
- Validar expiração, tentativas e cooldown.
20. Entregar verificação e reenvio.
- Criar `POST /auth/verify-email`.
- Criar `POST /auth/resend-verification`.
- Promover usuário para `Active` após OTP válido.

## Tarefa 3.5

21. Implementar outbox transacional.
- Criar tabela `OutboxMessages`.
- Criar `IOutboxRepository`.
- Persistir `EmailVerificationRequested` na mesma transação do registro.
22. Implementar publicação e consumo.
- Criar publisher do outbox.
- Integrar RabbitMQ.
- Criar worker consumidor.
23. Implementar envio de e-mail.
- Criar `IEmailSender`.
- Implementar sender inicial.
- Adicionar retry mínimo e logs de falha.

## Tarefa 4

24. Aplicar CSRF nos endpoints autenticados por cookie.
- Validar `Origin` ou `Referer`.
- Configurar `AllowedOrigins`.
- Cobrir `POST /auth/logout`, `DELETE /auth/sessions/{sid}` e `POST /auth/logout-all`.
25. Aplicar rate limiting no login.
- Limitar por IP.
- Limitar por e-mail.
- Padronizar resposta de bloqueio e logar ocorrência.
26. Formalizar política de cookies.
- Garantir `HttpOnly`, `Secure`, `SameSite=Lax` e `Path=/`.
- Ajustar comportamento por ambiente.
27. Completar observabilidade.
- Correlation id por request.
- Logs estruturados.
- Logs de login falho, revogação de sessão, revogação de refresh e reuse detectado.
- Métricas de login, refresh, logout e latência Redis.

## Testes obrigatórios por etapa

28. Domínio.
- `UserStatus`, `CanSignIn`, regras de sessão, OTP, cooldown e tentativas.
29. Application.
- Login por sessão, login por token, refresh, verify-email, resend-verification, logout-all e revoke-tokens.
30. Integração.
- Redis session store, refresh tokens, email verification persistence e outbox persistence.

A ordem recomendada é: `baseline/config -> sessão Redis -> status explícito do usuário -> modo token -> registro/OTP -> outbox/worker -> hardening -> observabilidade`.
