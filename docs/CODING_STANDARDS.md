# Coding Standards - AgentOps Console

## Propósito

Este documento define los estándares de codificación para AgentOps Console.

**Estos estándares NO son sugerencias. Son obligatorios.**

---

## Principios Fundamentales

1. **Legibilidad sobre Cleverness** - Código simple, claro, fácil de entender
2. **Seguridad Integrada** - Seguridad en cada línea
3. **Testeable por Defecto** - Código escrito para ser testeado
4. **Auditable** - Cada cambio es traceable
5. **Consistencia** - Mismos patrones en todo el proyecto
6. **Documentación Embebida** - Código autoexplicativo

---

## Estructura de Naming

### Namespaces

```csharp
// Patrón: AgentOps.{Layer}.{Feature}

// Correcto ✓
namespace AgentOps.Core.Entities
namespace AgentOps.Application.UseCases
namespace AgentOps.Security.Detectors
namespace AgentOps.Infrastructure.Repositories

// Incorrecto ❌
namespace AgentOps.Models
namespace AgentOps.Helpers
namespace AgentOps.Utils
```

### Clases

```csharp
// Patrón: PascalCase + sufijo según tipo

// Entidades ✓
public class AgentDefinition { }
public class Prompt { }
public class SecurityRisk { }

// Services ✓
public interface IPromptValidationService { }
public class PromptValidationService { }

// Validators ✓
public interface IPromptValidator { }
public class PromptValidator : IPromptValidator { }

// Detectors ✓
public interface IPromptInjectionDetector { }
public class PromptInjectionDetector : IPromptInjectionDetector { }

// Repositories ✓
public interface IAgentRepository { }
public class AgentRepository : IAgentRepository { }

// Exceptions ✓
public class InvalidAgentDefinitionException : Exception { }
public class SecurityViolationException : Exception { }

// DTOs ✓
public class CreateAgentDefinitionRequest { }
public class ValidatePromptResponse { }

// Incorrecto ❌
public class Agent {} // Muy genérico
public class Helper {} // Evitar
public class Util {} // Evitar
public class Data {} // Muy vago
```

### Variables y Propiedades

```csharp
// Correcto ✓
public string AgentName { get; set; }
public int MaxTokens { get; set; }
public bool IsSecure { get; set; }
public DateTime CreatedAt { get; set; }

private string _agentId;
private List<string> _rules;
private const int MaxPromptLength = 10000;

// Incorrecto ❌
public string agentname { get; set; } // No camelCase en properties
public int max_tokens { get; set; } // Usar PascalCase
private string agentId; // Private debe tener _prefijo
private const int max_prompt_length = 10000; // Constants en UPPER_CASE solo si es muy importante
```

---

## Estructura de Clase

```csharp
public class AgentDefinition
{
    // 1. Constantes
    private const int MinNameLength = 3;
    private const int MaxNameLength = 100;
    
    // 2. Fields privados
    private string _id;
    
    // 3. Properties públicas
    public string Id
    {
        get => _id;
        set => _id = value ?? throw new ArgumentNullException(nameof(value));
    }
    
    public string Name { get; set; }
    
    // 4. Constructor
    public AgentDefinition(string id, string name)
    {
        ValidateId(id);
        ValidateName(name);
        
        _id = id;
        Name = name;
    }
    
    // 5. Métodos públicos
    public void UpdateName(string newName)
    {
        ValidateName(newName);
        Name = newName;
    }
    
    // 6. Métodos privados
    private void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty");
            
        if (name.Length < MinNameLength)
            throw new ArgumentException($"Name must be at least {MinNameLength} characters");
    }
    
    private void ValidateId(string id)
    {
        // Validación...
    }
}
```

---

## Interfaces

### Naming Conventions

```csharp
// Servicios: I{Name}Service
public interface IPromptValidationService { }

// Repositories: I{Name}Repository
public interface IAgentRepository { }

// Validators: I{Name}Validator
public interface IPromptValidator { }

// Factories: I{Name}Factory
public interface IAgentDefinitionFactory { }

// Detectores: I{Name}Detector
public interface IPromptInjectionDetector { }
```

### Diseño de Interfaz

```csharp
// Correcto ✓ - Interfaz clara y cohesiva
public interface IPromptValidationService
{
    ValidationResult Validate(string prompt);
    List<ValidationWarning> GetWarnings(string prompt);
}

// Incorrecto ❌ - Interfaz demasiado grande
public interface IEverything
{
    void DoThis();
    void DoThat();
    void DoSomethingElse();
    // ... 50 métodos más
}
```

