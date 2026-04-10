# ProxyPay

> Proxy multi-tenant para gateways de pagamento com suporte a PIX, boleto e cartao.

**Produto Emagine** | [Repositorio](https://github.com/emaginebr/ProxyPay) | [Website](https://emagine.com.br/proxypay)

---

## O que e o ProxyPay?

O ProxyPay e um microservico que funciona como proxy unificado para multiplos gateways de pagamento. Ele abstrai a complexidade de integrar diferentes provedores de pagamento, oferecendo uma API unica para PIX (QR Code), boleto, cartao de credito e debito, faturas e cobrancas recorrentes.

Com arquitetura multi-tenant, cada cliente pode ter seus proprios provedores de pagamento configurados, sem compartilhamento de credenciais ou dados financeiros.

---

## Principais Funcionalidades

- **PIX com QR Code** — Geracao de QR Code PIX com verificacao automatica de status
- **Faturas (Invoice)** — Criacao de faturas com redirect para pagamento
- **Cobrancas recorrentes (Billing)** — Assinaturas configuraveis com diferentes frequencias (mensal, trimestral, semestral, anual)
- **Multi-gateway** — Suporte a multiplos provedores de pagamento em uma unica API
- **Multi-tenant** — Cada tenant possui suas proprias credenciais e configuracoes de gateway
- **API unificada** — Interface unica independente do gateway utilizado

---

## Stack Tecnologica

| Componente | Tecnologia |
|------------|------------|
| Backend | .NET 8, ASP.NET Core, C# |
| Banco de Dados | PostgreSQL |
| Autenticacao | NAuth (JWT multi-tenant) |
| Documentacao | Swagger |
| Infraestrutura | Docker, Docker Compose |
| CI/CD | GitHub Actions |

---

## Arquitetura e Integracoes

O ProxyPay atua como camada intermediaria entre sua aplicacao e os gateways de pagamento. As requisicoes chegam com identificacao do tenant, o middleware resolve as credenciais do gateway configurado para aquele tenant, e a operacao e executada no provedor correto.

### Produtos Emagine Relacionados

- **proxypay-react** — Biblioteca React com componentes de pagamento (PIX, Invoice, Billing)
- **NAuth** — Autenticacao e resolucao multi-tenant
- **Brahma / Lofn** — Plataformas de e-commerce que utilizam ProxyPay para pagamentos
- **MonexUp** — Plataforma MLM que pode usar ProxyPay como alternativa ao Stripe

---

## Casos de Uso

1. **Pagamento PIX em e-commerce** — Exiba QR Code PIX no checkout com verificacao automatica de confirmacao
2. **Assinaturas recorrentes** — Configure cobrancas mensais, trimestrais ou anuais para servicos SaaS
3. **Faturamento B2B** — Gere faturas para clientes empresariais com redirect para pagamento
4. **Multi-gateway** — Use diferentes gateways para diferentes tenants ou metodos de pagamento

---

## Perguntas Frequentes (FAQ)

### Tecnicas

**P: Quais gateways de pagamento sao suportados?**
R: O ProxyPay foi projetado para suportar multiplos gateways. Entre em contato para saber quais provedores estao atualmente integrados.

**P: Como verifico se um PIX foi pago?**
R: A API oferece endpoint de verificacao de status que pode ser consultado via polling ou pelo componente proxypay-react que faz isso automaticamente.

**P: Existe SDK frontend?**
R: Sim, o proxypay-react fornece componentes React prontos para PIX, Invoice e Billing.

**P: Como configuro as credenciais do gateway?**
R: As credenciais sao configuradas por tenant no backend. Cada tenant pode ter seu proprio provedor de pagamento.

### Comerciais

**P: Posso usar com meu proprio gateway?**
R: O ProxyPay suporta multiplos gateways. Entre em contato para verificar compatibilidade com seu provedor.

**P: Qual o custo?**
R: Entre em contato com a equipe comercial da Emagine.

---

## Como Comecar

1. Configure as credenciais do gateway no backend
2. Suba via Docker: `docker-compose up -d --build`
3. Integre usando a API REST ou o pacote proxypay-react no frontend

---

## Diferenciais Competitivos

- **Gateway-agnostico** — Troque de provedor de pagamento sem alterar codigo da aplicacao
- **Multi-tenant** — Credenciais e configuracoes isoladas por tenant
- **SDK React pronto** — Componentes de pagamento prontos para uso no frontend
- **PIX nativo** — Geracao de QR Code e verificacao de status integrados

---

## Contato e Suporte

- **Website**: https://emagine.com.br/proxypay
- **Repositorio**: https://github.com/emaginebr/ProxyPay
- **Suporte**: Entre em contato pelo site da Emagine

---

*Documentacao gerada automaticamente a partir do repositorio GitHub. Ultima atualizacao: 2026-04-10*
