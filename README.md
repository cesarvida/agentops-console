# AgentOps Console

> El portero que decide qué agentes IA pueden desplegarse.

## ¿Qué hace?

AgentOps Console es un **agente guardián** que supervisa otros agentes IA. Cuando alguien sube un agente IA nuevo a GitHub:

1. ✅ Lo analiza automáticamente
2. 🔍 Comprueba si cumple las reglas de seguridad
3. 📋 Publica un reporte detallado en el PR
4. 🛡️ Bloquea o aprueba el despliegue

---

## 📋 Reglas de Governance

Todo agente IA que pase por AgentOps Console es evaluado contra estas reglas.
Si viola alguna regla crítica, el PR queda **BLOQUEADO** automáticamente.

---

### 🔴 Reglas CRÍTICAS — Bloquean el PR automáticamente

#### 1. Sin funciones de ejecución peligrosa
El agente NO puede contener llamadas a funciones que ejecuten código arbitrario o comandos del sistema.

❌ Prohibido:
- `eval()` — ejecuta código como string
- `exec()` — ejecuta comandos del sistema  
- `Process.Start()` con input de usuario
- `Runtime.exec()`
- `os.system()` / `shell=True`

✅ Alternativa: Usa funciones específicas con parámetros controlados.

---

#### 2. Sin secretos hardcodeados
El agente NO puede tener credenciales, tokens o contraseñas escritas directamente en el código.

❌ Prohibido:
- API keys: `sk-abc123`, `pk-live-xxx`, `ghp_token`
- Passwords: `var password = "mi_password"`
- Tokens: `const API_KEY = "abc123"`

✅ Alternativa: Usa variables de entorno o un gestor de secretos.

---

#### 3. Sin SQL Injection
El agente NO puede construir queries SQL concatenando input de usuario directamente.

❌ Prohibido:
```csharp
"SELECT * FROM users WHERE id = " + userId
$"DELETE FROM orders WHERE name = {userInput}"
```

✅ Alternativa: Usa queries parametrizadas o un ORM.

---

#### 4. Sin Prompt Injection
El agente NO puede contener frases que intenten manipular o sobreescribir instrucciones de otros sistemas IA.

❌ Prohibido:
- "ignore previous instructions"
- "system override"
- "forget your rules"
- "new instructions:"

✅ Alternativa: Define el comportamiento del agente en su propia configuración YAML, nunca en el código.

---

#### 5. Sin Path Traversal
El agente NO puede navegar fuera de su directorio autorizado usando rutas relativas maliciosas.

❌ Prohibido:
- `"../../../etc/passwd"`
- `Path.Combine(userInput, "file.txt")` sin sanitizar
- Cualquier ruta que empiece con `../`

✅ Alternativa: Valida y sanitiza todas las rutas antes de usarlas.

---

### 🟡 Reglas de ADVERTENCIA — No bloquean pero bajan el score

#### 6. Audit Logging recomendado
El agente debería tener configurado un sistema de logging para registrar todas sus acciones.

#### 7. Owner definido
El agente debería tener un responsable asignado en su definición YAML.

---

## 📊 Sistema de Scoring

Cada agente recibe una puntuación de 0 a 100:

| Score     | Estado        | Resultado                              |
|-----------|---------------|----------------------------------------|
| 70 - 100  | ✅ APROBADO   | El agente puede desplegarse            |
| 40 - 69   | ⚠️ REVISAR    | Necesita revisión humana antes de pasar|
| 0 - 39    | ❌ BLOQUEADO  | El PR queda bloqueado automáticamente  |

Cada hallazgo crítico resta 15 puntos del score base de 100.

---

## ✅ Ejemplo de agente APROBADO

```yaml
# data/agent-definitions/mi-agente.yaml
agent:
  id: "code-reviewer-v1"
  name: "Code Reviewer"
  version: "1.0.0"
  owner: "equipo-backend"

governance:
  allowed_actions:
    - read_code
    - post_comment
    - request_changes
  
  audit:
    log_all_actions: true
    retention_days: 90
```

## ❌ Ejemplo de agente BLOQUEADO

```yaml
# Este agente sería BLOQUEADO por las siguientes razones:
agent:
  name: "Deploy Agent"
  # ❌ Sin owner definido
  
governance:
  allowed_actions:
    - deploy_to_production
    - push_to_main          # ❌ Acción prohibida
    - modify_permissions    # ❌ Acción prohibida
  
  # ❌ Sin audit logging configurado
```

---

## Patrones de Seguridad Detectados

Detecta **7 patrones deterministas** sin ML/LLM:

| Patrón | Severidad | Ejemplo |
|--------|-----------|---------|
| **Funciones peligrosas** | Alta | `eval()`, `exec()`, `Process.Start()` |
| **Secretos hardcodeados** | Crítica | `sk-*`, `pk-*`, `ghp_*` tokens |
| **SQL Injection** | Alta | String concatenation en queries |
| **Prompt Injection** | Media | "ignore previous instructions" |
| **Path Traversal** | Alta | `../`, `..\\` patterns |
| **Dependencias vulnerables** | Media | CVE database checks |
| **Inconsistencias** | Baja | Code pattern violations |

