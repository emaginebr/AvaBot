---
name: dotnet-env
description: "Structures .NET project environments (Development, Docker, Production) with proper appsettings per environment, docker-compose files, .env management, SSL configuration for production, and deploy pipeline."
allowed-tools: Read, Grep, Glob, Bash, Write, Edit, Task
user-invocable: true
---

# .NET Environment Structure Guide

You are an expert assistant that structures .NET projects with three well-defined environments: **Development**, **Docker**, and **Production**. Each environment has its own configuration files, secrets management strategy, and deployment approach.

## Input

The user will describe what to configure: `$ARGUMENTS`

Before generating files:
1. **Read the solution structure** — Identify the main project(s), `Program.cs`, existing `appsettings*.json` files
2. **Read existing `appsettings.json`** — Understand current configuration sections and keys
3. **Read existing `docker-compose*.yml`** — Check for existing Docker setup
4. **Read `.gitignore`** — Ensure `.env*` and secrets are properly ignored
5. **Read `Dockerfile`** — Check for existing Dockerfile setup
6. **Identify the project name** — Use it for container names, network names, and deploy paths

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Environment Matrix                        │
├──────────────┬──────────────┬──────────────┬────────────────┤
│              │ Development  │   Docker     │  Production    │
├──────────────┼──────────────┼──────────────┼────────────────┤
│ Runs on      │ Local machine│ Docker local │ Docker remote  │
│ IDE          │ VS / VS Code │ N/A          │ N/A            │
│ appsettings  │ .Development │ .Docker      │ .Production    │
│ Secrets in   │ JSON file    │ .env file    │ .env.prod file │
│ SSL          │ Dev cert     │ No SSL       │ SSL required   │
│ Compose file │ N/A          │ docker-compose│docker-compose-prod│
│ Ports        │ Default      │ Exposed      │ Per demand     │
└──────────────┴──────────────┴──────────────┴────────────────┘
```

---

## Core Principles

### 1. ASP.NET Core `__` Convention for Docker

In Docker environments, use the **double-underscore (`__`) separator** to map environment variables to `appsettings.json` sections. This is the standard .NET configuration binding.

```yaml
environment:
  ConnectionStrings__DefaultConnection: ${CONNECTION_STRING}
  JwtSettings__Secret: ${JWT_SECRET}
```

Maps to:
```json
{
  "ConnectionStrings": { "DefaultConnection": "value" },
  "JwtSettings": { "Secret": "value" }
}
```

### 2. NEVER use `AddEnvironmentVariables("PREFIX")` in code

Use only the default configuration providers. Environment variables with `__` convention are automatically bound.

### 3. Secrets NEVER committed

- `.env`, `.env.prod` → always in `.gitignore`
- `.env.example`, `.env.prod.example` → committed with placeholder values

### 4. ASPNETCORE_ENVIRONMENT controls which appsettings loads

- Development → `appsettings.Development.json`
- Docker → `appsettings.Docker.json`
- Production → `appsettings.Production.json`

---

## Environment 1: Development

### Purpose
Runs directly on the developer's machine via Visual Studio or VS Code. All values are hardcoded in the appsettings file for simplicity.

### File: `appsettings.Development.json`

All configuration values — including secrets — are written directly in this file. This is acceptable because it's a local-only environment.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=5432;Database=myapp_dev;Username=postgres;Password=postgres"
  },
  "JwtSettings": {
    "Secret": "dev-secret-key-min-32-chars-long-here",
    "Issuer": "myapp-dev",
    "Audience": "myapp-dev",
    "ExpirationInMinutes": 60
  }
}
```

### Rules
- **All values filled directly** in the JSON file
- Use local database connection strings (localhost)
- Use weak/dev-only secrets — never production values
- `ASPNETCORE_ENVIRONMENT=Development` is the default in `launchSettings.json`

### File: `Properties/launchSettings.json`

Ensure the `ASPNETCORE_ENVIRONMENT` is set:

```json
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

---

## Environment 2: Docker (Local Development)

### Purpose
Runs in Docker containers locally. Secrets and configuration are injected via `.env` file and `docker-compose.yml`.

### File: `appsettings.Docker.json`

Contains only **structure and non-secret defaults**. Secret values are **overridden** by environment variables from docker-compose:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "JwtSettings": {
    "Secret": "",
    "Issuer": "",
    "Audience": "",
    "ExpirationInMinutes": 60
  }
}
```

