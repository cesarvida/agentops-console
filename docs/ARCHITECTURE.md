# Architecture — AgentOps Console

> Current state: Phases 9–12 complete. Governance enforcement, semantic analysis, demo harness.

## Overview

AgentOps Console is a **governed agent registry** and **PR merge gate**.
When a developer pushes an agent definition (YAML or JSON) and opens a pull request, a GitHub Actions workflow validates it against a rule engine and optionally a semantic AI reviewer. If the agent is BLOCKED, the merge is rejected at the branch-protection level.

```
Developer pushes YAML/JSON
        │
        ▼
┌──────────────────────────────────┐
│   GitHub Pull Request            │
│   triggers governance-check.yml  │
└──────────────┬───────────────────┘
               │  dotnet run validate-agent <file>
               ▼
┌──────────────────────────────────┐
│   GovernanceRuleEngine           │
│   ─ AllowedActionsRule           │
│   ─ ForbiddenActionsRule         │
│   ─ AuditLoggingRule             │
│   ─ OwnerDefinedRule             │
│   ─ VersionDefinedRule           │
│   ─ RateLimitRule                │
│   ─ TimeoutRule                  │
│   ─ EnvironmentScopeRule         │
└──────────────┬───────────────────┘
               │  optional (if Azure creds present)
               ▼
┌──────────────────────────────────┐
│   AzureOpenAIGovernanceClient    │
│   semantic risk: LOW/MEDIUM/HIGH │
└──────────────┬───────────────────┘
               │
               ▼
          FinalStatus
     APPROVED / REVIEW / BLOCKED
               │
     exit 0   / exit 0 /  exit 1
               │
               ▼
  GitHub Required Status Check
  PASS (merge allowed) / FAIL (merge blocked)
```

---

## Project Structure

```
AgentOps.Console.sln
├── src/
│   ├── AgentOps.Core/           — Domain models, governance rules, config types
│   ├── AgentOps.Application/    — Use cases, command handlers, rule engine
│   ├── AgentOps.Infrastructure/ — YAML parsing, Azure OpenAI client, GitHub API client
│   ├── AgentOps.CLI/            — Entry point, DI wiring, command dispatch
│   ├── AgentOps.Agents/         — Agent framework placeholder (dependency of App+Infra)
│   ├── AgentOps.GitHub/         — GitHub REST API client (PR analysis, comment posting)
│   └── AgentOps.Security/       — Security analysis rules (prompt injection, etc.)
├── tests/
│   ├── AgentOps.Core.Tests/         — Domain entity and governance config tests
│   ├── AgentOps.Application.Tests/  — Rule engine, exceptions, semantic, demo golden tests
│   ├── AgentOps.Security.Tests/     — Security rule tests
│   └── AgentOps.Infrastructure.Tests/ — File persistence tests
├── demo/                        — Runnable demo harness (fake + real semantic modes)
├── tools/autopsy/               — Health check tool, generates autopsy report
├── scripts/                     — GitHub setup verification scripts
├── data/
│   ├── governance-config.yaml   — Per-repo governance rules and scoring config
│   └── agent-definitions/       — Example agent YAML fixtures
└── .github/workflows/
    ├── governance-check.yml     — Required Status Check (DO NOT RENAME)
    └── pr-analysis.yml          — Optional PR comment analysis workflow
```

---

## Layer Responsibilities

### `AgentOps.Core` — Domain Layer

**No dependencies** on other AgentOps projects.

