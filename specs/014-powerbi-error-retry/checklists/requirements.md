# Specification Quality Checklist: Diagnóstico detalhado e retentativas de consultas Power BI

**Purpose**: Validar completude e qualidade da especificação antes do planejamento  
**Created**: 2026-10-08  
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] Sem detalhes de implementação desnecessários; configuração `appsettings` consta porque foi exigida explicitamente.
- [x] Foco no valor para usuários e operadores.
- [x] Texto compreensível para stakeholders não técnicos.
- [x] Todas as seções obrigatórias foram preenchidas.

## Requirement Completeness

- [x] Nenhum marcador `[NEEDS CLARIFICATION]` permanece.
- [x] Requisitos são testáveis e sem ambiguidade relevante.
- [x] Critérios de sucesso são mensuráveis.
- [x] Critérios de sucesso são orientados a resultado, sem dependência de detalhes de implementação.
- [x] Cenários de aceitação estão definidos para cada história.
- [x] Casos extremos estão identificados, incluindo resposta sem formato reconhecível e configuração inválida.
- [x] Escopo está delimitado a erros de consultas BI, diagnóstico e tentativas por mensagem.
- [x] Dependências e pressupostos estão identificados.

## Feature Readiness

- [x] Todos os requisitos funcionais têm comportamento verificável nos cenários ou critérios de sucesso.
- [x] Histórias cobrem recuperação, inspeção do Histórico de consultas do Power BI com pergunta/DAX/erro por tentativa e configuração do limite.
- [x] A feature pode ser validada pelos resultados mensuráveis definidos.
- [x] A única especificação técnica explícita é a configuração em `appsettings`, solicitada pelo usuário.

## Notes

- O limite padrão conta a consulta inicial; portanto, cinco tentativas totais permitem até quatro retentativas corretivas.
- A configuração inválida ou ausente cai no padrão cinco; valores válidos devem ser positivos.
- Erros de DAX retornam ao modelo para correção; falhas temporárias repetem a mesma DAX automaticamente e consomem o mesmo limite.
- Falhas temporárias intermediárias não são enviadas ao modelo; o diagnóstico final é enviado quando o orçamento se esgota.
- Falhas de autenticação e permissão não são repetidas.
