---
name: deploy-prod
description: "Generates a GitHub Actions production deploy pipeline via SSH with sequential jobs. Reads docker-compose-prod.yml and .env.prod.example to auto-detect services, secrets, and network configuration."
allowed-tools: Read, Grep, Glob, Bash, Write, Edit
user-invocable: true
---

# Deploy Production Pipeline Generator

You are an expert DevOps assistant that generates GitHub Actions production deployment pipelines. The pipeline deploys via SSH using password authentication and is split into the maximum number of **sequential** jobs.

## Input

The user may provide additional context or customization: `$ARGUMENTS`

If no arguments are provided, analyze the current project and generate the pipeline.

---

## SSH Connection

All SSH steps use `appleboy/ssh-action@v1` with password authentication:

```yaml
uses: appleboy/ssh-action@v1
with:
  host: ${{ secrets.PROD_SSH_HOST }}
  username: ${{ secrets.PROD_SSH_USER }}
  password: ${{ secrets.PROD_SSH_PASSWORD }}
  port: ${{ secrets.PROD_SSH_PORT || 22 }}
  script: |
    # commands here
```

**Required GitHub Secrets (connection):**

| Secret | Description |
|--------|-------------|
| `PROD_SSH_HOST` | Production server IP/hostname |
| `PROD_SSH_USER` | SSH username |
| `PROD_SSH_PASSWORD` | SSH password |
| `PROD_SSH_PORT` | SSH port (default: 22) |

---

## Pipeline Architecture

The pipeline MUST be split into the **maximum number of sequential jobs**. Each job has a single, clear responsibility. Jobs run in strict sequence using `needs:`.

### Job Structure

```
checkout → inject-secrets → network-setup → stop-services → build-deploy → health-check → summary
```

Every SSH job MUST begin with `set -e` and define `DEPLOY_DIR` consistently.

---

## Job Definitions

### Job 1: `checkout` — Clone or Update Repository

Clones the repository on first deploy, or fetches and resets to the latest commit on subsequent deploys.

```yaml
checkout:
  runs-on: ubuntu-latest
  steps:
    - name: Clone or update repository
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

          if [ -d "$DEPLOY_DIR" ]; then
            echo "Updating existing repository..."
            cd "$DEPLOY_DIR"
            git fetch origin
            git reset --hard "origin/$BRANCH"
            git clean -fd
          else
            echo "Cloning repository..."
            git clone --branch "$BRANCH" --single-branch "$REPO_URL" "$DEPLOY_DIR"
          fi

          echo "Repository ready at $DEPLOY_DIR"
```

### Job 2: `inject-secrets` — Write .env.prod File

Writes the `.env.prod` file with secrets from GitHub Secrets. The variables are discovered by reading `.env.prod.example`.

```yaml
inject-secrets:
  runs-on: ubuntu-latest
  needs: checkout
  steps:
    - name: Inject secrets into .env.prod
      uses: appleboy/ssh-action@v1
      with:
        host: ${{ secrets.PROD_SSH_HOST }}
        username: ${{ secrets.PROD_SSH_USER }}
        password: ${{ secrets.PROD_SSH_PASSWORD }}
        port: ${{ secrets.PROD_SSH_PORT || 22 }}
        script: |
          set -e
          DEPLOY_DIR="/opt/$PROJECT_NAME"
          cd "$DEPLOY_DIR"

          cat > .env.prod <<'ENVEOF'
          $SECRET_VARIABLES_HERE
          ENVEOF

          sed -i 's/^[[:space:]]*//' .env.prod
          echo ".env.prod written successfully"
```

### Job 3: `network-setup` — Ensure Docker Network Exists

Creates the external Docker network if it doesn't already exist. The network name is discovered from `docker-compose-prod.yml`.

```yaml
network-setup:
  runs-on: ubuntu-latest
  needs: inject-secrets
  steps:
    - name: Ensure Docker network exists
      uses: appleboy/ssh-action@v1
      with:
        host: ${{ secrets.PROD_SSH_HOST }}
        username: ${{ secrets.PROD_SSH_USER }}
        password: ${{ secrets.PROD_SSH_PASSWORD }}
        port: ${{ secrets.PROD_SSH_PORT || 22 }}
        script: |
          set -e
          docker network inspect $EXTERNAL_NETWORK >/dev/null 2>&1 || docker network create $EXTERNAL_NETWORK
          echo "Network $EXTERNAL_NETWORK ready"
```

### Job 4: `stop-services` — Stop Running Containers

Gracefully stops existing containers before rebuilding.

```yaml
stop-services:
  runs-on: ubuntu-latest
  needs: network-setup
  steps:
    - name: Stop running containers
      uses: appleboy/ssh-action@v1
      with:
        host: ${{ secrets.PROD_SSH_HOST }}
        username: ${{ secrets.PROD_SSH_USER }}
        password: ${{ secrets.PROD_SSH_PASSWORD }}
        port: ${{ secrets.PROD_SSH_PORT || 22 }}
        script: |
          set -e
          DEPLOY_DIR="/opt/$PROJECT_NAME"
          cd "$DEPLOY_DIR"

          docker compose --env-file .env.prod -f docker-compose-prod.yml down || true
          echo "Services stopped"
```

### Job 5: `build-deploy` — Build and Start Services

Builds Docker images and starts all services in detached mode.

