# AuthCore - Aprovação por Blocos

Este arquivo existe para controlar a execução do plano em partes menores, com liberação explícita antes de avançar para o próximo bloco.

## Como usar

1. Revise o bloco atual.
2. Se quiser liberar a execução do bloco, me peça pelo nome do bloco ou marque a autorização aqui.
3. Eu executo apenas até o critério de parada do bloco liberado.
4. Ao finalizar, paro e aguardo sua próxima autorização.

Regra operacional adotada:
- eu não avanço automaticamente para o bloco seguinte;
- cada bloco tem escopo, entregáveis e critério de parada próprios;
- quando um bloco depender de ajuste fino, isso deve ser resolvido dentro do próprio bloco antes de encerrar.

---

## Estado atual

- Bloco atual concluído: `Bloco 2 - Sessão Primária`
- Próximo bloco elegível: `Bloco 3 - Sessões por Device`
- Observação: o fluxo principal stateful foi validado com autenticação por cookie `sid`, `me` e logout.

---

## Bloco 1 - Fundação e Identidade

Status:
- `Concluído`

Objetivo:
- estabilizar a base estrutural, contratos centrais no `Domain` e o estado explícito do usuário.

Inclui:
- contratos centrais no `Domain` para sessão, verificação de e-mail, outbox e envio de e-mail;
- introdução de `UserStatus`;
- atualização do agregado `User` e da persistência para suportar o status funcional;
- migrações base para `UserStatus`, expansão de `EmailVerificationTokens` e `OutboxMessages`;
- registro de DI da infraestrutura e contratos novos.

Não inclui:
- fluxo HTTP final de sessão por cookie;
- autenticação por sessão ponta a ponta;
- endpoints de device management;
- hardening, worker e observabilidade final.

Critério de parada:
- solução compila com a nova base estrutural e sem avançar para a entrega funcional completa da sessão.

Autorização:
- `Já executado`

---

## Bloco 2 - Sessão Primária

Status:
- `Concluído`

Objetivo:
- entregar o fluxo principal correto da spec: login por sessão Redis com cookie `sid`, `me` e logout.

Inclui:
- finalizar `SessionStore` em Redis para o fluxo principal;
- criar autenticação por cookie de sessão;
- separar claramente login por sessão do login por token;
- expor `POST /auth/login`, `GET /auth/me` e `POST /auth/logout` no contrato canônico da sessão;
- alinhar respostas `401` e `403` com o estado do usuário.

Não inclui:
- listagem de devices;
- logout global;
- CSRF;
- rate limiting;
- worker, outbox processor e envio real de e-mail.

Critério de parada:
- fluxo mínimo validável: `login -> me -> logout -> me(401)`.

Entregáveis esperados:
- controllers e contracts mínimos da sessão;
- auth handler ou scheme para cookie `sid`;
- casos de uso da sessão ajustados;
- integração da API com Redis para autenticação stateful.

Autorização:
- `Executado`

Comando de liberação sugerido:
- `Autorizo o Bloco 2 - Sessão Primária`

---

## Bloco 3 - Sessões por Device

Status:
- `Aguardando autorização`

Objetivo:
- permitir ao usuário listar e revogar sessões por device.

Inclui:
- `GET /auth/sessions`;
- `DELETE /auth/sessions/{sid}`;
- `POST /auth/logout-all`;
- validação de posse da sessão;
- limpeza de sessões órfãs no índice do Redis.

Não inclui:
- CSRF e rate limiting;
- ajustes finais de observabilidade;
- worker e mensageria.

Critério de parada:
- sessões por device funcionando e revogação imediata comprovável.

Entregáveis esperados:
- casos de uso de listagem/revogação/logout global;
- contratos HTTP de device management;
- integração com `ISessionStore`.

Autorização:
- `Pendente`

Comando de liberação sugerido:
- `Autorizo o Bloco 3 - Sessões por Device`

---

## Bloco 4 - Hardening da Sessão

Status:
- `Aguardando autorização`

Objetivo:
- endurecer o fluxo de sessão com política de cookie, CSRF e rate limiting.

Inclui:
- política unificada de cookie;
- validação de `Origin`/`Referer` para endpoints mutáveis autenticados por cookie;
- rate limiting do login;
- ajustes mínimos de logs de segurança ligados ao fluxo de sessão.

Não inclui:
- worker;
- observabilidade final completa;
- fechamento do fluxo de OTP e outbox.

Critério de parada:
- endpoints mutáveis por cookie protegidos e login com limitação básica.

Autorização:
- `Pendente`

Comando de liberação sugerido:
- `Autorizo o Bloco 4 - Hardening da Sessão`

---

## Bloco 5 - Modo Token Secundário

Status:
- `Aguardando autorização`

Objetivo:
- reposicionar o modo JWT/refresh como trilha secundária, separada do login por sessão.

Inclui:
- `POST /auth/token`;
- autenticação bearer ajustada;
- emissão inicial de refresh;
- rotação em `POST /auth/refresh`;
- logout do modo token.

Não inclui:
- cadastro com OTP ponta a ponta;
- worker de e-mail;
- observabilidade final.

Critério de parada:
- fluxo secundário funcionando: `/auth/token -> /auth/refresh -> token logout`.

Autorização:
- `Pendente`

Comando de liberação sugerido:
- `Autorizo o Bloco 5 - Modo Token Secundário`

---

## Bloco 6 - Registro, OTP e Outbox

Status:
- `Aguardando autorização`

Objetivo:
- concluir o cadastro pendente, verificação de e-mail, reenvio e persistência resiliente do evento.

Inclui:
- `POST /auth/register`;
- `POST /auth/verify-email`;
- `POST /auth/resend-verification`;
- persistência de OTP com cooldown e tentativas;
- persistência de outbox na mesma transação do registro/reenvio.

Não inclui:
- worker consumidor e ciclo operacional completo de publicação;
- observabilidade final completa.

Critério de parada:
- fluxo funcional: `register -> login(403) -> verify-email -> login(200)`.

Autorização:
- `Pendente`

Comando de liberação sugerido:
- `Autorizo o Bloco 6 - Registro, OTP e Outbox`

---

## Bloco 7 - Worker e Fechamento Operacional

Status:
- `Aguardando autorização`

Objetivo:
- fechar publicação/consumo, envio de e-mail e observabilidade final.

Inclui:
- processor da outbox;
- worker hospedado pela API;
- integração com sender inicial;
- métricas e logs finais;
- health checks complementares;
- fechamento dos testes restantes.

Critério de parada:
- solução pronta para fluxo operacional completo dentro do escopo da spec atual.

Autorização:
- `Pendente`

Comando de liberação sugerido:
- `Autorizo o Bloco 7 - Worker e Fechamento Operacional`

---

## Histórico de liberações

- `Bloco 1 - Fundação e Identidade`: executado
- `Bloco 2 - Sessão Primária`: executado
- `Bloco 3 - Sessões por Device`: pendente
- `Bloco 4 - Hardening da Sessão`: pendente
- `Bloco 5 - Modo Token Secundário`: pendente
- `Bloco 6 - Registro, OTP e Outbox`: pendente
- `Bloco 7 - Worker e Fechamento Operacional`: pendente