Key types:
- `AgentDefinition` — The agent being governed. Holds `AgentConfiguration` with `AllowedActions`, `Environments`, `AuditConfig`, `Exceptions`.
- `GovernanceConfig` — Per-repo config: allowed/forbidden action lists, scoring thresholds, `SemanticAnalysisConfig`.
- `GovernanceReport` — Result of an evaluation: `FinalStatus`, `GovernanceScore`, rule results, `SemanticAnalysis`.
- `GovernanceException` — Human-approved override that downgrades a Critical rule result to Warning for a time-bounded period.
- `IGovernanceRule` / `IConfigurableGovernanceRule` — Rule contracts. Configurable rules receive `GovernanceConfig` alongside the agent.
- **Rules** (8 rules, all in `Core.Governance.Rules/`):
  | Rule | Severity | What it checks |
  |---|---|---|
  | `AllowedActionsRule` | Critical | Actions must be in the allowed whitelist |
  | `ForbiddenActionsRule` | Critical | Actions must not appear in the forbidden list |
  | `AuditLoggingRule` | Warning | Agent must have `log_all_actions: true` |
  | `OwnerDefinedRule` | Critical | Agent must have a non-empty owner |
  | `VersionDefinedRule` | Critical | Version must match semver (not "latest", "dev", etc.) |
  | `RateLimitRule` | Warning | `requests_per_minute` should be ≤ 1000 |
  | `TimeoutRule` | Warning | `timeout_seconds` should be ≤ 300 |
  | `EnvironmentScopeRule` | Warning | At least one environment must be declared |

### `AgentOps.Application` — Use Case Layer

Depends on Core, Agents.

Key types:
- `GovernanceRuleEngine` — Runs all registered rules against an agent. Applies `GovernanceException` downgrades. Computes `GovernanceScore` and `FinalStatus` (APPROVED/REVIEW/BLOCKED) based on `ScoringConfig` thresholds.
- `ValidateAgentCommandHandler` — Orchestrates: load agent definition (YAML or JSON) → run rule engine → optional semantic analysis → merge results → persist JSON report.
- `AgentDefinitionLoader` — Loads agent definitions from files. Supports `.yaml`, `.yml`, and `.json` extensions. Automatically detects format and dispatches to appropriate deserializer.
- `AgentYamlDeserializer` — Parses agent YAML using YamlDotNet with CamelCase naming.
- `AgentJsonDeserializer` — Parses agent JSON using System.Text.Json with snake_case property mapping.
- `IAgentSemanticAnalyzer` — Interface for semantic analysis. Default implementation is `AzureOpenAIGovernanceClient`. A `FakeAgentSemanticAnalyzer` is available in `demo/` for testing without credentials.

### `AgentOps.Infrastructure` — External Dependencies Layer

Depends on Core, Application, Agents, GitHub.

Key types:
- `AzureOpenAIGovernanceClient` — Calls Azure OpenAI Chat Completions to assess agent YAML semantics. Returns `SemanticAnalysisResult.Skipped(...)` on any error — never throws.
- `GovernanceConfigLoader` — Fetches `data/governance-config.yaml` from the GitHub API (used in CI).
- `LocalGovernanceConfigReader` — Reads `data/governance-config.yaml` from the local filesystem (used in CLI `validate-agent`).
- `GovernanceConfigParser` — Shared YAML parsing for governance config (UnderscoredNamingConvention).

### `AgentOps.CLI` — Entry Point

Depends on all other src projects.

The `validate-agent <file>` subcommand is the core of the enforcement pipeline. It supports agent definition files in YAML (`.yaml`, `.yml`) or JSON (`.json`) format:
1. Reads `data/governance-config.yaml` via `LocalGovernanceConfigReader`
2. Loads agent definition via `AgentDefinitionLoader` (auto-detects YAML/JSON)
3. Builds `GovernanceRuleEngine` with all 8 rules
4. Builds `AzureOpenAIGovernanceClient` if Azure env vars are set
5. Calls `ValidateAgentCommandHandler.HandleAsync(command, config)`
6. Displays the report
7. Sets `Environment.ExitCode = 1` if `report.FinalStatus == "BLOCKED"`

---

## Governance Config System

`data/governance-config.yaml` controls the rule behavior per repo:

```yaml
governance:
  allowed_actions: [read_code, post_comment, ...]
  forbidden_actions: [push_to_main, delete_files, ...]
  scoring:
    critical_penalty: 25        # score deducted per Critical violation
    warning_penalty: 10         # score deducted per Warning
    blocked_threshold: 40       # score ≤ 40 → BLOCKED
    review_threshold: 70        # score ≤ 70 → REVIEW
  audit:
    required: true
    min_retention_days: 30
  semantic_analysis:
    enabled: true
    threshold: MEDIUM           # MEDIUM risk escalates APPROVED → REVIEW
    timeout_seconds: 5
    max_tokens: 800
```

