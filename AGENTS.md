# AGENTS.md

## Objetivo

O projeto segue uma arquitetura em camadas, com forte influência de Clean Architecture e DDD tático, usando a `Domain` como referência principal de estilo e modelagem. A solução está organizada em 5 projetos:

- `Api`: camada de entrada HTTP.
- `Application`: orquestração de casos de uso.
- `Domain`: regras de negócio, contratos centrais e tipos de domínio.
- `Infrastructure`: persistência, criptografia, migrações e configuração técnica.
- `Domain.UnitTests`: testes unitários focados no domínio.

## Dependências e limites de camada

Preserve o fluxo de dependência atual:

- `MyApp.Api -> MyApp.Application`
- `MyApp.Api -> MyApp.Infrastructure`
- `MyApp.Application -> MyApp.Domain`
- `MyApp.Infrastructure -> MyApp.Domain`
- `MyApp.Domain.UnitTests -> MyApp.Domain`

Não introduza atalhos que violem esse fluxo.

Regras de negócio centrais pertencem ao domínio. A camada de aplicação orquestra. A API adapta HTTP. A infraestrutura implementa detalhes técnicos.

## Estrutura da solução

A solução está organizada em:

- `Api`: borda HTTP.
- `Application`: casos de uso e orquestração.
- `Domain`: regras de negócio, contratos centrais e tipos de domínio.
- `Infrastructure`: persistência, criptografia, migrações e configuração técnica.
- `Domain.UnitTests`: testes unitários do domínio.

Quando uma mudança afetar mais de uma camada, mantenha cada responsabilidade no projeto correto.

## Regras por camada

### Domain

A camada `Domain` é a fonte de verdade para estilo e modelagem.

Preferências obrigatórias:

- Modelagem orientada a domínio, com comportamento dentro de entidades/agregados.
- Uso de value objects para proteger invariantes.
- Construtores privados quando a criação precisa ser controlada.
- Métodos de fábrica estáticos como `Create`, `Register`, `Restore` e `Read` quando fizer sentido.
- Propriedades encapsuladas, preferencialmente com `private set`.
- Validação interna do estado da entidade.
- Evite mover regra de negócio para serviços utilitários ou para a camada de aplicação.

Ao materializar estado vindo da persistência, prefira o padrão dominante já existente no projeto.

### Application

A camada de aplicação deve atuar como orquestradora de caso de uso.

Padrão esperado:

- Organização por módulo e por caso de uso.
- Cada caso de uso tende a agrupar `I...UseCase`, `...UseCase`, `...Command` e `...Result` quando houver retorno estruturado.
- Valide argumentos com `ArgumentNullException.ThrowIfNull` quando aplicável.
- Busque entidades via repositórios.
- Aplique regras por meio do domínio, e não reimplemente regra central na aplicação.
- Abra transação quando necessário.
- Faça `Commit` ou `Rollback` no lugar apropriado.
- Retorne DTO de resultado apenas quando isso agregar clareza.

Não transforme a camada de aplicação no lugar principal das regras de negócio.

### Api

A API deve permanecer fina.

Responsabilidades esperadas do controller:

- Receber request HTTP.
- Mapear request para command/query.
- Chamar o use case.
- Converter o resultado em response JSON.

Não coloque regra de negócio em controller.

Os contratos HTTP devem seguir a convenção de nomes com sufixo `Json`, por exemplo:

- `RequestRegisterUserJson`
- `ResponseRegisteredUserJson`

### Infrastructure

A infraestrutura implementa detalhes técnicos, sem absorver regra de negócio.

Padrões esperados:

- Persistência com `Npgsql` e SQL explícito.
- Uso de raw string para SQL quando isso já for o padrão do arquivo/módulo.
- Materialização do domínio por métodos como `Restore(...)`.
- Separação entre leitura e escrita.
- Uso de transação compartilhada via abstrações como `IDatabaseSession` e `IUnitOfWork`.
- Migrações versionadas com padrão `Version0000001`, `Version0000002`, etc.
- Configurações e preocupações técnicas separadas por responsabilidade.

Não introduza EF Core, ORM ou abstrações pesadas de persistência sem pedido explícito.

### Domain.UnitTests

Os testes atuais priorizam o domínio e esse é o foco correto.