### File: `.env`

Contains ALL configuration and secrets for the Docker environment:

```env
# Database
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_DB=myapp
CONNECTION_STRING=Server=db;Port=5432;Database=myapp;Username=postgres;Password=postgres

# JWT
JWT_SECRET=docker-secret-key-min-32-chars-long-here
JWT_ISSUER=myapp-docker
JWT_AUDIENCE=myapp-docker

# App
APP_PORT=5000
```

### File: `.env.example`

Committed to the repository with placeholder values:

```env
# Database
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your_password_here
POSTGRES_DB=myapp
CONNECTION_STRING=Server=db;Port=5432;Database=myapp;Username=postgres;Password=your_password_here

# JWT
JWT_SECRET=your_jwt_secret_min_32_chars
JWT_ISSUER=myapp
JWT_AUDIENCE=myapp

# App
APP_PORT=5000
```

### File: `docker-compose.yml`

- NO SSL configuration
- Exposes all necessary ports
- Uses `.env` file for variable substitution

```yaml
services:
  api:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: myapp-api
    ports:
      - "${APP_PORT:-5000}:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Docker
      ConnectionStrings__DefaultConnection: ${CONNECTION_STRING}
      JwtSettings__Secret: ${JWT_SECRET}
      JwtSettings__Issuer: ${JWT_ISSUER}
      JwtSettings__Audience: ${JWT_AUDIENCE}
    depends_on:
      db:
        condition: service_healthy
    networks:
      - myapp-network

  db:
    image: postgres:17-alpine
    container_name: myapp-db
    ports:
      - "5432:5432"
    environment:
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_DB: ${POSTGRES_DB}
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER}"]
      interval: 5s
      timeout: 5s
      retries: 5
    networks:
      - myapp-network

volumes:
  postgres_data:

networks:
  myapp-network:
    driver: bridge
```

### Rules
- `ASPNETCORE_ENVIRONMENT=Docker` set in docker-compose
- All secrets come from `.env` via `${VARIABLE}` substitution
- Expose all development-useful ports (API, database, etc.)
- No SSL — local development only
- Use `depends_on` with health checks

---

## Environment 3: Production

### Purpose
Runs on a remote server via Docker. SSL is required. Only secrets are injected via `.env.prod` — non-secret configuration lives in `appsettings.Production.json`.

### File: `appsettings.Production.json`

Contains **all non-secret values** directly. Only secret placeholders are empty (overridden by environment variables):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "JwtSettings": {
    "Secret": "",
    "Issuer": "myapp",
    "Audience": "myapp",
    "ExpirationInMinutes": 30
  }
}
```

### File: `.env.prod`

Contains **ONLY secrets** — no non-secret configuration:

```env
# Database
POSTGRES_USER=prod_user
POSTGRES_PASSWORD=strong_production_password
POSTGRES_DB=myapp_prod
CONNECTION_STRING=Server=db;Port=5432;Database=myapp_prod;Username=prod_user;Password=strong_production_password

# JWT
JWT_SECRET=production-secret-key-very-strong-min-64-chars-recommended
```

### File: `.env.prod.example`

Committed to repository — only secrets with placeholders:

```env
# Database
POSTGRES_USER=prod_user
POSTGRES_PASSWORD=<STRONG_PASSWORD>
POSTGRES_DB=myapp_prod
CONNECTION_STRING=Server=db;Port=5432;Database=myapp_prod;Username=prod_user;Password=<STRONG_PASSWORD>

# JWT
JWT_SECRET=<STRONG_JWT_SECRET_MIN_64_CHARS>
```

### File: `docker-compose-prod.yml`

- SSL configured via reverse proxy or Kestrel HTTPS
- Ports exposed per demand (not all ports open)
- Uses `.env.prod` for secrets
- External network for reverse proxy integration

```yaml
services:
  api:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: myapp-api
    restart: unless-stopped
    ports:
      - "8080:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__DefaultConnection: ${CONNECTION_STRING}
      JwtSettings__Secret: ${JWT_SECRET}
    depends_on:
      db:
        condition: service_healthy
    networks:
      - myapp-network
      - myapp-external

  db:
    image: postgres:17-alpine
    container_name: myapp-db
    restart: unless-stopped
    # No port exposed externally — only internal access
    environment:
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_DB: ${POSTGRES_DB}
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER}"]
      interval: 10s
      timeout: 5s
      retries: 5
    networks:
      - myapp-network

