# Quickstart: Contas de usuário e autenticação

**Feature**: 016-user-accounts-auth

## Configuração

Nenhuma variável nova. Antes do primeiro deploy desta versão, confira no `.env`:

```env
AVABOT_USERNAME=<login atual do administrador>
AVABOT_PASSWORD=<senha atual>
AVABOT_JWT_SECRET=<segredo atual, inalterado>
```

Opcional: `Auth__TokenExpirationMinutes` (padrão 43200 = 30 dias).

Aplicar a migração:
```bash
dotnet ef database update --project AvaBot.Infra --startup-project AvaBot.API
```

Subir a API. No log deve aparecer o bootstrap: `Conta do administrador criada (email=...) e N agente(s) atribuído(s)`. Se a base tiver agentes e as credenciais não estiverem configuradas, a API **não sobe** e o log explica o motivo (contrato `auth-api.md`).

Depois do primeiro deploy, `AVABOT_USERNAME`/`AVABOT_PASSWORD` podem sair do `.env`; o login passa a ser pelo banco.

## Validação manual (cenários da spec)

1. **US4 (migração)**: com a base atual, entrar em `/login` com o usuário/senha de hoje (o campo agora se chama e-mail; use o mesmo valor). Ver todos os agentes antigos na lista.
2. **US1 (criar conta)**: sair, abrir `/register`, criar "Ana" com e-mail e senha de 8+ caracteres. Deve cair direto no painel vazio, sem login. Tentar criar de novo com o mesmo e-mail em maiúsculas: "Este e-mail ja esta em uso". Senha de 5 caracteres: a página bloqueia.
3. **US2 (isolamento)**: com Ana, criar um agente. Sair, entrar com a conta do administrador: o agente da Ana não aparece. Copiar o id de um agente do administrador e, logado como Ana, chamar `PUT /agents/{id}`, `GET /sessions/agents/{id}`, `GET /files/{id}`, `GET /powerbi/{slug}/config`, `GET /telegram/{id}/webhook-info`, `GET /whatsapp/{slug}/status`, `POST /agents/{id}/test`: todos `404` com "Agente nao encontrado". Abrir `/chat/{slug}` de um agente do administrador sem login: funciona.
4. **US3 (30 dias)**: entrar, fechar o navegador, reabrir: continua no painel. Para simular expiração, subir a API com `Auth__TokenExpirationMinutes=1`, entrar, esperar 1 min e clicar em qualquer menu: volta ao login com "Sessão expirada". Clicar em "Sair": volta ao login e `localStorage` sem `avabot:auth-token`.
5. **US5 (conta)**: em `/admin/account`, trocar o nome e ver o cabeçalho atualizar. Trocar a senha com a atual errada: aviso, nada muda. Trocar com a atual certa, sair e entrar com a nova; a antiga dá "Credenciais invalidas".
6. **FR-007 (bloqueio)**: errar a senha 5 vezes; a 6ª tentativa, mesmo com a senha certa, responde com o aviso de 15 minutos.
7. **FR-017**: na página de login, o texto "Esqueceu a senha? Fale com o suporte" está visível e não há fluxo de recuperação.

## Testes automatizados

```bash
dotnet test AvaBot.Tests
cd frontend && npm run lint && npm run build
```

Testes de API (precisam da API rodando e do `AvaBot.Tests.API/appsettings.json` local com `ApiSettings.Email`/`Password`):
```bash
dotnet test AvaBot.Tests.API
```

## Coleção Bruno

`bruno/Auth`: `Login.bru` (corpo `email`/`password`, token em `dados.token`), `Register.bru`, `Me.bru`, `Update Me.bru`, `Change Password.bru`.
