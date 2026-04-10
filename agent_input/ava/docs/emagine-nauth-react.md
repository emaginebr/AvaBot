# NAuth React

> Biblioteca React completa de componentes de autenticacao para integracao com NAuth.

**Produto Emagine** | [Repositorio](https://github.com/emaginebr/nauth-react)

---

## O que e o NAuth React?

O NAuth React e um pacote NPM que fornece uma biblioteca completa de componentes React para autenticacao e gerenciamento de usuarios. Ele se integra diretamente com a API NAuth, oferecendo telas prontas de login, registro, recuperacao de senha, gerenciamento de perfil e controle de roles.

A biblioteca e projetada para ser incorporada em qualquer aplicacao React, reduzindo drasticamente o tempo de desenvolvimento de funcionalidades de autenticacao. Suporta temas claro/escuro, internacionalizacao (portugues e ingles) e e totalmente acessivel (WCAG).

---

## Principais Funcionalidades

- **Suite completa de autenticacao** — Login, registro, recuperacao de senha, alteracao de senha, tudo pronto para uso
- **Temas claro e escuro** — Suporte automatico a tema do sistema com opcao de troca manual
- **Internacionalizacao** — Portugues (BR) e ingles incluidos, extensivel para qualquer idioma com 200+ chaves de traducao
- **Tree-shakeable** — Importe apenas os componentes que precisa, sem peso desnecessario
- **TypeScript completo** — Tipagem total para autocompletar e seguranca em tempo de desenvolvimento
- **Fingerprinting de dispositivo** — Identificacao de dispositivo via FingerprintJS para seguranca adicional
- **Acessibilidade WCAG** — Componentes acessiveis por padrao
- **Design responsivo mobile-first** — Funciona perfeitamente em qualquer tamanho de tela
- **Gerenciamento de roles** — Componentes para listar, criar e atribuir roles
- **Busca de usuarios** — Componente de busca com paginacao

---

## Stack Tecnologica

| Componente | Tecnologia |
|------------|------------|
| Framework | React 19, TypeScript |
| Estilizacao | Tailwind CSS 3, tailwindcss-animate |
| Roteamento | React Router DOM v7 |
| Internacionalizacao | i18next |
| Fingerprinting | FingerprintJS |
| Icones | Lucide React |
| Notificacoes | Sonner (toasts) |
| Build | Vite 7 |

---

## Arquitetura e Integracoes

A biblioteca utiliza o padrao Provider-Context-Hook. Basta envolver a aplicacao com `NAuthProvider` configurado com a URL da API NAuth, e todos os componentes filhos terao acesso automatico a autenticacao.

Tambem exporta `createNAuthClient` para uso standalone da API sem componentes visuais.

Utilitarios inclusos: validacao de CPF, CNPJ, email, formatacao de telefone e verificacao de forca de senha.

### Produtos Emagine Relacionados

- **NAuth** — Backend de autenticacao que esta biblioteca consome
- **Todos os frontends Emagine** — avachat-app, dedalo-app, devblog, MonexUp e outros utilizam nauth-react para autenticacao

---

## Casos de Uso

1. **Adicionar autenticacao em minutos** — Instale o pacote, envolva com o Provider, e tenha login/registro funcionando imediatamente
2. **Portal administrativo** — Use os componentes de gerenciamento de roles e busca de usuarios para criar um painel admin
3. **Aplicacao multi-idioma** — Configure o idioma e todas as telas de autenticacao se adaptam automaticamente

---

## Perguntas Frequentes (FAQ)

### Tecnicas

**P: Como instalo o pacote?**
R: `npm install nauth-react react-router-dom` e configure o Tailwind para incluir os estilos do pacote.

**P: Preciso do backend NAuth para funcionar?**
R: Sim, a biblioteca se comunica com a API NAuth para todas as operacoes de autenticacao.

**P: Posso customizar as traducoes?**
R: Sim, voce pode passar traducoes customizadas via configuracao do Provider, ou adicionar novos idiomas completos.

**P: Os componentes sao estilizaveis?**
R: Sim, utilizam Tailwind CSS e podem ser customizados via classes e variaveis CSS.

### Comerciais

**P: O pacote e gratuito?**
R: O pacote NPM esta disponivel publicamente. Para uso em producao com suporte, entre em contato com a Emagine.

**P: Existe uma aplicacao de exemplo?**
R: Sim, o repositorio inclui um `example-app/` completo demonstrando todos os fluxos de autenticacao.

---

## Como Comecar

1. Instale: `npm install nauth-react react-router-dom`
2. Configure o Tailwind para incluir `node_modules/nauth-react/dist/**/*`
3. Envolva sua aplicacao com `NAuthProvider` configurando `apiUrl` e demais opcoes
4. Use os componentes: `<LoginForm />`, `<RegisterForm />`, etc.

---

## Diferenciais Competitivos

- **Pronto para uso** — Componentes completos de autenticacao, nao apenas hooks ou utilitarios
- **i18n nativo** — Portugues e ingles inclusos com 200+ chaves, extensivel para qualquer idioma
- **Ecossistema integrado** — Funciona nativamente com NAuth backend e todos os produtos Emagine
- **Acessibilidade** — WCAG compliant por padrao, sem configuracao adicional

---

## Contato e Suporte

- **Website**: https://emagine.com.br
- **Repositorio**: https://github.com/emaginebr/nauth-react
- **Suporte**: Entre em contato pelo site da Emagine

---

*Documentacao gerada automaticamente a partir do repositorio GitHub. Ultima atualizacao: 2026-04-10*
