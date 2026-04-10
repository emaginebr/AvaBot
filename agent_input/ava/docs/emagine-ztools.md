# zTools

> API de utilitarios compartilhados com IA, email, armazenamento e validacao.

**Produto Emagine** | [Repositorio](https://github.com/emaginebr/zTools)

---

## O que e o zTools?

O zTools e uma API RESTful que centraliza servicos utilitarios comuns utilizados por todos os produtos da Emagine. Ele fornece integracoes com inteligencia artificial (ChatGPT e DALL-E), envio de emails (MailerSend), armazenamento de arquivos (S3/DigitalOcean Spaces), validacao de documentos brasileiros e geracao de slugs.

Em vez de cada produto reimplementar essas funcionalidades, o zTools oferece uma API unificada e um pacote NuGet com clientes HTTP prontos para consumo em projetos .NET.

---

## Principais Funcionalidades

- **ChatGPT** — Envie mensagens, conversas e requisicoes customizadas para a API OpenAI
- **DALL-E** — Geracao de imagens por IA com prompts personalizados
- **Envio de emails** — Emails transacionais via MailerSend com validacao de formato
- **Armazenamento S3** — Upload e recuperacao de arquivos em storage compativel com S3 (DigitalOcean Spaces)
- **Validacao de documentos** — Validacao de CPF e CNPJ com verificacao de digito
- **Utilitarios de string** — Geracao de slug, extracao de numeros e IDs unicos

---

## Stack Tecnologica

| Componente | Tecnologia |
|------------|------------|
| Backend | .NET 8, ASP.NET Core, C# |
| IA | OpenAI API (ChatGPT, DALL-E) |
| Email | MailerSend |
| Armazenamento | AWS S3 / DigitalOcean Spaces |
| Pagamentos | Stripe (integracao) |
| Imagens | SixLabors.ImageSharp |
| Documentacao | Swagger/Swashbuckle |
| Testes | xUnit, Moq |
| Infraestrutura | Docker, Docker Compose |
| CI/CD | GitHub Actions, GitVersion |

---

## Arquitetura e Integracoes

Clean Architecture com 5 controllers: ChatGPT, Documents, Files, Mail e String.

Distribuido tambem como pacote NuGet contendo ACL (Anti-Corruption Layer) com clientes HTTP e DTOs para consumo direto em projetos .NET.

### Produtos Emagine Relacionados

- **Todos os backends Emagine** — NAuth, Avachat, Brahma, Lofn, Dedalo, NNews, BazzucaMedia e MonexUp consomem o zTools
- **NAuth** — Processamento de imagens de perfil
- **NNews** — Geracao de conteudo e imagens com IA
- **BazzucaMedia** — Upload de midias para redes sociais

---

## Casos de Uso

1. **Geracao de conteudo com IA** — Sistemas que precisam gerar textos, artigos ou descricoes automaticamente via ChatGPT
2. **Geracao de imagens** — Criacao de imagens de produtos, avatares ou ilustracoes via DALL-E
3. **Upload centralizado** — Todos os produtos usam o zTools como ponto unico de upload para S3
4. **Validacao de documentos** — Validacao de CPF/CNPJ em formularios e cadastros

---

## Perguntas Frequentes (FAQ)

### Tecnicas

**P: Como integro o zTools no meu projeto .NET?**
R: Instale o pacote NuGet do zTools que inclui clientes HTTP (ACL) e DTOs prontos para consumo.

**P: Quais modelos da OpenAI sao suportados?**
R: O modelo e configuravel via variavel de ambiente. Suporta qualquer modelo disponivel na API OpenAI.

**P: Posso usar outro provedor de S3?**
R: Sim, qualquer storage compativel com o protocolo S3 funciona (AWS, DigitalOcean Spaces, MinIO, etc.).

**P: O envio de email suporta templates?**
R: O zTools envia emails via MailerSend com conteudo customizado. Templates devem ser configurados no MailerSend.

**P: Existe limite de uso da API?**
R: Os limites sao definidos pelos provedores externos (OpenAI, MailerSend, S3). O zTools em si nao impoe limites.

### Comerciais

**P: O zTools pode ser usado independentemente?**
R: Sim, ele funciona como microservico independente e pode ser utilizado por qualquer aplicacao.

**P: O pacote NuGet e gratuito?**
R: Entre em contato com a equipe comercial da Emagine para detalhes sobre licenciamento.

---

## Como Comecar

1. Configure as variaveis de ambiente: MailerSend, OpenAI API key, S3 credentials
2. Suba via Docker: `docker-compose up -d --build`
3. Acesse o Swagger para explorar os endpoints
4. Ou instale o pacote NuGet para integrar em seu projeto .NET

---

## Diferenciais Competitivos

- **Centralizacao** — Um unico servico para IA, email, storage e validacao, evitando duplicacao
- **Pacote NuGet** — Integracao em projetos .NET em minutos com clientes HTTP prontos
- **Multi-provider S3** — Funciona com AWS, DigitalOcean Spaces ou qualquer storage S3-compativel
- **IA pronta para uso** — ChatGPT e DALL-E acessiveis via API REST simples

---

## Contato e Suporte

- **Website**: https://emagine.com.br
- **Repositorio**: https://github.com/emaginebr/zTools
- **Suporte**: Entre em contato pelo site da Emagine

---

*Documentacao gerada automaticamente a partir do repositorio GitHub. Ultima atualizacao: 2026-04-10*
