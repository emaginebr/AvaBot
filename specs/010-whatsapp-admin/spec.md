# Feature Specification: Área Administrativa WhatsApp

**Feature Branch**: `010-whatsapp-admin`  
**Created**: 2026-04-12  
**Status**: Draft  
**Input**: User description: "Criar área administrativa WhatsApp com sessão, QR Code, status de conexão e desconexão"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Iniciar Sessão e Escanear QR Code (Priority: P1)

O administrador acessa a seção "WhatsApp" no painel administrativo, logo abaixo de "Bot Telegram" no menu lateral. Ao clicar em "Iniciar Sessão", o sistema solicita ao backend que inicie uma sessão WhatsApp para o agente selecionado. Após o início, um QR Code é exibido na tela para que o administrador escaneie com o WhatsApp no celular. O sistema faz polling periódico do status da conexão e, quando o QR Code é escaneado com sucesso, atualiza automaticamente o status para "Conectado".

**Why this priority**: Sem iniciar a sessão e escanear o QR Code, nenhuma outra funcionalidade WhatsApp é possível. Este é o fluxo principal de ativação.

**Independent Test**: Acessar /admin/whatsapp, clicar "Iniciar Sessão", verificar que o QR Code é exibido, escanear com o celular e confirmar que o status muda para "Conectado".

**Acceptance Scenarios**:

1. **Given** um agente selecionado sem sessão WhatsApp ativa, **When** o administrador clica em "Iniciar Sessão", **Then** o sistema inicia a sessão e exibe um QR Code para escaneamento.
2. **Given** o QR Code exibido na tela, **When** o administrador escaneia com o WhatsApp, **Then** o status é atualizado automaticamente para "Conectado".
3. **Given** o QR Code exibido na tela, **When** o QR Code expira sem ser escaneado, **Then** o sistema exibe uma mensagem informando que o QR Code expirou e oferece opção de gerar um novo.
4. **Given** nenhum agente selecionado, **When** o administrador acessa "WhatsApp", **Then** uma mensagem informa que é necessário selecionar um agente.

---

### User Story 2 - Visualizar Status da Conexão (Priority: P2)

O administrador visualiza o status atual da conexão WhatsApp do agente. O status é exibido de forma clara com indicador visual (conectado/desconectado/conectando). O status é atualizado ao acessar a página e pode ser atualizado por polling automático durante o processo de conexão.

**Why this priority**: Saber se a sessão está ativa é essencial para gerenciamento, mas depende da sessão já ter sido iniciada (US1).

**Independent Test**: Com sessão WhatsApp ativa, acessar a página e verificar que o status "Conectado" é exibido corretamente. Com sessão inativa, verificar que o status reflete "Desconectado".

**Acceptance Scenarios**:

1. **Given** um agente com sessão WhatsApp ativa, **When** o administrador acessa "WhatsApp", **Then** o status "Conectado" é exibido com indicador visual verde.
2. **Given** um agente sem sessão WhatsApp, **When** o administrador acessa "WhatsApp", **Then** o status "Desconectado" é exibido com indicador visual cinza.
3. **Given** uma sessão em processo de conexão, **When** o administrador está na página, **Then** o status "Conectando..." é exibido e atualiza automaticamente via polling.

---

### User Story 3 - Desconectar Sessão WhatsApp (Priority: P3)

O administrador clica em "Desconectar" para encerrar a sessão WhatsApp ativa do agente. O sistema solicita confirmação antes de desconectar e, após confirmar, encerra a sessão via backend. O status é atualizado para "Desconectado".

**Why this priority**: Funcionalidade complementar de gerenciamento — usada quando o administrador precisa encerrar ou reiniciar a conexão.

**Independent Test**: Com sessão ativa, clicar "Desconectar", confirmar, e verificar que o status muda para "Desconectado".

**Acceptance Scenarios**:

1. **Given** um agente com sessão WhatsApp ativa, **When** o administrador clica em "Desconectar", **Then** uma confirmação é solicitada antes de prosseguir.
2. **Given** a confirmação aceita, **When** o sistema desconecta a sessão, **Then** o status é atualizado para "Desconectado" e o botão "Iniciar Sessão" volta a estar disponível.
3. **Given** a confirmação recusada, **When** o administrador cancela, **Then** a sessão permanece ativa sem alterações.
4. **Given** um agente sem sessão ativa, **When** o administrador está na página, **Then** o botão "Desconectar" está desabilitado.

