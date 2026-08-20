# Research: Autenticação JWT e Painel Administrativo

**Branch**: `007-admin-auth-panel` | **Date**: 2026-04-09

## R-001: Estratégia de Autenticação JWT no Frontend

**Decision**: Zustand store para estado de auth + interceptor de requisições com Bearer token + ProtectedRoute wrapper

**Rationale**:
- Zustand é o state manager obrigatório (constitution)
- localStorage para persistência do token (constitution — nunca cookies)
- Bearer token no header Authorization (padrão JWT)
- Componente ProtectedRoute redireciona para login se não autenticado
- Token expirado detectado via resposta 401 do backend → redirect para login

**Alternatives considered**:
- React Context para auth: Rejeitado — constitution exige Zustand para state management
- httpOnly cookies: Rejeitado — constitution proíbe cookies para tokens
- Refresh token: Não necessário — token tem validade de 8h e existe apenas 1 usuário admin

## R-002: Correção de Endpoints Frontend vs Backend

**Decision**: Corrigir os caminhos nos Services para corresponder ao backend real

**Rationale**: O frontend usa prefixo `/api/` e o backend não. O nginx/proxy faz o roteamento. A análise do Bruno collection e controllers do backend revelou:

| Frontend atual | Backend real | Ação |
|---------------|-------------|------|
| `POST /api/agents/{slug}/sessions` | `POST /sessions/agents/{slug}` | Corrigir path |
| `GET /api/agents/{agentId}/sessions?pagina=&tamanhoPagina=` | `GET /sessions/agents/{agentId}?page=&maxPage=` | Corrigir path + params |
| `GET /api/sessions/{id}/messages?pagina=&tamanhoPagina=` | `GET /sessions/{id}/messages?page=&maxPage=` | Corrigir params |
| `GET /api/agents/{agentId}/files` | `GET /files/{agentId}` | Corrigir path |
| `POST /api/agents/{agentId}/files` | `POST /files/{agentId}` | Corrigir path |
| `DELETE /api/agents/{agentId}/files/{fileId}` | `DELETE /files/{agentId}/{fileId}` | Corrigir path |
| `POST /api/agents/{agentId}/files/{fileId}/reprocess` | `POST /files/{agentId}/{fileId}/reprocess` | Corrigir path |

**Nota**: O proxy nginx pode estar mapeando `/api/` para o backend. Verificar configuração do nginx para decidir se os paths devem incluir ou não o prefixo. Se o nginx faz strip de `/api/`, os paths do frontend estão corretos mas os paths dos services de sessão e arquivos precisam ajuste estrutural.

**Alternatives considered**:
- Manter paths atuais e ajustar nginx: Possível mas aumenta acoplamento com infraestrutura
- Criar um API client centralizado: Bom mas fora do escopo — manter padrão atual dos Services

## R-003: Estrutura de Navegação Administrativa

**Decision**: AdminLayout com sidebar fixa à esquerda + navbar superior com seletor de agente

**Rationale**:
- Sidebar: itens de menu contextuais ao agente selecionado (Configurações, Sessões, Base de Conhecimento)
- Navbar: logo/título, seletor de agente (dropdown), botão de logout
- Agente selecionado armazenado no useAgentStore (Zustand) — persistido em localStorage
- Seções da sidebar ficam desabilitadas até um agente ser selecionado
- Dashboard acessível sem agente selecionado

**Alternatives considered**:
- Tabs horizontais: Rejeitado — sidebar é mais escalável e padrão em painéis admin
- Menu na navbar com dropdown: Rejeitado — menos espaço para itens de navegação

## R-004: Dashboard Pós-Login

**Decision**: Dashboard simples com cards de resumo + lista de sessões recentes

**Rationale**:
- Cards: total de agentes (ativos/inativos), total de sessões recentes
- Lista: últimas sessões de todos os agentes (requer chamada para cada agente ou endpoint dedicado)
- Como não existe endpoint de dashboard no backend, os dados serão agregados a partir dos endpoints existentes (GET /agents para contagem, GET /sessions/agents/{id} para sessões recentes)

**Alternatives considered**:
- Dashboard com gráficos: Fora do escopo — adicionaria dependência de charting library
- Apenas lista de agentes: Rejeitado pelo usuário na clarificação

## R-005: Busca na Base de Conhecimento

**Decision**: Página dedicada com campo de busca e exibição de resultados em cards

**Rationale**:
- Endpoint existente: `GET /agents/{id}/search?query=&topK=5`
- Resultados são strings (chunks de texto) — exibir em cards com destaque do termo buscado
- Parâmetro topK configurável via campo numérico (padrão 5)

**Alternatives considered**:
- Busca inline na página de arquivos: Menos espaço para resultados
- Modal de busca: Limita visualização de resultados longos

## R-006: Auth Headers em Requisições Protegidas

**Decision**: Função utilitária `getAuthHeaders()` no AuthService que retorna headers com Bearer token. Cada Service importa e usa nas chamadas protegidas.

**Rationale**:
- Mantém o padrão atual dos Services (cada um gerencia seus headers)
- Simples e explícito — sem interceptors globais complexos
- Se token não existe ou está expirado (401), redireciona para login

**Alternatives considered**:
- Fetch wrapper global com interceptor: Mais complexo, fora do padrão atual
- Axios com interceptors: Rejeitado — constitution não lista axios como dependência
