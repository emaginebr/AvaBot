# Feature Specification: Diagnóstico detalhado e retentativas de consultas Power BI

**Feature Branch**: `014-powerbi-error-retry`  
**Created**: 2026-10-08  
**Status**: Draft  
**Input**: User description: "Armazenar toda a mensagem de erro que o Power BI retorna. Caso a consulta dê erro, retornar o erro detalhado ao modelo de IA para ele corrigir a consulta e tentar novamente, com limite de 5 tentativas configurado no appsettings."

## Clarifications

### Session 2026-10-08

- Q: Quais falhas devem consumir tentativas de correção da consulta? → A: Erros de DAX e falhas temporárias do serviço também devem ser retentados; falhas de autenticação e permissão não entram nesse fluxo.
- Q: Como tratar falhas temporárias do serviço? → A: Repetir automaticamente a mesma DAX, contando cada execução no limite; erros de DAX são enviados ao modelo para correção.

## User Scenarios & Testing

### User Story 1 - Consultar BI com recuperação automática de erros (Priority: P1)

Como usuário que faz uma pergunta com dados do BI, quero que o agente receba os detalhes de uma consulta rejeitada e tente corrigi-la, para obter a resposta sem precisar interpretar ou reenviar a consulta manualmente.

**Why this priority**: Erros de DAX são comuns e, sem detalhes, o modelo não consegue diagnosticar nem corrigir a consulta.

**Independent Test**: Simular uma consulta rejeitada com uma mensagem detalhada e depois uma consulta corrigida; verificar que o modelo recebeu o erro completo antes da segunda execução e que a resposta final usa o resultado da consulta bem-sucedida.

**Acceptance Scenarios**:

1. **Given** uma consulta DAX rejeitada pelo Power BI com código e detalhes de erro, **When** o erro é devolvido ao modelo, **Then** o modelo recebe a mensagem completa, incluindo os detalhes aninhados disponíveis, antes de decidir se cria outra consulta.
2. **Given** uma consulta corrigível rejeitada e ainda restam tentativas, **When** o modelo recebe o erro, **Then** pode enviar uma nova consulta corrigida ao BI.
3. **Given** uma nova consulta corrigida aceita pelo Power BI, **When** ela é executada, **Then** o agente encerra as retentativas e responde usando os dados retornados.
4. **Given** uma consulta rejeitada por uma falha temporária do serviço e ainda restam tentativas, **When** a falha é identificada, **Then** o sistema reexecuta automaticamente a mesma DAX sem solicitar ao modelo uma consulta alterada nem encaminhar cada erro intermediário ao modelo.
5. **Given** todas as repetições automáticas de uma falha temporária esgotaram o orçamento, **When** o executor encerra, **Then** o diagnóstico da última falha é devolvido ao modelo para compor a resposta final sem iniciar outra consulta.

---

### User Story 2 - Consultar erros completos no Histórico do Power BI (Priority: P1)

Como administrador do agente, quero consultar o diagnóstico completo de cada falha no Histórico de consultas do Power BI, junto à pergunta e à DAX que falhou, para identificar problemas sem depender de logs externos ou reproduzir a pergunta.

**Why this priority**: A mensagem completa permite distinguir erros de sintaxe, nomes inválidos e outras falhas da consulta.

**Independent Test**: Executar uma consulta que retorne uma mensagem com mais de 2.000 caracteres e detalhes aninhados; verificar que o Histórico de consultas do Power BI apresenta o texto completo associado à pergunta e à DAX correspondente à falha.

**Acceptance Scenarios**:

1. **Given** uma resposta de erro do Power BI com mensagem e detalhes aninhados, **When** a falha é registrada e o administrador abre o Histórico de consultas do Power BI, **Then** todos os detalhes de erro disponíveis são exibidos junto à pergunta e à DAX da tentativa, sem truncamento em 2.000 caracteres.
2. **Given** uma falha de consulta que inclui informação confidencial de credencial, **When** ela é registrada ou repassada ao modelo, **Then** a credencial permanece redigida conforme as regras de segurança existentes.

---

### User Story 3 - Configurar o limite de tentativas (Priority: P2)

Como operador da aplicação, quero configurar quantas vezes o agente pode executar uma consulta de BI por mensagem, para limitar o custo e o tempo de recuperação automática.

**Why this priority**: Uma barreira explícita impede ciclos de correção sem fim e permite ajustar o comportamento operacionalmente.

**Independent Test**: Configurar um limite menor que o padrão, provocar falhas consecutivas e verificar que o número de execuções não excede o valor configurado.

**Acceptance Scenarios**:

1. **Given** o limite configurado como 5, **When** todas as consultas tentadas falham, **Then** no máximo cinco consultas são executadas para a mensagem, contando a consulta inicial.
2. **Given** um limite configurado menor que 5, **When** todas as consultas falham, **Then** o agente para ao atingir esse limite e não chama novamente a ferramenta de consulta.
3. **Given** que o limite de tentativas foi atingido, **When** o agente responde ao usuário, **Then** informa que não conseguiu concluir a consulta e apresenta o diagnóstico detalhado disponível, sem iniciar outra execução.
4. **Given** uma consulta bem-sucedida antes do limite, **When** o resultado retorna, **Then** nenhuma tentativa adicional é iniciada.

### Edge Cases

