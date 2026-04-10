# ProxyPay React

> Biblioteca React de componentes de pagamento para integracao com a API ProxyPay.

**Produto Emagine** | [Repositorio](https://github.com/emaginebr/proxypay-react) | [Website](https://emagine.com.br/proxypay)

---

## O que e o ProxyPay React?

O ProxyPay React e um pacote NPM leve (menos de 8KB gzipado) que fornece componentes React prontos para processar pagamentos via PIX, faturas e cobrancas recorrentes. Ele se integra diretamente com a API ProxyPay, abstraindo toda a complexidade de pagamentos em componentes simples de usar.

Zero dependencias em runtime alem do React, o pacote oferece componentes visuais, hooks e tipos TypeScript completos para uma integracao rapida e segura.

---

## Principais Funcionalidades

- **Pagamento PIX** — Geracao de QR Code com modal de exibicao, copy-paste e polling automatico de confirmacao
- **Faturas (Invoice)** — Pagamentos unicos com redirect automatico
- **Cobrancas recorrentes (Billing)** — Assinaturas configuraveis (mensal, trimestral, semestral, anual)
- **Multi-tenant** — Suporte via header X-Tenant-Id automatico
- **TypeScript completo** — Tipos exportados para seguranca em desenvolvimento
- **Modal customizavel** — Estilizacao flexivel do modal de QR Code PIX
- **Hook useProxyPay** — Acesso direto as funcoes da API sem componentes visuais

---

## Stack Tecnologica

| Componente | Tecnologia |
|------------|------------|
| Framework | React 18+, TypeScript 5.7 |
| Build | Vite 8, vite-plugin-dts |
| CI/CD | GitHub Actions, GitVersion |

---

## Arquitetura e Integracoes

Padrao Provider -> Context -> Hook -> Component. Envolva a aplicacao com `ProxyPayProvider` configurando baseUrl, clientId e tenantId, e use os componentes ou hook diretamente.

Componentes exportados: `ProxyPayProvider`, `PixPayment`, `InvoicePayment`, `BillingPayment`.
Hook: `useProxyPay` (createQRCode, checkQRCodeStatus, createInvoice, createBilling).
Enums: `PaymentMethod` (Pix, Boleto, CreditCard, DebitCard), `BillingFrequency` (Monthly, Quarterly, Semiannual, Annual).

### Produtos Emagine Relacionados

- **ProxyPay** — Backend API que esta biblioteca consome
- **Lofn / lofn-react** — E-commerce que pode usar ProxyPay para checkout
- **Brahma** — Gestao de pedidos com pagamento via ProxyPay

---

## Casos de Uso

1. **Checkout PIX em loja virtual** — Adicione pagamento PIX com QR Code em qualquer pagina de checkout
2. **Pagina de assinatura** — Configure cobrancas recorrentes para servicos SaaS
3. **Faturamento com redirect** — Gere faturas com redirect automatico para o provedor de pagamento

---

## Perguntas Frequentes (FAQ)

### Tecnicas

**P: Como instalo?**
R: `npm install proxypay-react` e envolva a app com `ProxyPayProvider`.

**P: Posso usar sem componentes visuais?**
R: Sim, use o hook `useProxyPay` para acessar as funcoes da API diretamente.

**P: O polling de status PIX e automatico?**
R: Sim, o componente `PixPayment` faz polling automatico para verificar confirmacao do pagamento.

**P: Qual o tamanho do pacote?**
R: Menos de 8KB gzipado, sem dependencias em runtime alem do React.

### Comerciais

**P: O pacote e gratuito?**
R: O pacote NPM e publico. Requer um backend ProxyPay configurado para funcionar.

---

## Como Comecar

1. Instale: `npm install proxypay-react`
2. Configure o Provider: `<ProxyPayProvider baseUrl="..." clientId="..." tenantId="...">`
3. Use os componentes: `<PixPayment />`, `<InvoicePayment />`, `<BillingPayment />`

---

## Diferenciais Competitivos

- **Ultra-leve** — Menos de 8KB gzipado sem dependencias externas
- **Componentes prontos** — PIX, Invoice e Billing funcionando em minutos
- **Polling automatico** — Verificacao de status de pagamento PIX sem codigo adicional
- **Hook flexivel** — Use programaticamente sem componentes visuais quando necessario

---

## Contato e Suporte

- **Website**: https://emagine.com.br/proxypay
- **Repositorio**: https://github.com/emaginebr/proxypay-react
- **Suporte**: Entre em contato pelo site da Emagine

---

*Documentacao gerada automaticamente a partir do repositorio GitHub. Ultima atualizacao: 2026-04-10*
