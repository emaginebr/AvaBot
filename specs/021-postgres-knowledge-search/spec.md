# Feature Specification: Base de conhecimento no banco principal (sem Elasticsearch)

**Feature Branch**: `021-postgres-knowledge-search`
**Created**: 2026-11-25
**Status**: Draft
**Input**: User description: "pode trocar para o Postgres, abre a feature 021"

## Contexto

Os trechos (chunks) dos arquivos de conhecimento dos agentes ficam hoje em um serviço de busca separado, o Elasticsearch. Os agentes consultam esses trechos para responder no chat, no widget, no Telegram, no WhatsApp, na busca do painel e no MCP.

A produção está migrando para o cluster Kubernetes compartilhado (feature 020). Nele, o Elasticsearch não cabe: pede 640Mi de memória e os nós têm cerca de 450Mi livres. No servidor antigo, ele roda com a porta pública aberta e sem autenticação.

A busca usada hoje é apenas textual: filtra por agente e retorna os trechos mais relevantes. Os vetores (embeddings) são gravados, mas nenhuma consulta os utiliza. O banco PostgreSQL principal, que já é gerenciado e tem backup, consegue fazer essa busca textual.

Esta feature move o armazenamento e a busca dos trechos para o banco principal e remove o Elasticsearch do projeto.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Agente continua respondendo com base nos arquivos (Priority: P1)

Um visitante conversa com um agente (chat, widget, Telegram ou WhatsApp) e recebe respostas fundamentadas nos arquivos de conhecimento desse agente, como hoje.

**Why this priority**: é a função central do produto; se a busca parar, os agentes param de responder com base no conhecimento.

**Independent Test**: com um agente que tem arquivos processados, fazer perguntas cujo conteúdo está nos arquivos e conferir que a resposta usa esse conteúdo.

**Acceptance Scenarios**:

1. **Given** um agente com arquivos processados, **When** o visitante pergunta algo coberto por um arquivo, **Then** o agente recebe os trechos relevantes daquele agente e responde com base neles.
2. **Given** dois agentes com arquivos diferentes, **When** um visitante conversa com o agente A, **Then** nenhum trecho do agente B é usado.
3. **Given** uma pergunta sem nenhuma correspondência nos arquivos, **When** a busca é feita, **Then** nenhum trecho é retornado (e não todos os trechos do agente).

---

### User Story 2 - Administrador gerencia arquivos e testa a busca (Priority: P1)

O administrador envia, reprocessa e exclui arquivos de conhecimento e usa a tela "Busca na base" e o teste do agente, com o mesmo comportamento de hoje.

**Why this priority**: sem isso não há como manter a base dos agentes.

**Independent Test**: enviar um arquivo, aguardar o processamento, buscar um termo dele, excluir o arquivo e buscar de novo.

**Acceptance Scenarios**:

1. **Given** um arquivo enviado, **When** o processamento termina, **Then** seus trechos aparecem na busca do agente.
2. **Given** um arquivo reprocessado, **When** o processamento termina, **Then** apenas os trechos novos existem (sem duplicação dos antigos).
3. **Given** um arquivo excluído, **When** o administrador busca um termo exclusivo dele, **Then** nenhum trecho dele é retornado.
4. **Given** um agente excluído, **When** a exclusão termina, **Then** todos os trechos do agente são removidos.
5. **Given** a busca indisponível por erro do banco, **When** o administrador busca, **Then** vê uma mensagem de erro (e não uma lista vazia que pareça "sem conteúdo").

---

### User Story 3 - Base atual preservada na migração (Priority: P1)

Depois da troca, os agentes que já existem em produção continuam com toda a sua base de conhecimento, sem que o administrador precise reenviar arquivos.

**Why this priority**: perder a base de produção deixaria os agentes sem resposta.

**Independent Test**: comparar, por agente, a quantidade de arquivos processados antes da troca com a quantidade de arquivos que têm trechos depois da troca.

**Acceptance Scenarios**:

1. **Given** os arquivos já processados em produção, **When** a migração é executada, **Then** cada um deles volta a ter trechos pesquisáveis no banco principal.
2. **Given** um arquivo cujo reprocessamento falha durante a migração, **When** a migração termina, **Then** o arquivo fica marcado com erro, visível no painel, e pode ser reprocessado depois.

---

### User Story 4 - Produção sem o serviço de busca separado (Priority: P2)

A equipe publica o AvaBot no cluster sem o Elasticsearch, e o serviço exposto e sem autenticação do servidor antigo pode ser desligado.

**Why this priority**: destrava a migração para o cluster (feature 020) e elimina um risco de segurança, mas depende das histórias anteriores.

**Independent Test**: deploy de produção no cluster concluído com sucesso, sem nenhum componente de Elasticsearch, e com os fluxos das histórias 1 e 2 funcionando.

**Acceptance Scenarios**:

1. **Given** a nova versão publicada, **When** o deploy termina, **Then** todos os componentes ficam prontos e nenhum recurso de Elasticsearch existe no cluster.
2. **Given** a nova versão, **When** a aplicação inicia sem configuração de Elasticsearch, **Then** ela inicia normalmente.