---

## Métodos

### Validación Temprana

```csharp
// Correcto ✓ - Guard clauses al inicio
public void CreateAgent(AgentDefinition definition)
{
    if (definition == null)
        throw new ArgumentNullException(nameof(definition));
        
    if (string.IsNullOrWhiteSpace(definition.Name))
        throw new ArgumentException("Agent name is required");
    
    // Lógica principal...
}

// Incorrecto ❌ - Validación al final
public void CreateAgent(AgentDefinition definition)
{
    if (definition != null && !string.IsNullOrWhiteSpace(definition.Name))
    {
        // Lógica...
    }
}
```

### Documentación Mínima Obligatoria

```csharp
/// <summary>
/// Validates a prompt against security policies and agent rules.
/// </summary>
/// <param name="prompt">The prompt to validate. Must not be null or empty.</param>
/// <param name="agentId">The ID of the agent to validate against.</param>
/// <returns>
/// A ValidationResult indicating whether the prompt is valid.
/// If invalid, contains list of errors.
/// </returns>
/// <exception cref="ArgumentNullException">Thrown when prompt or agentId is null.</exception>
/// <exception cref="InvalidOperationException">Thrown when agent not found.</exception>
public ValidationResult ValidatePrompt(string prompt, string agentId)
{
    // Implementation...
}
```

---

## Manejo de Errores

### Excepciones Personalizadas

```csharp
// Crear excepciones específicas del dominio
public class InvalidAgentDefinitionException : DomainException
{
    public InvalidAgentDefinitionException(string message) 
        : base(message) { }
}

public class SecurityViolationException : DomainException
{
    public string RiskType { get; }
    
    public SecurityViolationException(string message, string riskType) 
        : base(message)
    {
        RiskType = riskType;
    }
}

// NO usar excepciones genéricas
// ❌ throw new Exception("Invalid agent");
// ✓ throw new InvalidAgentDefinitionException("Agent name is required");
```

### Try-Catch

```csharp
// Correcto ✓ - Específico y manejable
try
{
    var agent = await _repository.GetAgent(agentId);
}
catch (AgentNotFoundException ex)
{
    _logger.LogWarning($"Agent not found: {agentId}");
    return Result.NotFound();
}
catch (RepositoryException ex)
{
    _logger.LogError($"Database error: {ex.Message}");
    throw; // Re-throw infrastructure errors
}

// Incorrecto ❌ - Genérico
try
{
    // Code...
}
catch (Exception ex)
{
    // Don't do this
}
```

---

## LINQ

### Estilo

```csharp
// Correcto ✓ - Query syntax para queries complejas
var validAgents = from agent in _repository.GetAll()
                  where agent.IsActive
                  where agent.SecurityScore >= 80
                  select agent;

// Correcto ✓ - Method syntax para queries simples
var agents = _repository.GetAll()
    .Where(a => a.IsActive)
    .OrderBy(a => a.CreatedAt)
    .ToList();

// Incorrecto ❌ - Mezclar innecesariamente
var agents = _repository.GetAll().Where(a =>
{
    if (a.IsActive)
    {
        if (a.SecurityScore >= 80)
        {
            return true;
        }
    }
    return false;
}).ToList();
```

---

## Logging

### Niveles de Log

```csharp
// LogLevel.Critical - Fallo del sistema
_logger.LogCritical("Database connection failed: {error}", ex.Message);

// LogLevel.Error - Error operacional
_logger.LogError("Agent creation failed: {agentId}", agentId);

// LogLevel.Warning - Algo inesperado pero recuperable
_logger.LogWarning("Prompt validation detected medium risk: {riskType}", riskType);

// LogLevel.Information - Información general importante
_logger.LogInformation("Agent created successfully: {agentId}", agentId);

// LogLevel.Debug - Detalles para debugging
_logger.LogDebug("Validating prompt: {promptLength} characters", prompt.Length);

// NO hacer ❌
_logger.LogInformation("value=" + value); // No concatenar strings
_logger.LogInformation($"value={value}"); // Mejor: structured logging
_logger.LogInformation("value={value}", value); // ✓ Mejor: indexed logging
```

---

## Console Output Policy

### Regla: Dónde se permite Console Output

**Prohibido en Core, Application, Infrastructure, Security, Agents**:
- Nunca usar `Console.WriteLine()` directamente
- Solo usar `ILogger` con structured logging
- Razón: Estas capas son reutilizables y no deben acoplarse a consola