When the file is absent or unparseable, `GovernanceConfig.Default` is used (all defaults, semantic disabled).

---

## Agent Definition Formats

AgentOps Console supports agent definitions in both **YAML** and **JSON** formats. Both formats are validated identically by the governance rule engine; the format choice is purely a matter of preference.

### YAML Format

```yaml
id: my-agent-001
name: My Agent
version: 1.0.0
description: A description of at least 10 characters for the agent
purpose: The purpose of this agent in the governance context
owner: team-name
actions:
  - read_code
  - post_comment
rate_limit:
  requests_per_minute: 60
timeout_seconds: 30
environments:
  - development
  - staging
audit:
  log_all_actions: true
  retention_days: 90
rules:
  - rule_name_1
  - rule_name_2
tools:
  - tool_name_1
  - tool_name_2
exceptions:
  - rule: "Allowed Actions Whitelist"
    reason: "Approved by security team for migration"
    approved_by: "security-team"
    expires_at: "2026-06-01T00:00:00Z"
```

### JSON Format

```json
{
  "id": "my-agent-001",
  "name": "My Agent",
  "version": "1.0.0",
  "description": "A description of at least 10 characters for the agent",
  "purpose": "The purpose of this agent in the governance context",
  "owner": "team-name",
  "actions": [
    "read_code",
    "post_comment"
  ],
  "rate_limit": {
    "requests_per_minute": 60
  },
  "timeout_seconds": 30,
  "environments": [
    "development",
    "staging"
  ],
  "audit": {
    "log_all_actions": true,
    "retention_days": 90
  },
  "rules": [
    "rule_name_1",
    "rule_name_2"
  ],
  "tools": [
    "tool_name_1",
    "tool_name_2"
  ],
  "exceptions": [
    {
      "rule": "Allowed Actions Whitelist",
      "reason": "Approved by security team for migration",
      "approved_by": "security-team",
      "expires_at": "2026-06-01T00:00:00Z"
    }
  ]
}
```

Both formats map to the same `AgentDefinition` model and produce identical governance evaluation results. The CLI `validate-agent` command auto-detects the format based on file extension (`.yaml`, `.yml`, or `.json`).

---

## Exception System

An agent definition can declare governance exceptions for temporarily approved violations:

```yaml
exceptions:
  - rule_name: "Allowed Actions Whitelist"
    justification: "Approved by security team for migration window"
    approved_by: "security-team"
    expires_at: "2026-06-01T00:00:00Z"
```

The engine downgrades matching Critical violations to Warning for the exception period. Once `expires_at` passes, the exception is no longer applied.

---

## Semantic Analysis Merge Logic

After the rule engine runs, semantic analysis is optionally applied:

| Rule-based status | Semantic risk | Final status |
|---|---|---|
| APPROVED | LOW | APPROVED |
| APPROVED | MEDIUM | REVIEW |
| APPROVED or REVIEW | HIGH | BLOCKED |
| BLOCKED | any | BLOCKED (unchanged) |

The rule-based BLOCKED result is **never overridden** by semantic analysis.
If Azure is unavailable (missing creds, timeout, invalid response), semantic is skipped silently.

---

## Exit Code Contract (Phase 10)

| FinalStatus | CLI exit code | GitHub check | Merge |
|---|---|---|---|
| APPROVED | 0 | ✅ pass | Allowed |
| REVIEW | 0 | ✅ pass | Allowed (warning in log) |
| BLOCKED | **1** | ❌ fail | **Blocked** |

`Environment.ExitCode = 1` is used (not `Environment.Exit(1)`) so async cleanup runs before process exit.

---

## User-Defined Rules

Users can define and apply custom governance rules in each execution without modifying code. Rules can be loaded from YAML files, specified inline via CLI flags, or configured interactively.

### Via YAML File

Load a pre-defined rules file:

```bash
dotnet run -- validate-agent agent.yaml --rules my-rules.yaml
```

Example rules file structure:

```yaml
rules:
  name: "Custom Rules"
  description: "Custom governance rules"
  
  actions:
    allowed:
      - read_code
      - post_comment
      - read_files
    forbidden:
      - push_to_main
      - delete_files

  requirements:
    owner_required: true
    audit_required: true
    version_required: true

  scoring:
    critical_penalty: 30
    warning_penalty: 15
    blocked_threshold: 60
    review_threshold: 85
```

### Via Inline Flags

Specify rules directly in the command:

```bash
dotnet run -- validate-agent agent.yaml \
  --allow "read_code,post_comment,read_files" \
  --forbid "push_to_main,delete_files,execute_code" \
  --require-owner \
  --require-audit \
  --min-score 75 \
  --block-score 45
```

### Interactive Mode

The system guides users through rule configuration step by step:

```bash
dotnet run -- validate-agent agent.yaml --interactive
```

The interactive mode:
1. Prompts for a rule set name
2. Asks for allowed and forbidden actions (with defaults suggested)
3. Confirms owner/audit requirements
4. Sets scoring thresholds
5. Optionally saves the rules to `data/rules/` for reuse

### Rule Precedence

When multiple rule sources are specified, this order applies:

1. **`--interactive`** — If present, prompts the user and ignores all other flags
2. **`--rules <file>`** — Load base rules from file
3. **CLI flags** (`--allow`, `--forbid`, etc.) — Override or extend file-based rules
4. **`governance-config.yaml`** — Per-repo configuration (if no other flags are set)
5. **System defaults** — Hardcoded default rules (last resort)

### Pre-defined Rule Sets

Three example rule sets are included in `data/rules/`:

| File | Score Thresholds | Enforcement | Best For |
|---|---|---|---|
| `default-rules.yaml` | BLOCKED: 40, APPROVED: 70 | Balanced | General use |
| `strict-rules.yaml` | BLOCKED: 60, APPROVED: 85 | Strict | Production deployments |
| `relaxed-rules.yaml` | BLOCKED: 20, APPROVED: 50 | Permissive | Development/testing |

### User Rules API

The `UserRulesLoader` class (in `AgentOps.Application.Rules`) provides:

```csharp
// Load from YAML file
var rules = await loader.LoadFromFileAsync("path/to/rules.yaml");

// Load from CLI flags
var rules = loader.LoadFromFlags(
    allowedActions: "read_code,post_comment",
    forbiddenActions: "push_to_main",
    requireOwner: true,
    requireAudit: true,
    minScore: 80,
    blockScore: 50
);

// Merge file + flags (flags override)
var merged = loader.Merge(rulesFromFile, rulesFromFlags);

// Convert to GovernanceConfig for engine use
var config = rules.ToGovernanceConfig();
```

---

## Visión Arquitectónica

AgentOps Console debe ser una aplicación modular, escalable y segura construida con **Clean Architecture** y **Vertical Slice Architecture** para casos de uso específicos.

```
┌─────────────────────────────────────────────────────────────┐
│                  AgentOps Console                            │
├─────────────────────────────────────────────────────────────┤
│                  Presentation Layer                          │
│              (CLI Commands & Menu System)                    │
├─────────────────────────────────────────────────────────────┤
│                   Application Layer                          │
│    (Use Cases, Validators, Orchestration, Mappers)         │
├─────────────────────────────────────────────────────────────┤
│                    Domain Layer                              │
│      (Entities, Value Objects, Domain Services)            │
├─────────────────────────────────────────────────────────────┤
│                 Infrastructure Layer                         │
│   (Logging, Configuration, Repositories, Azure Client)      │
└─────────────────────────────────────────────────────────────┘
```

## Proyectos de la Solución

```
AgentOps.Console/                              (Solution Root)
├── AgentOps.Console.sln                       (Solution file)
├── src/
│   ├── AgentOps.Core/                         (Domain & Business Logic)
│   ├── AgentOps.Application/                  (Use Cases & Orchestration)
│   ├── AgentOps.Infrastructure/               (External Dependencies)
│   ├── AgentOps.CLI/                          (Console Application)
│   ├── AgentOps.Agents/                       (Agent Framework Integration)
│   └── AgentOps.Security/                     (Security Validators)
├── tests/
│   ├── AgentOps.Core.Tests/                   (Domain Tests)
│   ├── AgentOps.Application.Tests/            (Use Case Tests)
│   ├── AgentOps.Security.Tests/               (Security Tests)
│   └── AgentOps.CLI.Tests/                    (CLI Tests)
├── docs/
│   └── [All markdown files - THE CONTRACT]
└── README.md
```