```yaml
build-deploy:
  runs-on: ubuntu-latest
  needs: stop-services
  steps:
    - name: Build and start services
      uses: appleboy/ssh-action@v1
      with:
        host: ${{ secrets.PROD_SSH_HOST }}
        username: ${{ secrets.PROD_SSH_USER }}
        password: ${{ secrets.PROD_SSH_PASSWORD }}
        port: ${{ secrets.PROD_SSH_PORT || 22 }}
        script: |
          set -e
          DEPLOY_DIR="/opt/$PROJECT_NAME"
          cd "$DEPLOY_DIR"

          docker compose --env-file .env.prod -f docker-compose-prod.yml up --build -d
          echo "Services started"
```

### Job 6: `health-check` — Verify Deployment

Waits for services to start and verifies they are running correctly.

```yaml
health-check:
  runs-on: ubuntu-latest
  needs: build-deploy
  steps:
    - name: Verify deployment health
      uses: appleboy/ssh-action@v1
      with:
        host: ${{ secrets.PROD_SSH_HOST }}
        username: ${{ secrets.PROD_SSH_USER }}
        password: ${{ secrets.PROD_SSH_PASSWORD }}
        port: ${{ secrets.PROD_SSH_PORT || 22 }}
        script: |
          set -e
          DEPLOY_DIR="/opt/$PROJECT_NAME"
          cd "$DEPLOY_DIR"

          echo "Waiting for services to start..."
          sleep 10

          docker compose --env-file .env.prod -f docker-compose-prod.yml ps

          # Health check via HTTP
          HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:$APP_PORT/ || echo "000")
          if [ "$HTTP_STATUS" = "200" ]; then
            echo "Health check passed (HTTP $HTTP_STATUS)"
          else
            echo "WARNING: Health check returned HTTP $HTTP_STATUS"
            docker compose --env-file .env.prod -f docker-compose-prod.yml logs --tail=50
            exit 1
          fi
```

### Job 7: `summary` — Write Deployment Summary

Writes a summary to the GitHub Actions job summary. This is the only job that does NOT use SSH.

```yaml
summary:
  runs-on: ubuntu-latest
  needs: health-check
  steps:
    - name: Deployment summary
      run: |
        echo "## ✅ Production Deployment" >> $GITHUB_STEP_SUMMARY
        echo "" >> $GITHUB_STEP_SUMMARY
        echo "| Detail | Value |" >> $GITHUB_STEP_SUMMARY
        echo "|--------|-------|" >> $GITHUB_STEP_SUMMARY
        echo "| Server | \`${{ secrets.PROD_SSH_HOST }}\` |" >> $GITHUB_STEP_SUMMARY
        echo "| Branch | \`main\` |" >> $GITHUB_STEP_SUMMARY
        echo "| Commit | \`${{ github.sha }}\` |" >> $GITHUB_STEP_SUMMARY
        echo "| Triggered by | \`${{ github.actor }}\` |" >> $GITHUB_STEP_SUMMARY
        echo "| Timestamp | \`$(date -u +'%Y-%m-%d %H:%M:%S UTC')\` |" >> $GITHUB_STEP_SUMMARY
```

---

## Discovery Phase

Before generating the pipeline, read these files to auto-detect configuration:

1. **`docker-compose-prod.yml`** — Discover:
   - External network name (for `network-setup` job)
   - Exposed port (for `health-check` job)
   - Service names

2. **`.env.prod.example`** — Discover:
   - All secret variable names → map to `${{ secrets.VAR_NAME }}`
   - These go into the `inject-secrets` job

3. **`Dockerfile`** or `docker-compose-prod.yml` `build:` section — Discover:
   - Build context and Dockerfile path

4. **Repository name** — From `git remote -v` or infer from directory name:
   - Used for `DEPLOY_DIR` (`/opt/<project-name-lowercase>`)

---

## Customization Rules

When generating the pipeline:

1. **`$PROJECT_NAME`**: Replace with the actual project name (lowercase, kebab-case). Used for `DEPLOY_DIR=/opt/$PROJECT_NAME`.

2. **`$EXTERNAL_NETWORK`**: Read from `docker-compose-prod.yml` → `networks:` section → find the one with `external: true`.

3. **`$SECRET_VARIABLES_HERE`**: Read `.env.prod.example`, extract each variable name, and map to GitHub Secrets syntax:
   ```
   VAR_NAME=${{ secrets.VAR_NAME }}
   ```

4. **`$APP_PORT`**: Read from `docker-compose-prod.yml` → `ports:` → extract the host port (left side of `:`).

5. **Workflow trigger**: Default is `workflow_dispatch` (manual). If the user requests automatic triggers, add `push:` with branch filters.

---

## Output

Save the generated pipeline to `.github/workflows/deploy-prod.yml`.

After generating, report:
- The file path
- The number of jobs created
- The list of required GitHub Secrets (connection + application)
- Any values that could not be auto-detected and need manual review

---

## Critical Rules

1. **ALL jobs are sequential** — every job after the first MUST have `needs:` pointing to the previous job
2. **NO parallel jobs** — the pipeline is a strict linear chain
3. **Maximum granularity** — split into as many jobs as makes sense (minimum 7 as defined above)
4. **SSH with password** — always use `appleboy/ssh-action@v1` with `password:` field
5. **`set -e`** — every SSH script MUST start with `set -e` for fail-fast behavior
6. **`DEPLOY_DIR`** — must be consistent across all jobs
7. **Network from compose** — always read the external network name from `docker-compose-prod.yml`, never hardcode
8. **Secrets from .env.prod.example** — always read variable names from this file, never guess
9. **Health check** — always verify the deployment is working via HTTP after starting services
10. **`|| true` on stop** — the `stop-services` job should not fail if containers aren't running yet (first deploy)
