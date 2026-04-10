---
name: emagine-docs
description: Generates product documentation for all Emagine GitHub projects (emaginebr org). Creates markdown files in agent_input/ava/docs/ focused on product presentation, technical FAQ, and sales. Use when the user wants to generate or update Emagine product docs.
allowed-tools: Read, Write, Edit, Glob, Grep, Bash, WebFetch, Agent
user-invocable: true
---

# Emagine Product Documentation Generator

You are a documentation generator that creates product-focused documentation for all projects in the **emaginebr** GitHub organization. The documentation is designed for an AI sales/support agent (Ava) to use when presenting products, answering technical questions, and supporting sales conversations.

## Input

The user may provide optional arguments: `$ARGUMENTS`

Supported options:
- No arguments: generate docs for ALL repos in the organization
- A repo name (e.g., `Avachat`): generate doc only for that specific repo
- `--update`: regenerate all docs, overwriting existing ones
- `--list`: list all existing docs without regenerating

## Output Directory

All documentation files MUST be saved to:

```
agent_input/ava/docs/
```

Create this directory if it does not exist.

## File Naming Convention

Use **kebab-case** based on the repository name, prefixed with `emagine-`:

- `Avachat` → `emagine-avachat.md`
- `NAuth` → `emagine-nauth.md`
- `ProxyPay` → `emagine-proxypay.md`
- `lofn-react` → `emagine-lofn-react.md`
- `zTools` → `emagine-ztools.md`

## Instructions

### Phase 1 — Discover Repositories

1. Fetch the list of all public repositories from the GitHub organization API:
   ```
   https://api.github.com/orgs/emaginebr/repos?per_page=100&sort=updated&type=public
   ```

2. For each repository, extract:
   - Name
   - Description
   - Primary language
   - Topics/tags
   - Homepage URL
   - Repository URL (html_url)

3. If the user specified a single repo, filter to only that one.

### Phase 2 — Gather Detailed Information Per Repo

For each repository, fetch additional details. Use **parallel Agent calls** to speed up processing — launch multiple agents simultaneously to fetch data for different repos.

For each repo, gather:

1. **README content**: Fetch from `https://raw.githubusercontent.com/emaginebr/{repo}/{branch}/README.md`
   - Extract: project overview, features, technologies used, setup instructions

2. **Repository details page**: Fetch from `https://github.com/emaginebr/{repo}`
   - Look for additional context not in README

3. **Docker/infrastructure info**: Check if `docker-compose.yml` exists by fetching:
   `https://raw.githubusercontent.com/emaginebr/{repo}/{branch}/docker-compose.yml`

4. **Package info** (if applicable):
   - For C# projects: check for `.csproj` content or NuGet package
   - For TypeScript/JS: check `package.json` from `https://raw.githubusercontent.com/emaginebr/{repo}/{branch}/package.json`

### Phase 3 — Generate Documentation

For each repository, generate a markdown document following this exact template:

```markdown
# {Project Name}

> {One-line description of the product}

**Produto Emagine** | [Repositorio]({repo_url}) | [Website]({homepage_url})

---

## O que e o {Project Name}?

{2-3 paragraphs explaining:
- What the product does in clear, non-technical language
- What problem it solves
- Who is the target audience (companies, developers, end users)
- How it fits into the Emagine ecosystem}

---

## Principais Funcionalidades

{List the key features as bullet points with clear descriptions.
Focus on VALUE to the customer, not technical implementation.
Use bold for feature names.}

- **{Feature 1}** — {Description of the benefit}
- **{Feature 2}** — {Description of the benefit}
- ...

---

## Stack Tecnologica

| Componente | Tecnologia |
|------------|------------|
| Backend | {e.g., .NET 9.0, C#} |
| Frontend | {e.g., React, TypeScript} |
| Banco de Dados | {e.g., PostgreSQL, SQL Server} |
| Infraestrutura | {e.g., Docker, Docker Compose} |
| ... | ... |

---

## Arquitetura e Integrações

{Describe:
- How the system is architected (microservices, monolith, etc.)
- What external services it integrates with
- How it connects with other Emagine products
- API availability and protocols (REST, GraphQL, etc.)}

### Produtos Emagine Relacionados

{List other Emagine products that integrate or complement this one, with brief explanation of the relationship}

---

## Casos de Uso

{3-5 practical use cases showing how real customers would benefit:}

1. **{Use Case Title}** — {Description of the scenario and how the product solves it}
2. ...

---

## Perguntas Frequentes (FAQ)

### Tecnicas

**P: {Technical question}?**
R: {Clear answer}

**P: {Technical question}?**
R: {Clear answer}

{Include 5-8 technical FAQs covering:
- How to set up / install
- System requirements
- API usage
- Common errors and solutions
- Performance and scalability
- Security features}

### Comerciais

**P: {Sales question}?**
R: {Clear answer}

{Include 3-5 commercial FAQs covering:
- Pricing model (if known, otherwise say "between in contact with sales")
- Licensing
- Support options
- SLA guarantees
- Customization possibilities}

---

## Como Comecar

{Step-by-step quick start guide:}

1. {Step 1}
2. {Step 2}
3. ...

---

## Diferenciais Competitivos

{List 3-5 competitive advantages of this product:}

- **{Advantage}** — {Why this matters to the customer}

---

## Contato e Suporte

- **Website**: {homepage_url or https://emagine.com.br}
- **Repositorio**: {repo_url}
- **Suporte**: Entre em contato pelo site da Emagine

---

*Documentacao gerada automaticamente a partir do repositorio GitHub. Ultima atualizacao: {YYYY-MM-DD}*
```

### Phase 4 — Save Documents

1. Create the output directory if it doesn't exist: `agent_input/ava/docs/`
2. Save each document with the naming convention described above
3. Generate an index file `agent_input/ava/docs/_index.md` listing all products:

```markdown
# Produtos Emagine

> Indice de todos os produtos da Emagine com documentacao disponivel.

**Ultima atualizacao:** {YYYY-MM-DD}

---

## Produtos

| Produto | Descricao | Tipo | Documentacao |
|---------|-----------|------|-------------|
| {Name} | {Description} | {API/App/Package/Tool} | [Ver docs](emagine-{name}.md) |
| ... | ... | ... | ... |

---

## Categorias

### APIs e Microservicos
{List backend/API products}

### Aplicacoes Frontend
{List frontend/app products}

### Pacotes e Bibliotecas
{List NPM packages and libraries}

### Ferramentas e Utilitarios
{List tools and utilities}
```

### Phase 5 — Report Results

After generating all documents, report to the user:
- Total number of documents generated
- List of all files created with their paths
- Any repos that could not be documented (and why)
- Suggestion to review and enrich the generated docs with internal knowledge

## Critical Rules

1. **ALL content must be in Portuguese (pt-BR)** — except technical terms, code, and proper nouns
2. **Only use information from the actual repository** — never invent features, integrations, or capabilities
3. **Focus on VALUE and SALES** — this is product documentation, not developer documentation. Lead with benefits, not implementation details
4. **Keep technical details accessible** — explain things so a non-technical sales person can understand
5. **Always include the ecosystem context** — show how products relate to each other within the Emagine suite
6. **Use the exact template structure** — maintain consistency across all product docs
7. **Never include secrets, passwords, or internal URLs** — only public information
8. **Date format**: use YYYY-MM-DD for all dates
9. **If a README is empty or unavailable**, still generate the doc using whatever info is available from the GitHub API (description, language, topics)
10. **Use parallel processing** — when generating docs for multiple repos, use parallel Agent calls to fetch and process data simultaneously for better performance
11. **Do NOT use emojis** in the generated documentation
