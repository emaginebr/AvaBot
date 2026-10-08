# Feature Specification: Busca Textual no Elasticsearch

**Feature Branch**: `013-lexical-search`  
**Created**: 2026-10-08  
**Status**: Draft  
**Input**: User description: "Alterar a busca da base de conhecimento para que as consultas sejam feitas somente por busca textual no Elasticsearch, sem chamar a API da OpenAI para gerar embedding da pergunta. Manter a geração de respostas do chat como fluxo separado."

## User Scenarios & Testing

### User Story 1 - Buscar conhecimento sem chamada à OpenAI (Priority: P1)

Como pessoa que consulta um agente, quero que minha pergunta seja pesquisada diretamente na base de conhecimento, para receber trechos relevantes sem depender da API da OpenAI durante a busca.

**Why this priority**: A busca na base deve continuar disponível mesmo quando a OpenAI estiver indisponível e não deve depender de uma chamada de modelo para procurar texto.

**Independent Test**: Fazer uma consulta que contenha palavras presentes em documentos indexados enquanto o acesso à OpenAI está indisponível e confirmar que a busca retorna os trechos correspondentes do agente consultado.

**Acceptance Scenarios**:

1. **Given** uma base de conhecimento com documentos indexados e uma pergunta contendo termos encontrados nesses documentos, **When** a pessoa consulta a base, **Then** a busca retorna os trechos correspondentes sem solicitar à OpenAI um embedding da pergunta.
2. **Given** duas bases de conhecimento pertencentes a agentes diferentes, **When** uma consulta é feita para um agente, **Then** somente trechos daquele agente podem ser retornados.
3. **Given** uma pergunta sem correspondências textuais nos documentos, **When** a pessoa consulta a base, **Then** a busca retorna uma lista vazia sem exigir uma chamada à OpenAI.
4. **Given** uma conversa que usa a base de conhecimento, **When** os trechos são recuperados e a resposta é preparada, **Then** a busca permanece independente da OpenAI e a geração da resposta continua sendo uma etapa separada do chat.

### Edge Cases

- A consulta está vazia ou contém somente espaços; o sistema informa que a consulta é obrigatória sem chamar a OpenAI.
- O Elasticsearch está indisponível; o sistema comunica a falha da busca sem convertê-la em falha de autenticação da OpenAI.
- A consulta usa palavras diferentes das usadas no documento para expressar a mesma ideia; a busca textual pode não encontrar o trecho, e a interface não deve afirmar que foi feita busca semântica.
- O agente não tem chave OpenAI configurada; a busca textual da base ainda pode ser realizada. Uma resposta conversacional gerada por modelo, quando solicitada, permanece sujeita à configuração própria do chat.

## Requirements

### Functional Requirements

- **FR-001**: O sistema MUST consultar o conteúdo textual indexado na base de conhecimento sem chamar a OpenAI para vetorizar a pergunta.
- **FR-002**: A busca MUST limitar os resultados ao agente associado à consulta.
- **FR-003**: A busca MUST retornar trechos relevantes por correspondência textual e retornar uma lista vazia quando não houver correspondências.
- **FR-004**: O sistema MUST manter a busca da base como etapa independente da geração de resposta conversacional.
- **FR-005**: O sistema MUST permitir que uma busca direta da base seja concluída sem uma chave OpenAI configurada para o agente.
- **FR-006**: A busca MUST preservar o limite de resultados solicitado e validar consultas vazias antes de executar a pesquisa.
- **FR-007**: Falhas da base de conhecimento MUST ser comunicadas como falhas de busca, sem depender de mensagens ou disponibilidade da OpenAI.
- **FR-008**: O sistema MUST retornar ausência de correspondências textuais como resultado vazio, sem recorrer a uma busca semântica externa.

### Key Entities

- **Consulta de busca**: Texto informado pela pessoa e associado ao agente cuja base deve ser pesquisada.
- **Trecho de conhecimento**: Conteúdo textual indexado e associado a um único agente, que pode ser retornado quando combina com os termos da consulta.
- **Resposta conversacional**: Conteúdo gerado pelo modelo de chat em etapa separada, podendo usar os trechos retornados pela busca como contexto.

## Success Criteria

### Measurable Outcomes

- **SC-001**: 100% das consultas diretas à base com termos presentes nos documentos retornam correspondências sem depender de um serviço externo de geração de linguagem.
- **SC-002**: 100% dos resultados de uma consulta pertencem ao agente selecionado.
- **SC-003**: 100% das consultas sem correspondência retornam uma lista vazia sem depender de um serviço externo de geração de linguagem.
- **SC-004**: Em 100% das consultas, a quantidade de trechos retornados respeita o limite solicitado.

## Assumptions

- A busca desta feature é textual, baseada nos termos da consulta e no conteúdo indexado; busca semântica por similaridade de significado fica fora do escopo.
- Esta feature remove a dependência de OpenAI na etapa de consulta da base. A geração de embeddings durante a ingestão de documentos não é alterada por esta especificação.
- O modelo de chat ainda pode ser chamado depois da busca para gerar respostas; essa chamada não faz parte da rotina de recuperação dos trechos.
- O índice pode continuar contendo embeddings já gerados, mesmo que a busca textual desta feature não os utilize.