---

### Edge Cases

- O que acontece quando o serviço WhatsApp (WPP Connect) está indisponível durante o início da sessão?
- Como o sistema se comporta quando o QR Code é gerado mas o serviço fica indisponível antes do escaneamento?
- O que acontece se o administrador troca de agente enquanto aguarda o escaneamento do QR Code?
- Como o sistema se comporta quando a sessão é desconectada pelo lado do celular (fora do painel admin)?
- O que acontece se o polling de status falha repetidamente (timeout de rede)?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema DEVE exibir uma seção "WhatsApp" no menu lateral do painel administrativo, posicionada logo abaixo de "Bot Telegram".
- **FR-002**: A seção "WhatsApp" DEVE estar disponível apenas quando um agente está selecionado.
- **FR-003**: O sistema DEVE fornecer um botão "Iniciar Sessão" que inicia a conexão WhatsApp para o agente selecionado.
- **FR-004**: O sistema DEVE exibir um QR Code após o início da sessão para que o administrador escaneie com o WhatsApp.
- **FR-005**: O QR Code DEVE ser exibido como imagem legível, em tamanho adequado para escaneamento.
- **FR-006**: O sistema DEVE verificar automaticamente o status da conexão via polling periódico durante o processo de conexão (enquanto aguarda escaneamento do QR Code).
- **FR-007**: O sistema DEVE exibir o status da conexão com indicadores visuais claros: "Conectado" (verde), "Desconectado" (cinza), "Conectando..." (amarelo/animação).
- **FR-008**: O sistema DEVE fornecer um botão "Desconectar" para encerrar a sessão WhatsApp ativa.
- **FR-009**: O botão "Desconectar" DEVE solicitar confirmação antes de encerrar a sessão.
- **FR-010**: O botão "Desconectar" DEVE estar desabilitado quando não houver sessão ativa.
- **FR-011**: O botão "Iniciar Sessão" DEVE estar desabilitado quando já houver sessão ativa (status "Conectado").
- **FR-012**: O sistema DEVE exibir feedback visual (sucesso/erro) para todas as operações (iniciar sessão, desconectar).
- **FR-013**: O polling de status DEVE parar automaticamente quando a conexão for estabelecida ou quando o administrador sair da página.

### Key Entities

- **Sessão WhatsApp do Agente**: Representa a conexão ativa do WhatsApp de um agente. Identificada pelo slug do agente. Possui estados: Desconectado, Conectando, Conectado.
- **QR Code**: Imagem gerada pelo serviço WhatsApp para autenticação. Temporário, expira após um período definido pelo serviço.
- **Status da Conexão**: Estado atual da sessão — slug do agente, status textual, indicador de conexão ativa.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: O administrador consegue iniciar uma sessão WhatsApp e visualizar o QR Code em menos de 10 segundos após clicar.
- **SC-002**: Após escanear o QR Code, o status atualiza automaticamente para "Conectado" em menos de 15 segundos.
- **SC-003**: A desconexão da sessão é concluída com feedback visual em até 5 segundos.
- **SC-004**: O status da conexão é exibido corretamente em 100% dos cenários (conectado, desconectado, conectando).
- **SC-005**: O administrador consegue completar o fluxo inteiro (iniciar sessão → escanear QR → confirmar conexão) em menos de 1 minuto.
- **SC-006**: O polling de status não gera requisições desnecessárias quando a sessão já está conectada ou a página não está visível.

## Assumptions

- O backend já possui todos os endpoints necessários para gerenciamento de sessão WhatsApp (start-session, qrcode, status, disconnect).
- O serviço WPP Connect está configurado e acessível pelo backend.
- O QR Code retornado pelo backend é uma string base64 que pode ser renderizada como imagem.
- O status da conexão retornado pelo backend inclui o campo "isConnected" (booleano) e "status" (string textual).
- O padrão de criação de páginas admin existente (service + page + rota + sidebar) será seguido.
- O sistema de autenticação admin existente será reutilizado para proteger as operações de WhatsApp.
- Os endpoints utilizam o slug do agente como identificador (diferente do Telegram que usa ID numérico).
- O intervalo de polling adequado para verificar o status é de 3-5 segundos durante o processo de conexão.