Padrões esperados:

- xUnit.
- Nome de teste no formato `Metodo_WhenCondicao_ShouldResultado`.
- Cobertura de invariantes e comportamento de negócio.
- Ao alterar o domínio, atualize ou adicione testes de domínio.

## Convenções de código C#

Mantenha o padrão dominante do repositório:

- `namespace` file-scoped.
- Classes concretas preferencialmente `sealed`.
- Interfaces com prefixo `I`.
- Uso consistente de `async/await`.
- Dependências injetadas por construtor.
- Campos privados como `private readonly`.
- Métodos auxiliares privados no fim da classe.

Ordem preferencial de membros:

1. campos privados e constantes
2. propriedades
3. construtores
4. métodos públicos
5. métodos privados

## Documentação XML

A documentação XML pública é parte importante do padrão do projeto e deve permanecer em português.

Siga estas formas textuais:

- Classes concretas: `Representa ...`
- Interfaces: `Define operação ...` ou `Define operações ...`
- Métodos: `Operação para ...`
- Fábricas: `Operação para criar instância da classe.`

Diretrizes adicionais:

- Seja curto, objetivo e consistente.
- Use `param` com frequência consistente quando houver parâmetros relevantes.
- Use `returns` quando realmente agregar valor.
- Documente propriedades públicas quando o significado não for óbvio.
- Ao implementar uma interface, mantenha a mesma linha semântica de documentação.

## Convenções de nomenclatura

Mantenha a previsibilidade atual do repositório:

- Agregados e entidades: nomes de negócio simples, como `User` e `Password`
- Interface de caso de uso: `IRegisterUserUseCase`
- Implementação: `RegisterUserUseCase`
- Entrada: `RegisterUserCommand`
- Saída: `RegisterUserResult`
- Contratos HTTP: `Request...Json` e `Response...Json`
- Configuração: `...Options`
- Migração: `Version0000001`

## Regra de evolução do código

O repositório possui um padrão dominante claro e alguns traços residuais de código legado.

Ao tocar arquivos mais antigos:

- Prefira alinhar o trecho alterado ao padrão dominante atual.
- Não replique inconsistências antigas em novos arquivos.
- Evite refactors amplos e não relacionados apenas para “modernizar tudo”.
- Se houver conflito entre um arquivo legado e o padrão recente do domínio, siga o padrão recente, salvo restrição explícita do arquivo ou da tarefa.

## Mudanças esperadas por tipo

Ao adicionar ou alterar código de domínio:

- preserva invariantes
- usa value objects quando necessário
- mantém comportamento dentro de agregados/entidades
- atualiza testes de domínio

Ao adicionar ou alterar use cases:

- mantenha a organização vertical por caso de uso
- orquestre repositórios e transação sem roubar responsabilidades do domínio

Ao adicionar ou alterar persistência:

- preserve a separação entre leitura e escrita
- prefira SQL explícito e materialização consistente com o domínio
- respeite o padrão transacional existente

Ao adicionar ou alterar endpoints:

- mantenha controller fino.
- preserve a convenção `Request...Json` e `Response...Json`

## Validação antes de concluir

Antes de finalizar uma tarefa:

- rode a menor validação relevante para a área alterada
- se o domínio foi alterado, execute os testes de domínio
- se a assinatura pública mudou, revise a documentação XML e contratos afetados
- não finalize mudanças com inconsistência óbvia entre arquitetura, nomes e estilo

## Referências aprofundadas

Se estes arquivos existirem, leia-os antes de alterar a área correspondente:

- Arquitetura geral: `docs/agents/architecture-overview.md`
- Modelagem de domínio: `docs/agents/domain-modeling.md`
- Casos de uso: `docs/agents/application-use-cases.md`
- Contratos HTTP: `docs/agents/api-contracts.md`
- Persistência e infraestrutura: `docs/agents/infrastructure-persistence.md`
- Convenções C# e XML docs: `docs/agents/csharp-style.md`
- Estratégia de testes: `docs/agents/testing.md`

Se a tarefa tocar mais de uma camada, leia todas as referências aplicáveis antes de implementar.

Se uma referência aprofundada não existir, use este `AGENTS.md` e o padrão recente do código como fonte de verdade.