- A resposta de erro pode trazer mensagem no nível principal, em uma ou mais estruturas aninhadas, ou sem um formato JSON reconhecível; o diagnóstico disponível deve ser preservado sem substituição por uma mensagem genérica quando houver texto útil.
- Uma falha sem mensagem legível deve ser registrada e devolvida com um diagnóstico alternativo claro, indicando que o serviço não forneceu detalhes legíveis.
- Erros temporários do serviço, como indisponibilidade ou limitação de taxa, são elegíveis para retentativa automática da mesma DAX e contam para o mesmo limite configurado; erros de autenticação e permissão não são elegíveis.
- Uma configuração ausente ou inválida usa o padrão de cinco tentativas; valor válido deve ser maior que zero.
- A execução que esgota o limite continua registrada como erro e seu diagnóstico é o que o agente usa na resposta final.
- Consultas de leitura de schema não contam como execução de consulta BI para esse limite.
- A falha de uma retentativa também fica registrada como uma chamada distinta no histórico, preservando a pergunta original e a DAX usada naquela chamada.

## Requirements

### Functional Requirements

- **FR-001**: O sistema MUST preservar a mensagem completa de erro fornecida pelo Power BI, incluindo código, mensagem principal e detalhes aninhados disponíveis.
- **FR-002**: O sistema MUST persistir o diagnóstico completo de cada consulta que falhar e disponibilizá-lo integralmente no Histórico de consultas do Power BI, junto à pergunta e à DAX da tentativa correspondente, sem truncamento em 2.000 caracteres.
- **FR-003**: O sistema MUST devolver ao modelo de IA o diagnóstico detalhado de uma falha de DAX, preservando código e contexto útil para revisar a consulta; após esgotar retries automáticos de falhas temporárias, MUST devolver o diagnóstico da falha mais recente para compor a resposta final.
- **FR-004**: Após receber um erro corrigível de DAX, o modelo MUST poder enviar uma nova consulta corrigida enquanto houver tentativas disponíveis.
- **FR-004a**: Após uma falha temporária elegível do serviço, o sistema MUST repetir automaticamente a mesma DAX enquanto houver tentativas disponíveis, sem solicitar ao modelo uma consulta alterada.
- **FR-005**: O sistema MUST interromper novas execuções de consulta quando uma execução tiver sucesso ou quando o limite de tentativas configurado for alcançado.
- **FR-006**: O limite MUST contar todas as execuções da ferramenta `consultar_bi` em uma mensagem, incluindo a tentativa inicial, e MUST ser independente de chamadas para listar schema.
- **FR-006a**: O sistema MUST permitir retentativas após erros de DAX e falhas temporárias do serviço, incluindo indisponibilidade e limitação de taxa, e MUST NOT repetir automaticamente consultas após falhas de autenticação ou permissão.
- **FR-007**: O sistema MUST permitir configurar o limite de tentativas por meio da configuração da aplicação `appsettings`, com valor padrão de 5 quando a configuração estiver ausente ou inválida.
- **FR-008**: O sistema MUST assegurar que o limite efetivo seja maior que zero.
- **FR-009**: Ao atingir o limite sem sucesso, o agente MUST informar que a consulta não foi concluída e usar o erro detalhado mais recente para explicar a falha; MUST NOT iniciar outra tentativa.
- **FR-010**: O sistema MUST continuar redigindo credenciais e segredos segundo as regras de segurança existentes, tanto no histórico quanto nas mensagens devolvidas ao modelo.
- **FR-011**: O sistema MUST preservar, em registros separados, o DAX e o diagnóstico correspondente a cada tentativa falha.

### Key Entities

- **Tentativa de consulta BI**: Uma execução individual da ferramenta de consulta, associada à pergunta do usuário, dataset, DAX, resultado ou diagnóstico de erro.
- **Limite de tentativas BI**: Configuração operacional que determina o máximo de execuções da consulta por mensagem.
- **Diagnóstico de erro Power BI**: Código, mensagem e detalhes que o Power BI retorna para explicar uma consulta rejeitada.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Em 100% das falhas simuladas com mensagem legível do Power BI, o Histórico de consultas do Power BI contém a mensagem e todos os detalhes de erro devolvidos, associados à pergunta e à DAX correspondente, sem truncamento de aplicação.
- **SC-002**: Em 100% dos erros de DAX, o modelo recebe o diagnóstico da falha anterior antes de enviar uma consulta corrigida; em falhas temporárias do serviço, a mesma DAX é repetida automaticamente.
- **SC-003**: Em 100% dos cenários de falha consecutiva, o número de execuções não excede o limite configurado; com configuração padrão, não excede cinco.
- **SC-004**: Quando uma consulta tem sucesso antes do limite, nenhuma consulta adicional é executada para a mesma mensagem.
- **SC-005**: Após o limite ser atingido sem sucesso, o agente apresenta ao usuário uma resposta final com o estado da consulta e o diagnóstico mais recente disponível.

## Assumptions

- O limite padrão de cinco conta a execução inicial; portanto permite até quatro retentativas corretivas.
- O limite é aplicado separadamente a cada mensagem do usuário e às chamadas `consultar_bi`, não ao total de ferramentas executadas.
- Erros de DAX são devolvidos ao modelo para correção; falhas temporárias do serviço repetem automaticamente a mesma DAX. Ambos os tipos consomem o mesmo limite por mensagem. Falhas de autenticação e permissão são devolvidas como erro final sem retentativa.
- A mensagem completa significa todo o texto diagnóstico legível devolvido na resposta de erro, incluindo os detalhes aninhados; informações confidenciais continuam redigidas.
- O Histórico de consultas do Power BI existente é o local que administradores usam para inspecionar cada tentativa, sua DAX e seu erro.
