# Dedalo

> CMS multi-tenant para criacao e gerenciamento de websites dinamicos.

**Produto Emagine** | [Repositorio](https://github.com/emaginebr/Dedalo)

---

## O que e o Dedalo?

O Dedalo e um sistema de gerenciamento de conteudo (CMS) multi-tenant que permite criar e gerenciar websites completos. Ele fornece uma API REST para gerenciar sites, paginas, menus e blocos de conteudo, com resolucao de tenant por dominio, subdominio ou caminho.

Com o Dedalo, empresas podem operar multiplos websites a partir de uma unica instalacao, cada um com seu proprio dominio, conteudo e configuracao, com isolamento total de dados por tenant.

---

## Principais Funcionalidades

- **Gerenciamento de websites** — CRUD de sites com dominios customizados, subdominios ou roteamento por caminho
- **Paginas dinamicas** — Criacao de paginas com slugs para acesso publico e multiplos templates
- **Sistema de conteudo flexivel** — Blocos de conteudo tipados com ordenacao e operacoes em lote por area
- **Menus hierarquicos** — Hierarquia pai-filho para navegacao complexa com auto-criacao de paginas
- **Upload de arquivos** — Upload via zTools com isolamento de bucket por tenant
- **Multi-tenant** — Banco de dados por tenant com resolucao dinamica via header X-Tenant-Id
- **Autenticacao baseada em propriedade** — Endpoints anonimos para renderizacao + endpoints autenticados para gerenciamento
- **Documentacao Swagger** — API interativa documentada

---

## Stack Tecnologica

| Componente | Tecnologia |
|------------|------------|
| Backend | .NET 8, ASP.NET Core, C# |
| Banco de Dados | PostgreSQL 17, Entity Framework Core 9 |
| Autenticacao | NAuth (JWT multi-tenant) |
| Upload | zTools (S3 por tenant) |
| Mapeamento | AutoMapper |
| Logging | Serilog |
| Testes | xUnit, Moq, Coverlet |
| Infraestrutura | Docker, Docker Compose |
| CI/CD | GitHub Actions |

---

## Arquitetura e Integracoes

Clean Architecture: API -> Application -> Domain -> Infra -> PostgreSQL. Inclui camadas DTO e Infra.Interfaces.

Fluxo: Client -> TenantMiddleware -> NAuth JWT validation -> Controller -> Domain Service -> Repository -> EF Core -> banco do tenant.

Endpoints publicos (sem auth) para renderizacao de sites e endpoints autenticados para gerenciamento.

### Produtos Emagine Relacionados

- **dedalo-app** — Frontend React que consome a API do Dedalo
- **NAuth** — Autenticacao e validacao JWT multi-tenant
- **zTools** — Upload de arquivos, ChatGPT e email
- **Peleja** — Widget de comentarios que pode ser incorporado nos sites

---

## Casos de Uso

1. **Plataforma de websites multi-tenant** — Agencia gerencia multiplos sites de clientes em uma unica instalacao
2. **Portal corporativo** — Empresa cria e gerencia seu website com paginas, menus e conteudo dinamico
3. **Blog profissional** — Criacao de blog com paginas, categorias e templates customizaveis
4. **Landing pages** — Crie paginas de captura rapidas com blocos de conteudo flexiveis

---

## Perguntas Frequentes (FAQ)

### Tecnicas

**P: Como funciona a resolucao de dominio?**
R: O Dedalo suporta tres modos: dominio customizado, subdominio ou roteamento por pasta (path-based).

**P: Posso criar templates customizados?**
R: Sim, o sistema de templates e extensivel e cada pagina pode usar um template diferente.

**P: O conteudo e editavel visualmente?**
R: Sim, atraves do dedalo-app que oferece drag-and-drop para edicao visual de blocos de conteudo.

**P: Como funciona o upload de arquivos?**
R: Arquivos sao enviados via API e armazenados no S3 do zTools com isolamento por tenant.

**P: Existe API para acesso publico ao conteudo?**
R: Sim, endpoints de leitura de sites, paginas e menus sao publicos (sem autenticacao) para renderizacao no frontend.

### Comerciais

**P: Quantos sites posso gerenciar?**
R: Nao ha limite tecnico. Cada tenant pode ter multiplos websites.

**P: Qual o custo?**
R: Entre em contato com a equipe comercial da Emagine.

---

## Como Comecar

1. Crie a rede: `docker network create emagine-network`
2. Suba: `docker-compose up -d --build`
3. Acesse Swagger em `http://localhost:5000/swagger/ui`
4. Instale o dedalo-app para ter a interface visual de gerenciamento

---

## Diferenciais Competitivos

- **Multi-tenant real** — Isolamento por banco de dados, nao apenas filtros de dados
- **Resolucao flexivel** — Dominio, subdominio ou path-based para atender qualquer cenario
- **Conteudo tipado e flexivel** — Blocos de conteudo com tipo, area e ordenacao para layouts complexos
- **Ecossistema completo** — Frontend (dedalo-app), autenticacao (NAuth), armazenamento (zTools) integrados

---

## Contato e Suporte

- **Website**: https://emagine.com.br
- **Repositorio**: https://github.com/emaginebr/Dedalo
- **Suporte**: Entre em contato pelo site da Emagine

---

*Documentacao gerada automaticamente a partir do repositorio GitHub. Ultima atualizacao: 2026-04-10*
