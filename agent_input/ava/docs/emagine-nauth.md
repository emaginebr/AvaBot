# NAuth

> Framework completo de autenticacao multi-tenant com isolamento por banco de dados.

**Produto Emagine** | [Repositorio](https://github.com/emaginebr/NAuth) | [Website](https://emagine.com.br/nauth)

---

## O que e o NAuth?

O NAuth e o framework central de autenticacao e gerenciamento de usuarios do ecossistema Emagine. Ele oferece uma solucao completa e modular para registro, login, recuperacao de senha, controle de acesso baseado em roles (RBAC) e gerenciamento de perfis.

O grande diferencial do NAuth e sua arquitetura multi-tenant com isolamento total por banco de dados. Cada tenant (empresa/aplicacao) possui seu proprio banco PostgreSQL, segredo JWT e bucket S3, garantindo isolamento completo de dados e seguranca.

O NAuth e utilizado por todos os demais produtos da Emagine como servico centralizado de autenticacao, alem de ser distribuido como pacote NuGet para integracao em projetos .NET externos.

---

## Principais Funcionalidades

- **Registro de usuarios com confirmacao por email** — Fluxo completo de criacao de conta com verificacao via MailerSend
- **Autenticacao JWT multi-tenant** — Cada tenant possui seu proprio segredo JWT, garantindo isolamento total
- **Isolamento por banco de dados** — Cada tenant tem seu proprio PostgreSQL, sem compartilhamento de dados
- **Recuperacao de senha** — Fluxo seguro de recuperacao via email
- **Controle de acesso por roles (RBAC)** — Crie e atribua roles para controlar permissoes
- **Gerenciamento de perfil** — Edicao de dados pessoais e upload de foto de perfil
- **Integracao com Stripe** — Pagamentos integrados por tenant
- **Armazenamento S3 por tenant** — Cada tenant possui seu proprio bucket na Amazon S3
- **Pacote NuGet reutilizavel** — DTOs e ACL distribuidos como pacote para integracao facil em outros projetos .NET
- **Health checks** — Monitoramento de saude do servico

---

## Stack Tecnologica

| Componente | Tecnologia |
|------------|------------|
| Backend | ASP.NET Core 8.0, C# |
| Banco de Dados | PostgreSQL 16 |
| Autenticacao | JWT (HMAC-SHA256), BCrypt |
| Email | MailerSend |
| Pagamentos | Stripe |
| Armazenamento | Amazon S3 |
| Processamento de Imagem | SixLabors.ImageSharp |
| Documentacao | Swagger/Swashbuckle |
| Testes | xUnit, Moq, Coverlet |
| Infraestrutura | Docker, Docker Compose |
| CI/CD | GitHub Actions, GitVersion |

---

## Arquitetura e Integracoes

O NAuth segue Clean Architecture e utiliza middleware de tenant para resolver qual banco de dados e segredo JWT utilizar em cada requisicao.

O fluxo multi-tenant funciona assim: a requisicao chega com o header `X-Tenant-Id` ou claim `tenant_id` no JWT, o TenantMiddleware resolve o tenant, o TenantDbContextFactory cria a conexao com o banco correto, e o MultiTenantHandler valida o JWT com o segredo especifico do tenant.

Tenants configurados incluem: emagine (padrao), viralt, devblog e bazzuca.

### Produtos Emagine Relacionados

- **nauth-react** — Biblioteca React com componentes de login, registro e gerenciamento de usuarios
- **Todos os produtos backend** — Avachat, Brahma, Lofn, Dedalo, NNews, ProxyPay, BazzucaMedia e MonexUp utilizam NAuth para autenticacao
- **zTools** — Processamento de imagens de perfil

---

## Casos de Uso

1. **Autenticacao centralizada para multiplas aplicacoes** — Uma empresa utiliza NAuth para gerenciar usuarios de todos os seus sistemas com um unico ponto de autenticacao
2. **SaaS multi-tenant** — Cada cliente do SaaS possui seu proprio banco de dados e configuracoes de seguranca, sem risco de vazamento de dados entre tenants
3. **Controle de acesso por roles** — Defina roles como "admin", "editor", "viewer" e controle o que cada usuario pode fazer em cada sistema
4. **Integracao rapida via NuGet** — Projetos .NET podem adicionar autenticacao NAuth em minutos usando o pacote NuGet com DTOs e ACL prontos

---

## Perguntas Frequentes (FAQ)

### Tecnicas

**P: Como funciona o isolamento multi-tenant?**
R: Cada tenant possui seu proprio banco de dados PostgreSQL, segredo JWT e bucket S3. O middleware resolve o tenant automaticamente a partir do header ou token JWT.

**P: Como integro o NAuth em meu projeto .NET?**
R: Instale o pacote NuGet do NAuth que inclui DTOs e ACL (Anti-Corruption Layer) com clientes HTTP prontos para consumir a API.

**P: Quais metodos de autenticacao sao suportados?**
R: Atualmente JWT com HMAC-SHA256. OAuth2 e login social estao no roadmap.

**P: Como funciona a recuperacao de senha?**
R: O usuario solicita recuperacao, recebe um email via MailerSend com token unico, e redefine a senha atraves do token.

**P: O sistema suporta upload de foto de perfil?**
R: Sim, com processamento automatico de imagem via ImageSharp e armazenamento no S3 do tenant.

**P: Posso adicionar novos tenants?**
R: Sim, basta configurar o novo tenant no appsettings com seu banco de dados, segredo JWT e bucket S3.

### Comerciais

**P: O NAuth pode ser utilizado como servico independente?**
R: Sim, o NAuth funciona como microservico independente e pode ser implantado em qualquer infraestrutura com Docker.

**P: Qual o modelo de licenciamento?**
R: Entre em contato com a equipe comercial da Emagine para informacoes sobre licenciamento e planos.

**P: Existe suporte para customizacao?**
R: Sim, a Emagine oferece servicos de customizacao para necessidades especificas. Entre em contato para mais detalhes.

---

## Como Comecar

1. Clone o repositorio e copie a configuracao: `cp .env.example .env`
2. Configure as variaveis de ambiente (banco de dados, JWT secrets, MailerSend, S3)
3. Para desenvolvimento: `docker compose -f docker-compose-dev.yml up -d --build`
4. Para producao: configure `.env.prod` e execute `docker compose --env-file .env.prod -f docker-compose-prod.yml up -d --build`
5. Acesse o Swagger para explorar os endpoints

---

## Diferenciais Competitivos

- **Isolamento real por banco de dados** — Diferente de solucoes que usam apenas filtros de tenant, cada tenant tem seu proprio banco, segredo JWT e bucket S3
- **Pacote NuGet pronto** — Integracao em projetos .NET em minutos com DTOs e ACL inclusos
- **Ecossistema completo** — Frontend React (nauth-react) + Backend + NuGet, cobrindo toda a stack
- **Multi-tenant nativo** — Arquitetura projetada desde o inicio para multi-tenancy, nao adaptada depois

---

## Contato e Suporte

- **Website**: https://emagine.com.br/nauth
- **Repositorio**: https://github.com/emaginebr/NAuth
- **Suporte**: Entre em contato pelo site da Emagine

---

*Documentacao gerada automaticamente a partir do repositorio GitHub. Ultima atualizacao: 2026-04-10*
