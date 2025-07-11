# WebooChat

**WebooChat** é uma plataforma de atendimento online integrada com **ChatGPT** e **WhatsApp**, desenvolvida para oferecer uma experiência conversacional inteligente, automatizada e personalizada para empresas e clientes.

---

## 🚀 Visão Geral

WebooChat permite que empresas se conectem com seus clientes via WhatsApp e Web Chat, utilizando a inteligência do ChatGPT para responder automaticamente, registrar interações e transferir para atendentes humanos quando necessário.

---

## ⚙️ Tecnologias Utilizadas

- **Frontend:** [React](https://reactjs.org/)
- **Backend:** [.NET Core 8](https://dotnet.microsoft.com/)
- **Banco de Dados:** [PostgreSQL](https://www.postgresql.org/)
- **Integrações:**
  - [OpenAI GPT-4](https://platform.openai.com/)
  - [WhatsApp Cloud API](https://developers.facebook.com/docs/whatsapp/cloud-api)

---

## 🧩 Funcionalidades

- Atendimento automatizado via ChatGPT
- Integração direta com WhatsApp Business API
- Interface web moderna e responsiva
- Suporte a múltiplos atendentes e setores
- Histórico de conversas
- Encaminhamento inteligente (bot → humano)
- Painel administrativo para controle de atendimentos

---

## 📦 Estrutura do Projeto

```plaintext
/WebooChat
│
├── backend/            # API em .NET Core
│   └── Controllers/
│   └── Services/
│   └── Models/
│
├── frontend/           # Interface em React
│   └── src/
│       └── components/
│       └── pages/
│       └── services/
│
├── database/           # Scripts e migrations PostgreSQL
│
└── README.md
```

---

## 🛠️ Como Rodar Localmente

### Pré-requisitos
- Node.js 18+
- .NET 8 SDK
- PostgreSQL 14+
- Docker (opcional, para ambiente integrado)

### Backend (.NET Core)
```bash
cd backend
dotnet restore
dotnet ef database update
dotnet run
```

### Frontend (React)
```bash
cd frontend
npm install
npm run dev
```

---

## 🔐 Variáveis de Ambiente

Crie um arquivo `.env` com as seguintes variáveis:

```env
# OpenAI
OPENAI_API_KEY=your_openai_key

# WhatsApp API
WHATSAPP_TOKEN=your_whatsapp_token
WHATSAPP_PHONE_NUMBER_ID=your_number_id

# Banco de dados
POSTGRES_HOST=localhost
POSTGRES_PORT=5432
POSTGRES_DB=weboochat
POSTGRES_USER=weboo
POSTGRES_PASSWORD=securepassword
```

---

## 📄 Licença

Este projeto está licenciado sob a licença MIT. Veja o arquivo [LICENSE](LICENSE) para mais detalhes.

---

## 🌐 Acesse

🔗 [https://weboochat.com](https://weboochat.com)

---

## 🙌 Contribuição

Pull requests são bem-vindos! Sinta-se livre para sugerir melhorias ou reportar bugs.
