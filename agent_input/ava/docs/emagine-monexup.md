# MonexUp

> Plataforma de marketing multinivel (MLM) com gestao financeira e comissoes.

**Produto Emagine** | [Repositorio](https://github.com/emaginebr/MonexUp) | [Website](https://monexup.com)

---

## O que e o MonexUp?

O MonexUp e uma plataforma completa de marketing multinivel (MLM) que permite gerenciar redes de vendas, doacoes, assinaturas e comissoes em multiplos niveis. E uma solucao SaaS pronta para empresas que operam com modelos de venda direta ou network marketing.

A plataforma inclui gestao de redes, produtos, pedidos, faturas, sistema de comissoes multi-nivel, pagamentos via Stripe e suporte a aplicativos moveis via Capacitor. Tudo com interface multi-idioma (portugues, ingles, espanhol e frances).

---

## Principais Funcionalidades

- **Gestao de redes multinivel** — Crie e gerencie redes de vendedores com hierarquia multi-nivel
- **Sistema de comissoes** — Calculo e rastreamento automatico de comissoes em multiplos niveis
- **Pagamentos via Stripe** — Checkout embarcado para doacoes e assinaturas
- **Gestao de produtos e pedidos** — CRUD completo de produtos, pedidos e faturas
- **Multi-idioma** — Interface em portugues, ingles, espanhol e frances (i18next)
- **Aplicativo mobile** — Compilacao para Android via Capacitor 7
- **Templates dinamicos** — Sistema de templates customizaveis por rede
- **Integracao com IA** — ChatGPT e DALL-E via zTools para conteudo e imagens
- **Email transacional** — Envio de emails via MailerSend
- **Tarefas agendadas** — Background service com NCrontab para processamento periodico

---

## Stack Tecnologica

| Componente | Tecnologia |
|------------|------------|
| Backend | ASP.NET Core 8.0, C# |
| Frontend | React 18, TypeScript 5.3, Bootstrap 5, Material-UI 6 |
| Banco de Dados | PostgreSQL 16, Entity Framework Core 9 |
| Pagamentos | Stripe |
| Armazenamento | DigitalOcean Spaces (S3 compativel) |
| Autenticacao | NAuth (JWT) |
| Mobile | Capacitor 7 |
| i18n | i18next |
| Editor | Quill |
| Infraestrutura | Docker, Docker Compose, Nginx |
| CI/CD | GitHub Actions, GitVersion |

---

## Arquitetura e Integracoes

Monorepo com backend .NET e frontend React. Arquitetura DDD com camadas API, Application, Domain, DTO e Infra. Frontend organizado por Business/Components/Contexts/Services.

Submodulos Git: NAuth (autenticacao) e zTools (utilitarios).

### Produtos Emagine Relacionados

- **NAuth** — Autenticacao centralizada de usuarios
- **zTools** — ChatGPT, DALL-E, upload de arquivos e email
- **ProxyPay** — Alternativa ao Stripe para pagamentos

---

## Casos de Uso

1. **Empresa de venda direta** — Gerencie toda a rede de vendedores, comissoes e pedidos em uma unica plataforma
2. **Plataforma de doacoes recorrentes** — Configure assinaturas e doacoes com checkout Stripe integrado
3. **Network marketing digital** — Vendedores acompanham suas redes, comissoes e pedidos pelo aplicativo mobile
4. **Marketplace multinivel** — Multiplas redes operando com produtos, templates e configuracoes independentes

---

## Perguntas Frequentes (FAQ)

### Tecnicas

**P: Quais niveis de comissao sao suportados?**
R: O sistema suporta multiplos niveis de comissao configurados por rede.

**P: O aplicativo mobile funciona em iOS?**
R: Atualmente o build mobile e via Capacitor para Android. iOS pode ser adicionado com configuracao adicional.

**P: Como funciona a integracao com Stripe?**
R: O MonexUp utiliza Stripe Embedded Checkout para processar pagamentos de assinaturas e doacoes.

**P: Posso customizar os templates das redes?**
R: Sim, cada rede possui sistema de templates dinamicos customizaveis.

**P: Quais idiomas sao suportados?**
R: Portugues, ingles, espanhol e frances, com possibilidade de adicionar novos.

### Comerciais

**P: O MonexUp e um SaaS ou auto-hospedado?**
R: O MonexUp esta disponivel em https://monexup.com como SaaS. Para hospedagem propria, entre em contato.

**P: Qual o modelo de precos?**
R: Entre em contato com a equipe comercial pelo site monexup.com.

---

## Como Comecar

1. Clone com submodulos: `git clone --recurse-submodules`
2. Copie e configure: `cp .env.example .env`
3. Configure credenciais: PostgreSQL, NAuth JWT, Stripe, DigitalOcean Spaces
4. Suba: `docker-compose up -d --build`
5. Backend na porta 5000, frontend na porta 3000

---

## Diferenciais Competitivos

- **Solucao completa de MLM** — Rede, comissoes, pedidos, faturas e pagamentos em uma unica plataforma
- **Multi-idioma nativo** — 4 idiomas inclusos para operacao internacional
- **Mobile ready** — Aplicativo Android via Capacitor sem desenvolvimento nativo adicional
- **IA integrada** — ChatGPT e DALL-E para geracao de conteudo e imagens de produtos

---

## Contato e Suporte

- **Website**: https://monexup.com
- **Repositorio**: https://github.com/emaginebr/MonexUp
- **Suporte**: Entre em contato pelo site da Emagine

---

*Documentacao gerada automaticamente a partir do repositorio GitHub. Ultima atualizacao: 2026-04-10*