**Permitido en CLI/Presentation Layer únicamente**:
- Solo a través de interfaz `IConsoleWriter` o `OutputFormatter`
- Mantener abstracción para permitir tests e inyección

### Ejemplo de abstracción en CLI:

```csharp
// En AgentOps.CLI
public interface IConsoleWriter
{
    void WriteLine(string message);
    void WriteSuccess(string message);
    void WriteError(string message);
    void WriteWarning(string message);
}

public class ConsoleWriter : IConsoleWriter
{
    public void WriteLine(string message) => Console.WriteLine(message);
    public void WriteSuccess(string message) => Console.ForegroundColor = ConsoleColor.Green;
    // etc...
}

// Uso en CLI command:
public class CreateAgentCommand
{
    private readonly IConsoleWriter _console;
    private readonly ILogger<CreateAgentCommand> _logger;
    
    public CreateAgentCommand(IConsoleWriter console, ILogger<CreateAgentCommand> logger)
    {
        _console = console;
        _logger = logger;
    }
    
    public void Execute()
    {
        _console.WriteLine("Creating agent..."); // ✓ CLI output
        _logger.LogInformation("Agent creation initiated"); // ✓ Structured logging
    }
}
```

### Regla resumida:

- ✅ **CLI**: `IConsoleWriter` para output visual
- ✅ **Todas las capas**: `ILogger` para eventos internos y auditoría
- ❌ **Nunca**: `Console.WriteLine()` fuera de la capa CLI; la capa CLI debe usar `IConsoleWriter` para facilitar tests e inyección
```

### Structured Logging

```csharp
// Correcto ✓ - Structured logging
_logger.LogInformation(
    "Agent validation completed | AgentId={agentId} | Result={result} | Duration={durationMs}ms",
    agentId,
    result.IsValid,
    stopwatch.ElapsedMilliseconds
);

// Permite que herramientas de logging extraigan campos automáticamente
```

---

## Testing

### Nomenclatura de Tests

```csharp
// Patrón: MethodName_Condition_ExpectedResult

[Fact]
public void ValidatePrompt_WithValidPrompt_ReturnsSuccess()
{
    // Arrange
    var prompt = "Valid prompt";
    
    // Act
    var result = _validator.Validate(prompt);
    
    // Assert
    Assert.True(result.IsValid);
}

[Fact]
public void ValidatePrompt_WithInjectionAttempt_ReturnsFailure()
{
    // Arrange
    var prompt = "'; DROP TABLE--";
    
    // Act
    var result = _validator.Validate(prompt);
    
    // Assert
    Assert.False(result.IsValid);
    Assert.Contains("injection", result.Error);
}
```

### AAA Pattern

```csharp
[Fact]
public void CreateAgent_WithValidDefinition_PersistsSuccessfully()
{
    // Arrange - Setup del test
    var definition = new AgentDefinition 
    { 
        Id = "test-001",
        Name = "TestAgent"
    };
    
    // Act - Ejecutar lo que se quiere probar
    var result = _service.Create(definition);
    
    // Assert - Verificar resultado
    Assert.True(result.Success);
    var persisted = _repository.GetById("test-001");
    Assert.NotNull(persisted);
    Assert.Equal("TestAgent", persisted.Name);
}
```

---

## Async/Await

```csharp
// Correcto ✓ - Usar async cuando hay I/O
public async Task<Agent> GetAgentAsync(string agentId)
{
    var agent = await _repository.GetAgentAsync(agentId);
    return agent ?? throw new AgentNotFoundException(agentId);
}

// Correcto ✓ - Métodos síncronos cuando no hay I/O
public int CalculateSecurity(Agent agent)
{
    return agent.Rules.Count * 10;
}

// Incorrecto ❌ - Usar .Result
public Agent GetAgent(string agentId)
{
    return _repository.GetAgentAsync(agentId).Result; // Deadlock potencial
}

// Incorrecto ❌ - Fire and forget
public void NotifyUser(string message)
{
    NotifyUserAsync(message); // ¿Y si falla?
}
```

---

## Null Safety

```csharp
// Correcto ✓ - Null coalescing
var name = agent?.Name ?? "Unknown";

// Correcto ✓ - Null checking
if (agent is not null)
{
    // Use agent
}

// Correcto ✓ - Nullable reference types
#nullable enable
public class AgentService
{
    public Agent? GetAgent(string id) // Puede retornar null
    {
        return _repository.GetAgent(id);
    }
    