## Capa de Presentación (CLI)

**Proyecto**: `AgentOps.CLI`

### Responsabilidades
- Presentar menú interactivo
- Procesar entrada de usuario
- Orquestar llamadas a Application Layer
- Formatear salida para consola
- Manejo de errores de usuario

### Componentes Principales
```csharp
public class ConsoleMenu
{
    // Menú principal interactivo
    // Opciones documentadas en AGENT_BEHAVIOR.md
}

public class CommandProcessor
{
    // Procesa comandos del usuario
    // Valida entrada
}

public class OutputFormatter
{
    // Formatea resultados para consola
}
```

### No incluye
- Lógica de negocio
- Acceso a datos
- Configuración de seguridad específica

---

## Capa de Aplicación

**Proyecto**: `AgentOps.Application`

### Responsabilidades
- Orquestar casos de uso
- Mapear DTOs a entidades
- Ejecutar validaciones
- Coordinar servicios

### Servicios Principales

```csharp
IAgentDefinitionService         // Crear/validar definiciones
IPromptValidationService        // Validar prompts
ISecurityEvaluationService      // Detectar riesgos
IEvaluationService              // Ejecutar evaluaciones
IAuditService                   // Registrar operaciones
IReportGenerationService        // Generar reportes
```

### DTOs (Data Transfer Objects)
- `CreateAgentDefinitionRequest`
- `ValidatePromptRequest`
- `EvaluationRequest`
- `AuditLogEntry`

### No incluye
- Reglas de negocio complejas (están en Domain)
- Acceso directo a base de datos (usa Repositories)
- Detalles de infraestructura

---

## Capa de Dominio

**Proyecto**: `AgentOps.Core`

### Responsabilidades
- Definir entidades y value objects
- Implementar reglas de negocio
- Validaciones de dominio
- Eventos de dominio

### Entidades Principales

```csharp
public class AgentDefinition
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Purpose { get; set; }
    public List<string> Rules { get; set; }
    public List<string> Tools { get; set; }
    public AgentConfiguration Configuration { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Prompt
{
    public string Id { get; set; }
    public string Content { get; set; }
    public string AgentId { get; set; }
    public int TokenCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SecurityRisk
{
    public string Type { get; set; }        // PromptInjection, Hallucination, etc
    public string Severity { get; set; }    // Critical, High, Medium, Low
    public string Description { get; set; }
    public List<string> Evidence { get; set; }
}

public class EvaluationResult
{
    public string AgentId { get; set; }
    public bool Passed { get; set; }
    public List<SecurityRisk> RisksDetected { get; set; }
    public DateTime EvaluatedAt { get; set; }
}
```

### Value Objects

```csharp
public record AgentRule(string Rule, string Category);
public record AgentTool(string Name, string Description);
public record SecurityPolicy(string PolicyName, string PolicyContent);
```

### Domain Services

```csharp
public interface IPromptRiskAnalyzer
{
    List<SecurityRisk> AnalyzeForRisks(Prompt prompt);
}

public interface IAgentValidator
{
    ValidationResult ValidateDefinition(AgentDefinition definition);
}
```

---

## Capa de Seguridad

**Proyecto**: `AgentOps.Security`

### Responsabilidades
- Validar prompts contra patrones de riesgo
- Detectar prompt injection
- Detectar hallucination patterns
- Aplicar políticas de seguridad

### Componentes

```csharp
IPromptInjectionDetector      // Detecta intentos de inyección
IHallucinationDetector        // Patrones comunes de alucinación
ISecurityPolicyValidator      // Valida contra políticas
ITrustBoundaryValidator       // Valida límites de confianza
```

### Reglas de Seguridad (documentadas en SECURITY.md)
- Detección de palabras clave sospechosas
- Análisis de patrones de escape
- Validación de límites de contexto
- Verificación de autorización

