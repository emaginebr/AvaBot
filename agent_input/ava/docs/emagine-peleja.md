# Peleja

> Widget de comentarios e discussoes multi-tenant para qualquer site.

**Produto Emagine** | [Repositorio](https://github.com/emaginebr/Peleja)

---

## O que e o Peleja?

O Peleja e um sistema completo de comentarios e discussoes que pode ser incorporado em qualquer pagina web. Ele permite que visitantes comentem, respondam, curtam e compartilhem GIFs nas discussoes, tudo com moderacao por site e autenticacao integrada.

Com arquitetura multi-tenant, cada site que utiliza o Peleja opera com dados isolados e configuracoes independentes de moderacao. A autenticacao e gerenciada pelo NAuth com segredos JWT por tenant, garantindo seguranca e isolamento total.

O Peleja e composto por um backend .NET (API) e um frontend React (widget), ambos trabalhando em conjunto para oferecer uma experiencia de comentarios moderna e performatica.

---

## Principais Funcionalidades

- **Comentarios com edicao e exclusao** — Usuarios podem criar, editar e excluir (soft-delete) seus comentarios
- **Respostas encadeadas** — Suporte a um nivel de resposta para discussoes organizadas
- **Curtidas com contagem** — Toggle de like/unlike com contagem agregada por comentario
- **Integracao com GIFs** — Busca e insercao de GIFs do Giphy diretamente nos comentarios
- **Gerenciamento de sites** — Cada site possui ClientId unico com configuracoes independentes de moderacao
- **Multi-tenant** — Isolamento por banco de dados com segredos JWT dinamicos por tenant
- **Paginacao por cursor** — Carregamento eficiente com ordenacao por "recentes" ou "populares"
- **Rate limiting por IP** — Protecao contra spam em endpoints de criacao
- **Documentacao Swagger** — API documentada e interativa

---

## Stack Tecnologica

| Componente | Tecnologia |
|------------|------------|
| Backend | .NET 8.0, ASP.NET Core, C# |
| Frontend | React 18, TypeScript, Bootstrap 5 |
| Banco de Dados | PostgreSQL 17, Entity Framework Core |
| Autenticacao | NAuth (JWT multi-tenant) |
| GIFs | Giphy API |
| Rate Limiting | AspNetCoreRateLimit |
| Testes | xUnit, Moq, FluentAssertions |
| Infraestrutura | Docker, Docker Compose |
| CI/CD | GitHub Actions, GitVersion |

---

## Arquitetura e Integracoes

Clean Architecture com camadas API, Application, Domain, DTO e Infra. O fluxo de requisicao usa headers `X-Client-Id` para identificar o site e Bearer Token para autenticacao do usuario.

O multi-tenant resolve o tenant do NAuth dinamicamente para validar JWT com o segredo correto de cada tenant.

### Produtos Emagine Relacionados

- **peleja-react** — Widget frontend React que consome a API do Peleja
- **NAuth** — Autenticacao e validacao JWT multi-tenant
- **Dedalo / dedalo-app** — CMS que pode incorporar o widget de comentarios
- **devblog** — Blog que utiliza o Peleja para comentarios nos artigos

---

## Casos de Uso

1. **Comentarios em blog** — Adicione discussoes em artigos de blog com autenticacao de usuarios e moderacao
2. **Feedback em paginas de produto** — Permita que clientes comentem e discutam sobre produtos
3. **Forum de discussao simples** — Use como sistema leve de forum com curtidas e GIFs
4. **Widget multi-site** — Uma unica instalacao serve comentarios para multiplos sites com dados isolados

---

## Perguntas Frequentes (FAQ)

### Tecnicas

**P: Como adiciono o Peleja ao meu site?**
R: Instale o widget peleja-react e configure com o ClientId do seu site e a URL da API.

**P: Os dados de cada site ficam separados?**
R: Sim, cada site possui seu proprio ClientId e os dados sao isolados por tenant no banco de dados.

**P: Como funciona a moderacao?**
R: Cada site possui configuracoes independentes de moderacao gerenciadas pelo proprietario do site.

**P: Existe limite de comentarios?**
R: Existe rate limiting por IP para prevenir spam, mas nao ha limite de volume de comentarios.

**P: Posso customizar a aparencia do widget?**
R: Sim, o peleja-react utiliza Bootstrap 5 e pode ser estilizado conforme necessidade.

### Comerciais

**P: O Peleja pode ser auto-hospedado?**
R: Sim, basta ter Docker e configurar o banco PostgreSQL e NAuth.

**P: Qual o custo?**
R: Entre em contato com a equipe comercial da Emagine para informacoes sobre precos.

---

## Como Comecar

1. Copie a configuracao: `cp .env.example .env`
2. Configure PostgreSQL, URL do NAuth, segredo JWT e chave da API Giphy
3. Crie a rede: `docker network create emagine-network`
4. Suba os servicos: `docker-compose up -d --build`

---

## Diferenciais Competitivos

- **Multi-tenant nativo** — Uma instalacao serve multiplos sites com isolamento total
- **GIFs integrados** — Busca Giphy nativa melhora o engajamento das discussoes
- **Leve e embarcavel** — Widget React compacto que pode ser adicionado a qualquer pagina
- **Ecossistema Emagine** — Autenticacao pronta via NAuth, sem necessidade de sistema proprio

---

## Contato e Suporte

- **Website**: https://emagine.com.br
- **Repositorio**: https://github.com/emaginebr/Peleja
- **Suporte**: Entre em contato pelo site da Emagine

---

*Documentacao gerada automaticamente a partir do repositorio GitHub. Ultima atualizacao: 2026-04-10*