## Cómo Funciona

### Localmente (CLI)

```bash
# Analizar un PR específico
dotnet run -- analyze-pr <owner> <repo> <prNumber>

# Ejemplo
dotnet run -- analyze-pr cesarvida agentops-console 12

# GITHUB_TOKEN required
$env:GITHUB_TOKEN = 'ghp_xxxxxxxxxxxx'
```

### En GitHub Actions (Automático)

El workflow se activa automáticamente en cada PR y genera un reporte de seguridad como comentario.

## Estado del Proyecto

```
Build:     ✅ 0 errores
Tests:     ✅ 42/42 passing
GitHub Actions: ✅ Funcional

Últimas Features:
✅ 7 detectores de seguridad
✅ PR Comment Posting
✅ GitHub API integration
✅ CQRS architecture
```

## Stack Tecnológico

- .NET 10 con C# 12
- YamlDotNet 12.2.0
- xUnit para testing
- GitHub REST API v3

## Development

### Build y Test

```bash
dotnet build
dotnet test
dotnet run
```

### Opción 1: CLI Menu
```bash
dotnet run
# Selecciona opciones 1-8 en el menú
```

### Opción 2: Analyze PR (GitHub Actions mode)
```bash
dotnet run -- analyze-pr cesarvida agentops-console 123
```

## Arquitectura

**Clean Architecture:**
- Core: Entidades, value objects
- Application: Handlers, interfaces
- Infrastructure: GitHub API, persistence
- CLI: Entry point

## Licencia

MIT

---

## 🧠 Phase 11: Semantic Analysis (Azure OpenAI)

In addition to deterministic rule checks, AgentOps Console can send the agent YAML to Azure OpenAI for a semantic governance review. The model evaluates ambiguity, missing constraints, unsafe behavior, excessive permissions, and risky operational patterns.

### What it does

| Semantic risk | Governance impact |
|---|---|
| `LOW` | No status change |
| `MEDIUM` | APPROVED → REVIEW |
| `HIGH` | APPROVED or REVIEW → BLOCKED |

Existing rule-based `BLOCKED` results are **never overridden** by semantic analysis.
If Azure is unavailable (missing credentials, timeout, invalid response), the rule-based result stands.

### Required environment variables

| Variable | Description |
|---|---|
| `AZURE_OPENAI_ENDPOINT` | Endpoint URL for your Azure OpenAI resource |
| `AZURE_OPENAI_API_KEY` | API key (never hardcoded, never logged) |
| `AZURE_OPENAI_DEPLOYMENT_NAME` | Deployment name (default: `gpt-5.4-nano`) |

Copy `.env.example` to `.env` and fill in your values for local development.
`.env` is gitignored and must never be committed.

### GitHub Secrets needed

Add these three secrets in **Settings → Secrets and variables → Actions**:

- `AZURE_OPENAI_ENDPOINT`
- `AZURE_OPENAI_API_KEY`
- `AZURE_OPENAI_DEPLOYMENT_NAME`

The `AI Governance Check` workflow reads them automatically. If any are absent, semantic analysis is skipped and the rule-based check continues normally.

### Disable semantic analysis

Set `enabled: false` in `data/governance-config.yaml`:

```yaml
governance:
  semantic_analysis:
    enabled: false
```

### Fallback behavior

When Azure OpenAI is unavailable the report shows:

```
🧠 Semantic Analysis:
   Status: Skipped
   Reason: Timeout after 5s    # or: missing configuration / API error / invalid response
```

The governance check continues with rule-based results only and does not fail the PR solely because semantic analysis was unavailable.

---

## 🔒 Branch Protection & Required Status Checks

The **AI Governance Check** workflow (`governance-check.yml`) enforces governance rules at the GitHub merge gate. When any agent definition file changes in a PR, the workflow validates it and blocks the merge if violations are critical.

### How it works

| Governance result | Workflow exit code | GitHub check status | Merge allowed? |
|---|---|---|---|
| `APPROVED` | `0` | ✅ success | Yes |
| `REVIEW` | `0` | ✅ success | Yes (warning logged) |
| `BLOCKED` | `1` | ❌ failure | **No** |

### Enable branch protection on `main`

1. Go to **Settings → Branches** in your GitHub repository.
2. Click **Add branch protection rule** (or edit the existing rule for `main`).
3. Enable **Require status checks to pass before merging**.
4. In the search box, type `Governance Enforcement` and select the check.
5. Enable **Require branches to be up to date before merging**.
6. Click **Save changes**.

> The Required Status Check name is `AI Governance Check / Governance Enforcement` — it appears automatically after the workflow runs at least once on a PR.

### Verify the setup

Open any PR that modifies a file under `data/agent-definitions/`. You should see the **AI Governance Check / Governance Enforcement** check in the PR checks list. If the agent is BLOCKED, the **Merge pull request** button will be disabled until violations are resolved.

---

**Version:** 0.4.0 | **Status:** Production Ready ✅