---

## Capa de Infraestructura

**Proyecto**: `AgentOps.Infrastructure`

### Responsabilidades
- Acceso a datos
- Logging
- Configuración
- Integración con servicios externos

### Componentes

```csharp
IAgentRepository                // Persistencia de agentes
IAuditRepository                // Persistencia de auditoría
IConfigurationProvider          // Lee configuración
ILoggerFactory                  // Proporciona loggers

// Azure Integration (preparado)
IOpenAIClient                   // Cliente de Azure OpenAI
IOpenAIDeploymentResolver       // Resuelve deployments
```

### Implementaciones

- **File-based Repository**: Almacena en JSON/YAML localmente
- **Audit Logger**: Escribe en archivo de auditoría
- **Configuration**: Usa appsettings.json + environment variables
- **Azure OpenAI**: Preparado pero sin credenciales reales

---

## Capa de Agentes

**Proyecto**: `AgentOps.Agents`

### Responsabilidades
- Encapsular Microsoft Agent Framework
- Definir herramientas estándar
- Preparar para futuras integraciones

### Componentes

```csharp
IAgentDefinitionConverter       // Convierte AgentDefinition a Agent Framework
IToolRegistry                   // Registra herramientas disponibles
IAgentOrchestrator              // Orquesta ejecución de agentes
```

### Herramientas Estándar (TOOLS.md)
- `ValidateAgentDefinition`
- `CheckSecurityRisks`
- `GenerateReport`
- `LogAuditEntry`
- `RetrieveAgentMetadata`

---

## Flujos de Datos Clave

### Flujo 1: Crear Definición de Agente

```
CLI Menu
  ↓
CreateAgentDefinitionCommand
  ↓
Application Layer (CreateAgentUseCase)
  ↓
Domain Validation (AgentValidator)
  ↓
Infrastructure (Save Repository)
  ↓
Audit Log
  ↓
CLI Output
```

### Flujo 2: Validar Prompt

```
CLI Menu
  ↓
ValidatePromptCommand
  ↓
Application Layer (PromptValidationUseCase)
  ↓
Security Layer (RiskAnalysis)
  ↓
Domain Layer (Apply Policies)
  ↓
Audit Log
  ↓
CLI Output (Risk Report)
```

### Flujo 3: Ejecutar Evaluación

```
CLI Menu
  ↓
RunEvaluationCommand
  ↓
Application Layer (EvaluationUseCase)
  ↓
Agent Framework Integration
  ↓
Security Validation
  ↓
Store Results (Repository)
  ↓
Audit Log
  ↓
Generate Report
  ↓
CLI Output
```

---

## Patrones de Diseño Utilizados

| Patrón | Ubicación | Propósito |
|--------|-----------|----------|
| **Repository** | Infrastructure | Abstracción de persistencia |
| **Dependency Injection (DI) / IoC Container** | Application | Inyección de dependencias centralized |
| **Strategy** | Security | Validadores intercambiables |
| **Factory** | Application | Crear entidades complejas |
| **Observer** | Infrastructure | Eventos de auditoría |
| **Value Object** | Domain | Objetos inmutables de valor |
| **Specification** | Domain | Lógica de validación reutilizable |

**Nota sobre inyección de dependencias**: El contenedor DI se compone en el entrypoint (Program.cs de CLI/App) y se propaga hacia las capas internas vía constructor injection. No se utiliza el patrón de localizador de servicios en runtime. Esta restricción facilita testing y evita acoplamientos ocultos.

---

## Decisiones Arquitectónicas

### D1: Clean Architecture
**Decisión**: Usar capas bien separadas  
**Razón**: Testabilidad, mantenibilidad, independencia de frameworks  
**Implicación**: Código más verboso pero más flexible

### D2: Separar Security Layer
**Decisión**: Proyecto dedicado a seguridad  
**Razón**: Cumplimiento de "Security First"  
**Implicación**: Fácil audit y cambios de políticas

### D3: File-based Storage en MVP
**Decisión**: JSON/YAML local, no BD  
**Razón**: Simplificar MVP, facilitar migración  
**Implicación**: Escalabilidad limitada a inicios

