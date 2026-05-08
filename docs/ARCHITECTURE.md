# Architecture - AgentOps Console

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
