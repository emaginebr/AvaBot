# Quickstart: Chave OpenAI por Agente

## Configurar um agente

1. Autentique-se no painel administrativo e abra a edição do agente.
2. Na seção **Modelo de IA**, informe a chave OpenAI e salve.
3. Confirme que a interface indica credencial configurada sem exibir o valor salvo.
4. Para trocar a chave, informe o novo valor e salve. Para removê-la, use a ação explícita de remoção.
5. Salvar outras alterações sem tocar na credencial deve preservá-la.

## Diagnosticar

1. Acione **Diagnóstico** na seção Modelo de IA.
2. Se o campo contém uma chave nova, o diagnóstico testa esse valor sem salvá-lo.
3. Se o campo está inalterado e já existe chave salva, o diagnóstico usa a credencial do agente.
4. O modal apresenta sucesso quando a autenticação é aceita e uma mensagem segura quando falha. O diagnóstico não gera uma resposta.

## Regras operacionais

- Configure `PowerBI__SecretEncryptionKey` em cada ambiente que salva ou usa credenciais protegidas; o valor deve ser uma chave base64 de 32 bytes estável e não pode ser armazenado no frontend ou no controle de versão.
- Configure uma chave OpenAI individual para cada agente que usa chat, busca semântica ou ingestão de arquivos. Não haverá fallback para `OpenAI:ApiKey`.
- Agentes já existentes ficam sem configuração OpenAI própria após a atualização e precisam ser configurados no painel antes que suas operações OpenAI funcionem.
- Os vetores já indexados não precisam ser regenerados só pela troca de credencial; se o modelo de embedding mudar, essa troca não está coberta por esta feature.

## Verificações de aceitação

- Dois agentes mantêm chaves separadas após salvar e reabrir.
- `GET /agents` e rotas públicas de agente nunca devolvem a chave ou o texto cifrado; mostram no máximo o indicador de configuração permitido pelo contrato.
- Chave vazia sem ação de remoção preserva a existente; remoção explícita limpa a credencial.
- Diagnóstico aceita tanto a chave não salva do formulário como uma credencial já persistida, confirma autenticação sem gerar texto e não expõe o segredo nos erros.
- Chat, streaming, teste, busca e ingestão usam a chave correspondente ao agente.
- Inspecionar os logs do navegador e backend confirma que nenhum valor de chave é registrado.