volumes:
  postgres_data:

networks:
  myapp-network:
    driver: bridge
  myapp-external:
    external: true
```

### SSL Configuration

Production SSL is handled via **reverse proxy** (recommended). The API container listens on HTTP internally, and a reverse proxy (Nginx, Traefik, Caddy) terminates SSL.

#### Option A: Nginx Reverse Proxy (add to docker-compose-prod.yml)

```yaml
  nginx:
    image: nginx:alpine
    container_name: myapp-nginx
    restart: unless-stopped
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf:ro
      - ./nginx/certs:/etc/nginx/certs:ro
    depends_on:
      - api
    networks:
      - myapp-network
      - myapp-external
```

#### Nginx Config (`nginx/nginx.conf`):

```nginx
events {
    worker_connections 1024;
}

http {
    server {
        listen 80;
        server_name yourdomain.com;
        return 301 https://$host$request_uri;
    }

    server {
        listen 443 ssl;
        server_name yourdomain.com;

        ssl_certificate /etc/nginx/certs/fullchain.pem;
        ssl_certificate_key /etc/nginx/certs/privkey.pem;

        ssl_protocols TLSv1.2 TLSv1.3;
        ssl_ciphers HIGH:!aNULL:!MD5;

        location / {
            proxy_pass http://api:8080;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }
    }
}
```

#### Option B: Caddy (automatic Let's Encrypt)

```yaml
  caddy:
    image: caddy:2-alpine
    container_name: myapp-caddy
    restart: unless-stopped
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./Caddyfile:/etc/caddy/Caddyfile:ro
      - caddy_data:/data
      - caddy_config:/config
    depends_on:
      - api
    networks:
      - myapp-network
      - myapp-external
```

```
# Caddyfile
yourdomain.com {
    reverse_proxy api:8080
}
```

### Rules
- `ASPNETCORE_ENVIRONMENT=Production`
- Only secrets in `.env.prod` — non-secret config in `appsettings.Production.json`
- Database port NOT exposed externally
- `restart: unless-stopped` on all services
- External network for reverse proxy integration
- SSL is mandatory — use reverse proxy approach

---

## Deploy Pipeline: `.github/workflows/deploy-prod.yml`

Generate a GitHub Actions workflow for production deployment via SSH:

```yaml
name: Deploy Production

on:
  workflow_dispatch:

jobs:
  deploy:
    runs-on: ubuntu-latest

    steps:
    - name: Deploy via SSH
      uses: appleboy/ssh-action@v1
      with:
        host: ${{ secrets.PROD_SSH_HOST }}
        username: ${{ secrets.PROD_SSH_USER }}
        password: ${{ secrets.PROD_SSH_PASSWORD }}
        port: ${{ secrets.PROD_SSH_PORT || 22 }}
        script: |
          set -e

          DEPLOY_DIR="/opt/$PROJECT_NAME"
          REPO_URL="https://github.com/${{ github.repository }}.git"
          BRANCH="main"

          # Clone or update repository
          if [ -d "$DEPLOY_DIR" ]; then
            echo "Updating existing repository..."
            cd "$DEPLOY_DIR"
            git fetch origin
            git reset --hard "origin/$BRANCH"
            git clean -fd
          else
            echo "Cloning repository..."
            git clone --branch "$BRANCH" --single-branch "$REPO_URL" "$DEPLOY_DIR"
            cd "$DEPLOY_DIR"
          fi

          # Inject .env.prod from GitHub Secrets
          cat > .env.prod <<'ENVEOF'
          $SECRET_VARIABLES_HERE
          ENVEOF

          # Remove leading whitespace from .env.prod
          sed -i 's/^[[:space:]]*//' .env.prod

          # Create external network if it doesn't exist
          docker network inspect $EXTERNAL_NETWORK >/dev/null 2>&1 || docker network create $EXTERNAL_NETWORK

          # Deploy with docker compose
          docker compose --env-file .env.prod -f docker-compose-prod.yml down
          docker compose --env-file .env.prod -f docker-compose-prod.yml up --build -d

          # Wait and verify
          echo "Waiting for services to start..."
          sleep 10
          docker compose --env-file .env.prod -f docker-compose-prod.yml ps

          echo "Deployment completed successfully."

    - name: Summary
      run: |
        echo "## Production Deployment" >> $GITHUB_STEP_SUMMARY
        echo "" >> $GITHUB_STEP_SUMMARY
        echo "- Server: \`${{ secrets.PROD_SSH_HOST }}\`" >> $GITHUB_STEP_SUMMARY
        echo "- Branch: \`main\`" >> $GITHUB_STEP_SUMMARY
        echo "- Commit: \`${{ github.sha }}\`" >> $GITHUB_STEP_SUMMARY
        echo "- Triggered by: \`${{ github.event_name }}\`" >> $GITHUB_STEP_SUMMARY
```

### Pipeline Customization Rules

When generating the pipeline:
1. Replace `$PROJECT_NAME` with the actual project name (lowercase, kebab-case)
2. Replace `$EXTERNAL_NETWORK` with the project's external network name
3. Replace `$SECRET_VARIABLES_HERE` with the actual secret variables from `.env.prod.example`, using GitHub Secrets syntax:
   ```
   POSTGRES_USER=${{ secrets.POSTGRES_USER }}
   POSTGRES_PASSWORD=${{ secrets.POSTGRES_PASSWORD }}
   CONNECTION_STRING=${{ secrets.CONNECTION_STRING }}
   JWT_SECRET=${{ secrets.JWT_SECRET }}
   ```
4. Each secret in `.env.prod` must have a corresponding GitHub Secret

### Required GitHub Secrets

| Secret | Description |
|--------|-------------|
| `PROD_SSH_HOST` | Production server IP/hostname |
| `PROD_SSH_USER` | SSH username |
| `PROD_SSH_PASSWORD` | SSH password |
| `PROD_SSH_PORT` | SSH port (default: 22) |
| + all secrets from `.env.prod.example` | Application secrets |

---

## Dockerfile

Ensure a multi-stage Dockerfile exists:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MyApp.dll"]
```

### Rules
- Replace `MyApp.dll` with the actual project assembly name
- Use the .NET version matching the project's `TargetFramework`
- Expose port `8080` (default Kestrel port in .NET 8+)

---

## .gitignore Entries

Ensure these entries exist in `.gitignore`:

```gitignore
# Environment files with secrets
.env
.env.prod
.env.local

# SSL certificates
nginx/certs/
*.pem
*.key
```

---

## Implementation Checklist

When the user invokes this skill, follow this order:

1. **Read** existing project structure, `appsettings.json`, `Program.cs`, `.gitignore`, `Dockerfile`
2. **Identify** project name, .NET version, existing configuration sections
3. **Create/Update** `appsettings.Development.json` — fill all values directly
4. **Create/Update** `appsettings.Docker.json` — empty secrets, structure only
5. **Create/Update** `appsettings.Production.json` — non-secret values filled, secrets empty
6. **Create** `.env` and `.env.example` for Docker environment
7. **Create** `.env.prod` and `.env.prod.example` for Production (secrets only)
8. **Create/Update** `docker-compose.yml` — no SSL, expose ports, use `.env`
9. **Create/Update** `docker-compose-prod.yml` — SSL via reverse proxy, use `.env.prod`
10. **Create** `.github/workflows/deploy-prod.yml` — SSH deploy pipeline
11. **Update** `.gitignore` — ensure `.env` and `.env.prod` are ignored
12. **Verify** `Dockerfile` exists and is correct
13. **Verify** `Program.cs` does NOT use `AddEnvironmentVariables("PREFIX")`

### Adaptation Rules
- Match all configuration sections from the existing `appsettings.json`
- Use the actual project name for container names, network names, and deploy paths
- Use the actual database type (PostgreSQL, SQL Server, etc.) from existing connection strings
- Preserve any existing configuration that doesn't conflict with this structure