---

### Edge Cases

- **Termos com acentos ou sem eles**, em maiúsculas ou minúsculas ("Ação", "acao", "AÇÃO"): a busca deve encontrar o mesmo conteúdo. O português é o idioma principal dos arquivos.
- **Pergunta só com palavras muito comuns** ("o que é isso?"): pode não retornar nada; o comportamento não deve travar nem retornar todos os trechos.
- **Arquivos grandes**, com centenas de trechos: a busca continua rápida.
- **Arquivo em processamento** durante a busca: retorna os trechos já gravados, sem erro.
- **Ferramenta de calibração** (`AvaBot.Calibration`): usa uma base local própria e precisa continuar funcionando.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Os trechos dos arquivos de conhecimento MUST ser armazenados no banco principal da aplicação, associados ao agente, ao arquivo e à sua ordem no arquivo.
- **FR-002**: A busca MUST considerar somente os trechos do agente consultado e retornar, no máximo, a quantidade pedida (padrão 5), ordenada por relevância textual.
- **FR-003**: A busca MUST retornar zero trechos quando nenhum trecho corresponder aos termos da pergunta.
- **FR-004**: A busca MUST ignorar diferenças de acentuação e de maiúsculas/minúsculas.
- **FR-005**: Enviar, reprocessar e excluir arquivos, e excluir agentes, MUST manter os trechos consistentes: sem trechos órfãos e sem duplicação após o reprocessamento.
- **FR-006**: Uma falha da busca MUST ser reportada como erro, nunca como resultado vazio.
- **FR-007**: Os vetores (embeddings) gerados na ingestão MUST continuar sendo gravados junto com cada trecho, para permitir busca semântica no futuro. Eles não são usados na busca nesta feature.
- **FR-008**: MUST existir um procedimento de migração que reprocesse, a partir do conteúdo já guardado no banco, todos os arquivos processados, sem exigir reenvio pelo administrador. Ele também MUST informar quantos arquivos foram migrados e quantos falharam.
- **FR-009**: Toda dependência do Elasticsearch MUST ser removida: código, configuração, testes, manifestos do cluster e pipeline de deploy. A aplicação MUST iniciar sem nenhuma configuração dele.
- **FR-010**: Os pontos de uso da base MUST manter o comportamento atual: chat, widget, Telegram, WhatsApp, busca e teste do agente no painel, e ferramentas MCP.
- **FR-011**: A documentação de deploy (`deploy/k8s/README.md`) MUST deixar de prever o Elasticsearch e a sua migração por reindexação.

### Key Entities

- **Trecho de conhecimento**: parte de um arquivo de conhecimento. Contém o agente, o arquivo, a ordem no arquivo, o texto, o vetor (embedding) e a data de criação. Pertence a um arquivo e a um agente; é removido quando o arquivo ou o agente é removido.
- **Arquivo de conhecimento** (existente): já guarda o conteúdo original e o status de processamento; continua sendo a fonte para regenerar os trechos.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos arquivos processados em produção antes da troca têm trechos pesquisáveis depois da migração, ou ficam marcados com erro e listados no relatório da migração.
- **SC-002**: Em um conjunto de pelo menos 10 perguntas de referência por agente ativo, os agentes respondem com base nos arquivos tão bem quanto antes da troca. A comparação usa a calibração existente e não pode haver nenhuma pergunta que antes era respondida e passe a ficar sem trecho relevante.
- **SC-003**: A busca na base responde em menos de 1 segundo para agentes com até 5.000 trechos.
- **SC-004**: O deploy de produção no cluster conclui com sucesso sem reservar memória para um serviço de busca separado (economia de pelo menos 640Mi de memória reservada).
- **SC-005**: Nenhuma referência ao Elasticsearch permanece no código, na configuração ou nos manifestos do projeto, exceto em documentos históricos de features anteriores.
- **SC-006**: Os testes de unidade e o build passam.

## Assumptions

- O conteúdo original de cada arquivo já está guardado no banco, então a migração reprocessa os arquivos a partir dele, sem copiar dados do Elasticsearch antigo. O reprocessamento gera novamente os vetores, com um custo pequeno de uso da API de embeddings, considerado aceitável.
- A busca textual nativa do banco atende ao nível de qualidade atual, que usa só busca textual. A busca semântica por vetor fica fora do escopo.
- O banco gerenciado de produção é PostgreSQL e permite os recursos de busca textual e de ignorar acentos. Se a extensão de remoção de acentos não estiver disponível, há uma alternativa equivalente na aplicação.
- A migração roda uma vez, depois do deploy da nova versão e antes da troca de tráfego na Cloudflare. Até a troca, o site continua no servidor antigo, sem impacto para os usuários.
- O Elasticsearch do servidor antigo é desligado depois da troca validada, como parte do desligamento do ambiente antigo (fora do código deste repositório).
- O RabbitMQ e o WPP Connect não são afetados.