### D4: Microsoft Agent Framework como Dependency
**Decisión**: Encapsular en proyecto separado  
**Razón**: Permitir cambios de versión fácilmente  
**Implicación**: Indirección en llamadas

### D5: OpenTelemetry Infrastructure
**Decisión**: Preparado pero desactivado  
**Razón**: Enterprise ready sin overhead inicial  
**Implicación**: Mínima complejidad extra

---

## Dependencias Entre Proyectos

```
AgentOps.CLI
├── depends on: AgentOps.Application
└── depends on: AgentOps.Infrastructure

AgentOps.Application
├── depends on: AgentOps.Core
├── depends on: AgentOps.Security
└── depends on: AgentOps.Agents

AgentOps.Core
└── no dependencies (except .NET)

AgentOps.Security
└── depends on: AgentOps.Core

AgentOps.Infrastructure
├── depends on: AgentOps.Core
└── depends on: AgentOps.Agents

AgentOps.Agents
└── depends on: AgentOps.Core
```

**Regla**: No hay dependencias circulares. Flujo siempre hacia adentro.

---

## Estructura de Carpetas Internas

### Cada proyecto sigue esta estructura:

```
AgentOps.Core/
├── Entities/              (AgentDefinition, Prompt, etc)
├── ValueObjects/          (AgentRule, SecurityPolicy, etc)
├── Services/              (Domain Services)
├── Specifications/        (Domain Specifications)
└── Exceptions/            (Domain Exceptions)

AgentOps.Application/
├── UseCases/              (CreateAgent, ValidatePrompt, etc)
├── Services/              (Application Services)
├── DTOs/                  (Request/Response objects)
├── Mappers/               (Entity ↔ DTO conversion)
└── Exceptions/            (Application Exceptions)

AgentOps.Security/
├── Validators/            (PromptValidator, etc)
├── Detectors/             (PromptInjectionDetector, etc)
├── Policies/              (SecurityPolicy implementations)
└── Models/                (SecurityRisk, etc)

AgentOps.Infrastructure/
├── Repositories/          (AgentRepository, etc)
├── Logging/               (ILogger implementations)
├── Configuration/         (Config providers)
├── Azure/                 (Azure clients)
└── Persistence/           (Storage implementations)
```

---

## Clean Architecture + Vertical Slice: Cómo Conviven

### Clean Architecture (define capas y dependencias)

La arquitectura limpia establece las fronteras inmutables:
- Las capas exteriores (Presentation, Infrastructure) dependen de capas interiores (Application, Domain)
- Las capas interiores NUNCA conocen a las exteriores (regla de dependencia)
- Esto facilita testabilidad y mantenibilidad

### Vertical Slice Architecture (organiza features)

Dentro de Application/UseCases, cada feature (p.ej. "CreateAgent", "ValidatePrompt") está agrupada verticalmente:
- CreateAgent/ (contiene: Request, Handler, Validator, Mapper)
- ValidatePrompt/ (contiene: Request, Handler, Validator, Mapper)
- Cada slice es independiente y cohesivo

### Compatibilidad

No hay conflicto:
- Clean Architecture estructura por **responsabilidad y capas**
- Vertical Slice organiza por **feature dentro de cada capa**
- Ambos patrones se refuerzan mutuamente

### Ejemplo visual:

```
Clean Architecture (horizontal):
Presentation → Application → Domain → Infrastructure

Vertical Slice dentro de Application:
CreateAgent (Feature)
├── CreateAgentRequest
├── CreateAgentHandler
├── CreateAgentValidator
└── CreateAgentMapper

ValidatePrompt (Feature)
├── ValidatePromptRequest
├── ValidatePromptHandler
├── PromptValidator
└── PromptMapper
```

---

## Nada se Ejecuta sin Trazabilidad

Cada operación debe:
1. ✅ Pasar validación de seguridad
2. ✅ Ser registrada en auditoría
3. ✅ Ser loggueada correctamente
4. ✅ Retornar resultado claro
5. ✅ Permitir rollback o análisis

---

**Documento versión**: 1.0  
**Aprobado**: 2026-05-07  
**Estado**: Active - Define estructura inmutable