    public string GetName(Agent agent) // Nunca retorna null
    {
        return agent.Name;
    }
}

// Incorrecto ❌
if (agent != null)
{
    // Use agent
}

// Incorrecto ❌
var name = agent?.Name != null ? agent.Name : "Unknown";
```

---

## Performance

### Guidelines

1. **No premature optimization** - Medir antes de optimizar
2. **Lazy loading** - No cargar datos que no se usan
3. **Caching** - Cache resultados computados
4. **Batch operations** - Menos roundtrips
5. **Avoid LINQ to Objects** - Para grandes colecciones

```csharp
// ❌ No recomendado - Múltiples iteraciones
public int CountValidAgents(List<Agent> agents)
{
    var active = agents.Where(a => a.IsActive).ToList();
    var secure = active.Where(a => a.SecurityScore >= 80).ToList();
    return secure.Count;
}

// ✓ Recomendado - Una sola iteración
public int CountValidAgents(List<Agent> agents)
{
    return agents.Count(a => a.IsActive && a.SecurityScore >= 80);
}
```

---

## Code Review Checklist

Antes de hacer commit:

- [ ] Código sigue naming conventions
- [ ] Métodos tienen documentación XML
- [ ] Tests unitarios pasan
- [ ] Tests incluidos para lógica nueva
- [ ] Cobertura de tests no bajó
- [ ] No hay Console.WriteLine fuera de CLI; la capa CLI debe usar `IConsoleWriter`
- [ ] No hay hardcoded secretos
- [ ] Logging es structured
- [ ] Excepciones son específicas
- [ ] No hay dead code
- [ ] No hay commented code
- [ ] Métodos tienen una responsabilidad
- [ ] Clases tienen una responsabilidad
- [ ] Interfaces están bien definidas
- [ ] Security best practices aplicadas

---

## Mejoras de Código

### Before

```csharp
public class AgentService
{
    private AgentRepository _repo;
    
    public AgentService()
    {
        _repo = new AgentRepository();
    }
    
    public Agent CreateAgent(string name, string desc, List<string> rules)
    {
        if (name == null || name == "")
            return null;
        
        var a = new Agent();
        a.Name = name;
        a.Description = desc;
        a.Rules = rules;
        _repo.Save(a);
        
        return a;
    }
}
```

### After

```csharp
/// <summary>
/// Service for managing agent definitions.
/// </summary>
public class AgentDefinitionService : IAgentDefinitionService
{
    private readonly IAgentRepository _repository;
    private readonly ILogger<AgentDefinitionService> _logger;
    
    public AgentDefinitionService(
        IAgentRepository repository,
        ILogger<AgentDefinitionService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <summary>
    /// Creates a new agent definition with validation.
    /// </summary>
    /// <param name="request">The create request with agent details.</param>
    /// <returns>
    /// A result containing the created agent or validation errors.
    /// </returns>
    public async Task<Result<AgentDefinition>> CreateAgentAsync(CreateAgentDefinitionRequest request)
    {
        if (request == null)
            return Result<AgentDefinition>.Failure("Request cannot be null");
        
        var validation = ValidateRequest(request);
        if (!validation.IsValid)
            return Result<AgentDefinition>.Failure(validation.Errors);
        
        var agent = new AgentDefinition(request.Name, request.Description, request.Rules);
        
        try
        {
            await _repository.AddAsync(agent);
            _logger.LogInformation("Agent created successfully | AgentId={agentId}", agent.Id);
            return Result<AgentDefinition>.Success(agent);
        }
        catch (RepositoryException ex)
        {
            _logger.LogError("Failed to create agent | Error={error}", ex.Message);
            return Result<AgentDefinition>.Failure("Failed to persist agent");
        }
    }
    
    private CreateAgentValidation ValidateRequest(CreateAgentDefinitionRequest request)
    {
        // Validación detallada...
    }
}
```

---

## Tools for Compliance

- EditorConfig - Consistencia de formateo
- StyleCop - Análisis estático de estilo
- SonarAnalyzer - Code quality
- Roslynator - Analizadores Roslyn adicionales

```xml
<!-- .editorconfig -->
root = true

[*.cs]
indent_size = 4
indent_style = space
trim_trailing_whitespace = true
insert_final_newline = true
```

---

**Documento versión**: 1.0  
**Aprobado**: 2026-05-07  
**Estado**: Active - Estándares obligatorios
