# Feature Specification: Chave OpenAI por Agente

**Feature Branch**: `012-openai-agent-key`  
**Created**: 2026-10-08  
**Status**: Draft  
**Input**: User description: "A API_KEY da openAI está fixa no código, isso deve ser uma configuração para cada agente. Ao editar o agente, Modelos de IA, deve ter a opção de informar a API_KEY da OpenAI. Deve também um botão de diagnostico que abre um modal e testar se a conexão com a API da OpenAI com essa API_KEY está funcionando."

## Clarifications

### Session 2026-10-08

- Q: Como o campo deve se comportar quando o agente já tem uma chave salva? → A: Ocultar a chave ao reabrir, manter a credencial ao salvar sem alterações e removê-la somente por uma ação explícita.
- Q: O que deve contar como uma conexão funcional no diagnóstico? → A: Confirmar que a chave autentica com sucesso, sem gerar uma resposta.

## User Scenarios & Testing

### User Story 1 - Configurar chave OpenAI do agente (Priority: P1)

Como pessoa que administra agentes, quero informar a chave da OpenAI nas configurações de Modelos de IA de cada agente, para que cada agente use suas próprias credenciais.

**Why this priority**: Sem uma chave configurável por agente, os agentes dependem de uma credencial fixa e não podem ser configurados de forma independente.

**Independent Test**: Editar dois agentes e salvar uma chave diferente para cada um; ao reabrir cada agente, confirmar que sua própria configuração está presente e que nenhum agente mostra a chave do outro.

**Acceptance Scenarios**:

1. **Given** um agente existente aberto para edição em Modelos de IA, **When** a pessoa informa uma chave OpenAI e salva, **Then** a chave fica associada somente àquele agente.
2. **Given** um agente com uma chave já salva, **When** a pessoa reabre suas configurações, **Then** a interface indica que há uma chave configurada sem expor seu valor completo.
3. **Given** um agente sem chave OpenAI configurada, **When** a pessoa salva a configuração sem preencher esse campo, **Then** o agente permanece sem chave própria e a interface informa que a credencial é necessária para usar recursos OpenAI.
4. **Given** um agente com uma chave salva, **When** a pessoa salva outras alterações sem modificar a credencial, **Then** a chave existente é mantida; para removê-la, a pessoa usa uma ação explícita de remoção.

### User Story 2 - Diagnosticar conexão OpenAI (Priority: P2)

Como pessoa que administra agentes, quero testar a chave OpenAI informada por meio de um diagnóstico, para confirmar que a conexão funciona antes de depender dela.

**Why this priority**: O diagnóstico torna problemas de credencial ou conectividade identificáveis durante a configuração.

**Independent Test**: Abrir o diagnóstico com uma chave válida e com uma chave inválida e confirmar que o modal comunica sucesso ou falha de forma compreensível.

**Acceptance Scenarios**:

1. **Given** uma chave informada para o agente, **When** a pessoa aciona Diagnóstico, **Then** um modal é aberto e inicia o teste usando a chave daquele agente.
2. **Given** que a chave autentica com sucesso sem gerar uma resposta, **When** o resultado é apresentado, **Then** o modal informa que a conexão está funcionando.
3. **Given** que a chave é inválida, não tem acesso ou o serviço está indisponível, **When** o teste termina, **Then** o modal informa que o diagnóstico falhou e apresenta uma explicação adequada sem revelar a chave.
4. **Given** que não há chave informada nem salva, **When** a pessoa aciona Diagnóstico, **Then** a interface solicita uma chave antes de iniciar o teste.

### Edge Cases

- A chave contém espaços antes ou depois; a configuração deve tratar esses espaços sem armazenar acidentalmente uma credencial diferente da informada.
- O teste demora ou não recebe resposta; o modal deve indicar que o diagnóstico está em andamento e permitir reconhecer uma falha de comunicação.
- A pessoa altera a chave no formulário e executa o diagnóstico antes de salvar; o teste deve usar a chave atualmente informada.
- A pessoa limpa uma chave salva; ao salvar, o agente deve ficar sem chave própria e a credencial removida não deve continuar utilizável por esse agente.
- A resposta de erro do serviço inclui detalhes técnicos ou conteúdo sensível; a interface não deve exibir a chave nem dados de autenticação.

## Requirements

### Functional Requirements

- **FR-001**: O sistema MUST permitir informar uma chave de API OpenAI nas configurações de Modelos de IA de cada agente.
- **FR-002**: O sistema MUST associar a chave configurada exclusivamente ao agente que está sendo editado.
- **FR-003**: O sistema MUST persistir a configuração da chave e recuperá-la ao editar novamente o agente, sem expor o valor completo na interface.
- **FR-004**: O sistema MUST permitir substituir a chave OpenAI de um agente e removê-la somente por uma ação explícita; deixar o campo inalterado ao salvar MUST manter a chave existente.
- **FR-005**: O sistema MUST usar a chave configurada para o agente nas operações OpenAI desse agente, sem compartilhar a configuração entre agentes.
- **FR-006**: O sistema MUST oferecer uma ação de Diagnóstico na seção Modelos de IA que abra um modal para confirmar a autenticação OpenAI com a chave daquele agente, sem gerar uma resposta.
- **FR-007**: O diagnóstico MUST permitir testar uma chave ainda não salva que esteja preenchida no formulário.
- **FR-008**: O modal MUST apresentar um resultado de sucesso ou falha compreensível, incluindo orientação adequada quando nenhuma chave está disponível.
- **FR-009**: O sistema MUST impedir que a chave completa apareça no modal, em mensagens de erro ou em outras respostas visíveis à pessoa usuária.
- **FR-010**: O sistema MUST comunicar que recursos OpenAI não podem ser utilizados por um agente sem chave própria configurada.

### Key Entities

- **Agente**: Configuração individual do agente, à qual pertence uma credencial OpenAI opcional.
- **Credencial OpenAI do agente**: Chave usada para autenticar as operações OpenAI de um único agente; pode ser substituída ou removida.
- **Diagnóstico de conexão**: Resultado transitório de um teste da credencial informada, com estado de execução e resultado compreensível.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Em uma verificação com dois agentes, 100% das chaves salvas permanecem associadas apenas ao agente correspondente após salvar e reabrir as configurações.
- **SC-002**: Uma pessoa consegue localizar, informar e salvar a chave OpenAI a partir de Modelos de IA sem ajuda externa em até 2 minutos.
- **SC-003**: Em 100% dos diagnósticos concluídos, a pessoa consegue distinguir conexão funcional de falha a partir do resultado no modal.
- **SC-004**: Em todos os estados da interface do fluxo, a chave completa não fica visível após ser salva nem é incluída em mensagens apresentadas à pessoa usuária.

## Assumptions

- A configuração é opcional para agentes que não usam recursos OpenAI; quando esses recursos forem usados, uma chave própria deve estar configurada.
- A chave deve ser protegida como credencial sensível e o sistema deve evitar sua exposição após o salvamento.
- O diagnóstico deve refletir a chave presente no formulário naquele momento, inclusive quando ainda não foi salva.
- O resultado de diagnóstico deve explicar a categoria da falha em linguagem compreensível sem depender da exposição de mensagens brutas do provedor.
