# Quickstart: Fluxo Start Session

**Branch**: `006-chat-start-session` | **Date**: 2026-04-08

## Pre-requisitos

1. Backend Avachat rodando com agente "Bia" (slug: `bia`) ativo
2. Agente Bia com collectName, collectEmail e collectPhone ativados
3. Variaveis de ambiente: `VITE_API_URL` e `VITE_WS_URL`

## Verificacao

### Cenario 1: Fluxo completo (3 campos)

1. Acessar `http://localhost:5173/`
2. Clicar no balao de chat
3. Widget abre com: "Oi, eu sou a Bia. Em que posso ajudar?"
4. Assistente pergunta: "Qual seu nome?"
5. Digitar "Maria" e enviar
6. Assistente pergunta: "Bem vindo Maria, qual seu email?"
7. Digitar "maria@email.com" e enviar
8. Assistente pergunta: "Qual seu numero de telefone? Com DDD"
9. Digitar "11999998888" e enviar
10. Assistente exibe: "Muito obrigado pelas informacoes, agora em que posso ajudar?"
11. Digitar uma pergunta e verificar resposta em streaming

### Cenario 2: Email invalido

1. Na etapa do email, digitar "invalido"
2. Verificar mensagem de erro de validacao
3. Digitar email valido e prosseguir

### Cenario 3: Sem coleta de dados

1. Desativar todos os campos de coleta no agente Bia
2. Abrir o chat
3. Verificar que apos a boas-vindas, a sessao inicia automaticamente
4. Assistente exibe: "Muito obrigado pelas informacoes, agora em que posso ajudar?"
5. Enviar mensagem e receber resposta

### Cenario 4: Coleta parcial (so nome)

1. Ativar apenas collectName no agente Bia
2. Abrir o chat
3. Verificar que so pergunta o nome, pula email e telefone
4. Sessao inicia apos informar o nome
