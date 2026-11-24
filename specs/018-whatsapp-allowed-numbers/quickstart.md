# Quickstart: validar o controle de acesso do WhatsApp

**Feature**: `018-whatsapp-allowed-numbers`

Os passos 1–2 rodam localmente. Os passos 3–8 precisam de PostgreSQL, da API e de um WPP Connect com um número pareado: ambiente Docker ou produção, já que o ambiente local não tem banco nem docker. Use dois celulares: **A** (será autorizado) e **B** (não autorizado).

## 1. Build e testes unitários

```powershell
dotnet build AvaBot.sln
dotnet test AvaBot.Tests --filter "FullyQualifiedName~Whatsapp"
```

Esperado: passam `WhatsappPhoneTest`, `WhatsappReplyParserTest`, `WhatsappSenderTest`, `WhatsappAccessServiceTest` (os 16 casos da tabela de verdade), `WhatsappServiceTest` (ordem do webhook) e `WhatsappAccessToolsTest`.

## 2. Frontend

```powershell
cd frontend; npm run lint; npm run build
```

## 3. Migração

Suba a API: a migração `AddWhatsappAccessControl` é aplicada no startup. Confira:

```sql
SELECT whatsapp_access_mode, count(*) FROM avabot_agents GROUP BY 1;   -- todos = 1
\d avabot_whatsapp_contacts
\d avabot_whatsapp_access_requests
\d avabot_whatsapp_sender_states
```

## 4. Modo "Todos" não muda nada (SC-007)

Com o agente pareado e em modo "Todos", envie "oi" de **B**: a IA responde como antes.

## 5. Contato manual e restrição (US1, US4, US5)

1. Painel → WhatsApp → Contatos autorizados → adicione **A** como `(11) 9XXXX-XXXX`, sem `+55`.
2. Mude o modo para "Somente contatos autorizados" e salve.
3. **A** envia "oi" → a IA responde.
4. **B** envia "oi" → recebe só a frase de oferta. Confira no log `[WhatsApp Acesso] ... etapa=Offer` e que nenhuma mensagem nova aparece em Sessões para **B**.
5. **B** envia "oi" de novo em seguida → nada é enviado (`etapa=Ignore:limite-1h`).

## 6. Solicitação, LID e aprovação (US2, US3)

1. **B** responde "Sim" → recebe a confirmação. Painel → Solicitações → Pendentes mostra **B** com o nome de perfil.
2. Se o log mostrar o remetente de **B** como `...@lid`, verifique a resolução: `GET {WPP}/api/{slug}/contact/pn-lid/{lid}` deve devolver `phoneNumber`. Se a rota não existir na imagem, a solicitação aparece sem telefone ("Não identificado") e a aprovação continua funcionando pelo identificador (research R3).
3. Aprove **B** (edite o nome) → ele aparece em Contatos autorizados com origem "Aprovação".
4. **B** envia uma pergunta → a IA responde.

## 7. Rejeição e recusa

1. Remova **B** dos contatos. **B** envia "oi" após 1 h (ou apague a linha de **B** em `avabot_whatsapp_sender_states` para testar na hora) → recebe a oferta.
2. **B** responde "não" → recebe a frase de recusa e nenhuma solicitação é criada.
3. Repita a oferta, responda "sim" e rejeite no painel → as mensagens seguintes de **B** são ignoradas em silêncio.

## 8. Frases personalizadas e MCP

1. Altere a frase de oferta, salve e dispare a oferta para **B**: chega o texto novo. "Restaurar padrão" volta ao texto original.
2. Com um cliente MCP conectado (feature 017), chame `obter_acesso_whatsapp`, `listar_solicitacoes_whatsapp` com `status: "pendente"` e `aprovar_solicitacao_whatsapp`. Os resultados batem com o painel.
3. Grupo: mencione o agente num grupo a partir de um número não autorizado → nada é respondido no grupo.
