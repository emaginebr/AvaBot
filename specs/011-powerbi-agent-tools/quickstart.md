# Quickstart: Integração do Agente com Power BI

**Feature**: 011-powerbi-agent-tools

## Pré-requisitos no Power BI / Entra ID

1. Ter um aplicativo registrado no Entra ID com client secret. Os dados de teste estão no `.env` (`BI_APP_ID`, `BI_DIRECTORY_ID`, `BI_SECRET_VALUE`); `BI_SECRET_ID` não é usado.
2. No Admin portal do Power BI (tenant settings), habilitar para o aplicativo ou para o grupo de segurança dele:
   - *Service principals can use Fabric/Power BI APIs*;
   - *Dataset Execute Queries REST API*.
3. No workspace, dar ao aplicativo o papel **Contributor**, ou dar permissão **Build** em cada dataset. Com o papel *Viewer*, as consultas falham com `PowerBIEntityNotFound` (ver research R3).

## Configuração do backend

Adicionar ao `.env` e ao `docker-compose*.yml`:

```env
POWERBI_SECRET_ENCRYPTION_KEY=<32 bytes em base64>
```
```yaml
PowerBI__SecretEncryptionKey: ${POWERBI_SECRET_ENCRYPTION_KEY}
```
Para gerar a chave: `openssl rand -base64 32`, ou em PowerShell `[Convert]::ToBase64String((1..32 | % { Get-Random -Max 256 }) -as [byte[]])`.

Aplicar a migração:
```bash
dotnet ef database update --project AvaBot.Infra --startup-project AvaBot.API
```

## Validação manual (cenários da spec)

1. **US1 (credenciais)**: no admin, selecionar o agente, abrir **Power BI**, informar tenant, client e secret e salvar. Recarregar a página: o secret deve aparecer como `••••xxxx`. Clicar em **Testar conexão**. Enquanto o papel for Viewer, o esperado é auth ✓, workspaces ✓ e o passo do dataset ✗, com orientação de permissão.
2. **US2 (datasets e schema)**: **Adicionar dataset**, escolher o workspace "BI ABIPESCA" e o dataset "ABIPESCA - Comércio Internacional" nos selects, preencher a descrição e salvar. Clicar em **Gerar schema** e conferir as tabelas, colunas e medidas e a data da geração.
3. **US4 (descrições)**: adicionar a descrição de uma medida, salvar, gerar o schema de novo e verificar que a descrição foi mantida.
4. **US3 (flag e respostas)**:
   - Tentar ativar a flag sem schema: deve ser bloqueado com explicação.
   - Com schema: ativar, ver o aviso de exposição (FR-024a) e confirmar.
   - Em **Teste do Agente**, perguntar "Quanto exportamos de tilápia?". O agente deve **perguntar o período** (FR-022b).
   - Perguntar "Quanto exportamos de tilápia em 2025?". A resposta deve vir em texto ou lista, sem tabela, e a seção "Consultas Power BI" deve mostrar o DAX executado. Conferir o valor no relatório do Power BI.
   - Fazer uma pergunta da base de conhecimento: a resposta não deve gerar nenhuma consulta ao Power BI.
   - Repetir a pergunta de dados pelo chat web, pelo Telegram e pelo WhatsApp.
5. **Histórico**: em **Power BI → Histórico de consultas**, conferir os registros, com pergunta, DAX, duração, linhas e status.
6. **Flag desligada**: desativar a flag e repetir a pergunta de dados. O agente responde só com a base de conhecimento, e nenhum registro novo aparece no histórico.

## Testes automatizados

```bash
dotnet test AvaBot.Tests
cd frontend && npm run lint
```
