# Quickstart: Relatório de calibração de perguntas

## Pré-requisitos

1. A API está rodando, local ou em homologação, com um agente que tenha chave OpenAI. Para o BI, o agente também precisa ter Power BI ligado e schema gerado.
2. `AvaBot.Tests.API/appsettings.json` tem `ApiSettings.BaseUrl`, `Username` e `Password` de um administrador.
3. O agente é identificado pelo slug, como `abipesca`.

## Uma pergunta

```powershell
./scripts/calibrate-questions.ps1 -Agent abipesca -Question "Qual foi o volume de exportação da tilápia em 2024?"
```

O console mostra o relatório em markdown. O caminho do arquivo gravado aparece na última linha.

## Conversa com esclarecimento

`calibration/abipesca.json`:

```json
{
  "agent": "abipesca",
  "conversations": [
    { "name": "tilapia-2024", "messages": ["Qual foi o volume de exportação da tilápia em 2024?"] },
    { "name": "tilapia-esclarecimento", "messages": [
      "Qual foi o volume de exportação da tilápia em 2024?",
      "Considere todos os produtos de tilápia"
    ]}
  ]
}
```

```powershell
./scripts/calibrate-questions.ps1 -File ./calibration/abipesca.json -Output ./calibracao-abipesca.md
```

## Sem o script

```powershell
$env:CALIBRATION_AGENT = "abipesca"
$env:CALIBRATION_QUESTION = "Qual foi o volume de exportação da tilápia em 2024?"
dotnet test AvaBot.Tests.API --filter "Category=Calibration" --logger "console;verbosity=detailed"
```

Sem as variáveis, `dotnet test AvaBot.Tests.API` mostra o teste de calibração como **Skipped**.

## Analisar com uma IA

Cole o arquivo `.md` numa conversa com uma IA, ou peça ao Claude Code para "rodar a calibração e analisar o relatório". O relatório termina com instruções para a IA que vai analisá-lo.

## Validação da feature

| Cenário | Esperado |
|---|---|
| Pergunta com BI (tilápia) | Seções 1 a 5 presentes, uma subseção por rodada, DAX e diagnósticos completos, tokens por rodada |
| Pergunta sem BI ("o que é a ABIPESCA?") | "Ferramentas de BI não disponíveis" ou nenhuma chamada; uma rodada `stop` |
| Conversa de 2 mensagens | A mensagem 2 mostra o histórico com a pergunta 1 e a resposta do agente |
| Agente com slug inexistente | O teste falha com "Agente '<slug>' não encontrado" |
| Teste de agente no painel | Mesma tela e mesmos dados de antes |
| `dotnet test` sem variáveis | Calibração Skipped; demais testes inalterados |
| Busca por segredos no `.md` gerado | Nenhuma ocorrência de `sk-`, `Bearer ` ou JWT |
