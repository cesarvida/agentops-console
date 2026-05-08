# Tools & Integration Specification - AgentOps Console

## Propósito

Este documento define las herramientas disponibles para los agentes IA dentro de AgentOps Console y cómo se integran con Microsoft Agent Framework.

---

## Principios de Diseño de Herramientas

1. **Seguridad First** - Cada herramienta valida entrada y autorización
2. **Auditable** - Toda invocación es registrada
3. **Modular** - Independientes, componibles
4. **Documentada** - Cada herramienta tiene especificación clara
5. **Testeable** - Cada herramienta tiene tests unitarios
6. **Sin Side Effects Inesperados** - Comportamiento predecible

---

## Herramientas Estándar Obligatorias

### Tool 1: ValidateAgentDefinition

**Propósito**: Validar que una definición de agente cumple con estándares de seguridad y formato.

**Entrada**:
```json
{
  "agentDefinitionId": "agentops-agent-001",
  "validateSecurity": true,
  "validateFormat": true
}
```

**Salida**:
```json
{
  "isValid": true,
  "errors": [],
  "warnings": ["Consider being more specific in rules"],
  "validationScore": 95
}
```

**Validaciones Internas**:
- ✅ Formato JSON/YAML correcto
- ✅ Campos obligatorios presentes
- ✅ Reglas coherentes
- ✅ No contradicciones en políticas
- ✅ Herramientas referenciadas existen
- ✅ ID único
- ✅ Cumplimiento con políticas de seguridad

**Restricciones**:
- Máximo 100 reglas por agente
- Nombre longitud máxima 100 caracteres
- Descripción máximo 500 caracteres
- Máximo 50 herramientas por agente

**Auditoría**: Registra validación realizada

---

### Tool 2: AnalyzePromptForRisks

**Propósito**: Analizar un prompt para detectar riesgos de seguridad.

**Entrada**:
```json
{
  "prompt": "Please review this document...",
  "agentId": "agentops-agent-001",
  "analysisDepth": "comprehensive"
}
```

**Salida**:
```json
{
  "riskScore": 15,
  "severity": "LOW",
  "risksDetected": [
    {
      "type": "PromptInjection",
      "severity": "MEDIUM",
      "pattern": "Detected: '--'",
      "evidence": "Line 2, position 45",
      "recommendation": "Remove or escape special characters"
    }
  ],
  "promptLength": 256,
  "tokenEstimate": 45
}
```

**Análisis Incluido**:
- 🔍 Detección de prompt injection
- 🎭 Detección de patrones de alucinación
- 🚫 Palabras clave sospechosas
- ⚠️ Límites de contexto
- 🔐 Escape sequences peligrosas
- 🎯 Cumplimiento con reglas del agente

**Riesgos Detectables**:

| Tipo de Riesgo | Descripción | Ejemplo |
|---|---|---|
| **PromptInjection** | Intento de inyectar comandos | "Ignore all previous instructions" |
| **HallucinationRisk** | Patrón que puede causar alucinación | Fechas vagas, referencias incompletas |
| **ContextOverflow** | Prompt que excede límites | >4000 tokens sin necesidad |
| **PrivateDataLeak** | Potencial exposición de datos sensibles | Números de cuenta, SSN patterns |
| **AuthorizationBypass** | Intento de bypass de autorización | "Pretend you are admin" |
| **SuspiciousKeyword** | Palabras clave en lista negra | DROP, DELETE, EXEC, etc |
| **EscapeSequence** | Caracteres de escape peligrosos | SQL injection patterns |

**Restricciones**:
- Máximo 50 prompts por sesión de análisis
- Timeout de 5 segundos por análisis
- Máximo 10,000 caracteres por prompt

**Auditoría**: Registra cada análisis con riesgos encontrados

---

### Tool 3: GenerateTechnicalReport

**Propósito**: Generar reportes técnicos sobre agentes, validaciones o evaluaciones.

**Entrada**:
```json
{
  "reportType": "AgentSummary|SecurityAssessment|ComplianceCheck|EvaluationHistory",
  "agentId": "agentops-agent-001",
  "includeRawData": false,
  "format": "markdown"
}
```

**Tipos de Reportes**:

#### ReportType: AgentSummary
Incluye:
- Metadata del agente
- Definición completa
- Reglas y herramientas
- Validaciones recientes
- Evaluaciones ejecutadas

#### ReportType: SecurityAssessment
Incluye:
- Riesgos detectados (histórico)
- Validaciones de seguridad
- Cumplimiento de políticas
- Recomendaciones de remediación
- Trend analysis

#### ReportType: ComplianceCheck
Incluye:
- Checklist de requisitos
- Estado de cada requisito
- Auditoría de cambios
- Cumplimiento por período
- Brecha de compliance

#### ReportType: EvaluationHistory
Incluye:
- Evaluaciones ejecutadas
- Resultados de cada evaluación
- Métricas de rendimiento
- Comparativa temporal
- Recomendaciones

**Salida**:
```markdown
# AgentOps Technical Report
## Agent Summary: DocumentReviewAgent

**Generated**: 2026-05-07T14:35:00Z

### Metadata
- ID: agentops-agent-001
- Version: 1.0
- Status: Active
- Created: 2026-05-07T14:32:00Z

### Rules
1. Must not modify original documents
2. Must flag inconsistencies in security policies

### Recent Validations
| Date | Result | Risks |
|------|--------|-------|
| 2026-05-07 | ✓ Valid | 0 |
| 2026-05-06 | ✓ Valid | 1 Medium |

### Recommendations
- Consider adding specific policies to validate
```

**Formatos Soportados**:
- Markdown (por defecto)
- JSON (raw data)
- HTML (futuro)
- PDF (futuro)

**Restricciones**:
- Reportes máximo 100 páginas
- Historial máximo 90 días de datos
- Timeout de 10 segundos por generación

**Auditoría**: Registra generación de reportes

---

### Tool 4: LogAuditEntry

**Propósito**: Registrar entrada manual en auditoría (uso interno).

**Entrada**:
```json
{
  "action": "CREATE_AGENT|VALIDATE_PROMPT|RUN_EVALUATION|etc",
  "entityType": "Agent|Prompt|Evaluation|Configuration",
  "entityId": "agentops-agent-001",
  "status": "SUCCESS|ERROR|WARNING",
  "details": "Additional context"
}
```

**Salida**:
```json
{
  "auditId": "AUD-20260507-143500-001",
  "timestamp": "2026-05-07T14:35:00Z",
  "recorded": true
}
```

**Acciones Auditables**:
- CREATE_AGENT
- UPDATE_AGENT
- DELETE_AGENT
- VALIDATE_PROMPT
- ANALYZE_RISKS
- RUN_EVALUATION
- GENERATE_REPORT
- LOAD_CONFIG
- CHANGE_CONFIG
- VIEW_AUDIT
- EXPORT_DATA

**Restricciones**:
- Solo acciones pre-definidas
- Máximo 500 caracteres en details
- Registra automáticamente timestamp, usuario, IP (futura)

**Auditoría**: Es auditoría, por lo tanto self-auditing

---

### Tool 5: RetrieveAgentMetadata

**Propósito**: Obtener metadata de un agente sin ejecutar lógica compleja.

**Entrada**:
```json
{
  "agentId": "agentops-agent-001",
  "includeRules": true,
  "includeTools": true,
  "includeHistory": false
}
```

**Salida**:
```json
{
  "agent": {
    "id": "agentops-agent-001",
    "name": "DocumentReviewAgent",
    "status": "Active",
    "createdAt": "2026-05-07T14:32:00Z",
    "updatedAt": "2026-05-07T14:33:00Z",
    "version": "1.0"
  },
  "rules": [...],
  "tools": [...],
  "statistics": {
    "validationsRun": 5,
    "evaluationsRun": 2,
    "lastValidation": "2026-05-07T14:35:00Z"
  }
}
```

**Restricciones**:
- Datos de solo lectura
- Caché de máximo 5 minutos
- Sin incluir contenido sensible

**Auditoría**: Registra acceso a metadata

---

## Herramientas Futuras (No en MVP)

Estas herramientas están documentadas para referencia futura pero NO se implementan en MVP:

### Tool F1: ExecuteAgentInference (Futuro)
Ejecutar inferencia real usando Azure OpenAI.
```json
{
  "agentId": "agentops-agent-001",
  "prompt": "...",
  "maxTokens": 500,
  "temperature": 0.3
}
```

### Tool F2: ValidateResponseQuality (Futuro)
Evaluar calidad de respuesta generada.

### Tool F3: PersistToAzureAIFoundry (Futuro)
Persistir definiciones a Azure AI Foundry.

### Tool F4: SyncWithCentralRegistry (Futuro)
Sincronizar con registro central de agentes.

---

## Interfaz de Herramientas

### Contrato de Interfaz Estándar (Type-Safe)

Toda herramienta DEBE implementar una versión genérica type-safe:

```csharp
// Interfaz genérica con type safety fuerte
public interface ITool<TInput, TOutput>
{
    string Name { get; }
    string Description { get; }
    string Version { get; }
    
    // Validar entrada antes de ejecutar (typed)
    ValidationResult ValidateInput(TInput input);
    
    // Ejecutar herramienta (typed in/out)
    Task<ToolResult<TOutput>> ExecuteAsync(TInput input);
    
    // Retornar schema de entrada/salida
    ToolSchema GetSchema();
}

// Resultado type-safe con genéricos
public class ToolResult<TOutput>
{
    public bool Success { get; set; }
    public TOutput Data { get; set; }
    public List<string> Errors { get; set; }
    public List<string> Warnings { get; set; }
    public string ExecutionId { get; set; }
    public DateTime ExecutedAt { get; set; }
}

// Ejemplo de herramienta concreta:
public class ValidateAgentDefinitionTool : ITool<AgentDefinition, ValidationResult>
{
    public string Name => "ValidateAgentDefinition";
    public string Description => "Valida que una definición de agente cumple con estándares";
    public string Version => "1.0";
    
    public ValidationResult ValidateInput(AgentDefinition input)
    {
        if (input == null)
            return new ValidationResult { IsValid = false, Errors = new() { "Input cannot be null" } };
        // más validación...
        return new ValidationResult { IsValid = true };
    }
    
    public async Task<ToolResult<ValidationResult>> ExecuteAsync(AgentDefinition input)
    {
        var validation = ValidateInput(input);
        if (!validation.IsValid)
        {
            return new ToolResult<ValidationResult>
            {
                Success = false,
                Errors = validation.Errors
            };
        }
        
        // Lógica de validación...
        var result = new ValidationResult { IsValid = true };
        return new ToolResult<ValidationResult>
        {
            Success = true,
            Data = result,
            ExecutedAt = DateTime.UtcNow,
            ExecutionId = Guid.NewGuid().ToString()
        };
    }
    
    public ToolSchema GetSchema()
    {
        // Retorna schema del input/output
        return new ToolSchema { /* ... */ };
    }
}
```

### Notas importantes sobre Type Safety:

1. **Genéricos obligatorios**: Nunca usar `object` en herramientas internas. Cada herramienta declara sus tipos de entrada/salida explícitamente.

2. **Mapeo JSON ↔ Tipos**: Aunque JSON es el contrato externo (p.ej. con Agent Framework), internamente se mapea a DTOs fuertemente tipados:

```csharp
// JSON externo (contrato con Agent Framework)
{
  "toolId": "unique-identifier",
  "version": "1.0",
  "parameters": { /* input data */ }
}

// Mapeo interno a tipo fuerte
var dto = JsonConvert.DeserializeObject<AgentDefinition>(json);
var result = await tool.ExecuteAsync(dto);
```

3. **IMPORTANTE**: Las Tools de AgentOps Console (herramientas de la plataforma que gestionan agentes) son diferentes de las Tools/Capabilities que un agente puede usar. No confundir:
   - **AgentOps Tools** = ValidateAgentDefinition, AnalyzePromptForRisks, etc (plataforma)
   - **Agent Tools** = Las herramientas que el agente definido puede invocar en runtime (el agente gestionado)

---

## Integración con Microsoft Agent Framework

### Mapeo de Herramientas a Agent Framework

Cuando un agente usa AgentOps Tools:

1. **Definición en Agent Framework**:
   ```csharp
   agent.AddTool(new AgentOpsToolAdapter("ValidateAgentDefinition"));
   agent.AddTool(new AgentOpsToolAdapter("AnalyzePromptForRisks"));
   ```

2. **Interceptor de Seguridad**:
   - Toda invocación pasa por validador de seguridad
   - Verificar permisos antes de ejecutar
   - Registrar en auditoría

3. **Result Mapping**:
   - Convertir ToolResult a Agent Framework response
   - Mantener trazabilidad

---

## Restricciones de Ejecución de Herramientas

### Políticas Globales

1. **No Herramientas Sin Auditoría**
   - Cada invocación se registra
   - Incluye timestamp, usuario, input, output

2. **Validación Obligatoria**
   - Input validado antes de ejecutar
   - Schema checking
   - Type safety

3. **Timeout Obligatorio**
   - Máximo 30 segundos por herramienta
   - Máximo 5 minutos por cadena de herramientas

4. **Rate Limiting**
   - Máximo 100 invocaciones por minuto
   - Máximo 1000 por sesión

5. **Isolamiento**
   - Herramientas no acceden a datos una de otra
   - No comparten estado
   - Cada una tiene su scope

---

## Formato Estándar de Entrada/Salida

### Todas las herramientas siguen este contrato:

**Entrada**:
```json
{
  "toolId": "unique-identifier",
  "version": "1.0",
  "timestamp": "2026-05-07T14:35:00Z",
  "requestId": "REQ-20260507-143500-001",
  "parameters": { /* tool-specific */ }
}
```

**Salida**:
```json
{
  "success": true,
  "requestId": "REQ-20260507-143500-001",
  "executionId": "EXC-20260507-143500-001",
  "timestamp": "2026-05-07T14:35:01Z",
  "executionTimeMs": 1234,
  "data": { /* tool-specific */ },
  "errors": [],
  "warnings": []
}
```

---

## Herramientas y Seguridad

### Validaciones de Seguridad Obligatorias por Herramienta

| Herramienta | Validación |
|-------------|-----------|
| ValidateAgentDefinition | Schema validation, policy compliance |
| AnalyzePromptForRisks | Injection patterns, escape sequences |
| GenerateTechnicalReport | Authorization check, data classification |
| LogAuditEntry | Action validation, entity existence |
| RetrieveAgentMetadata | Authorization check |

---

## Testing de Herramientas

Cada herramienta DEBE tener tests unitarios:

```csharp
[Fact]
public void ValidateAgentDefinition_WithValidInput_ReturnsSuccess()
{
    // Arrange
    var input = new { agentDefinitionId = "valid-id" };
    
    // Act
    var result = _tool.Execute(input);
    
    // Assert
    Assert.True(result.Success);
}

[Fact]
public void ValidateAgentDefinition_WithInvalidInput_ReturnsError()
{
    // Arrange
    var input = new { agentDefinitionId = "" };
    
    // Act
    var result = _tool.Execute(input);
    
    // Assert
    Assert.False(result.Success);
    Assert.NotEmpty(result.Errors);
}
```

---

**Documento versión**: 1.0  
**Aprobado**: 2026-05-07  
**Estado**: Active - Define todas las herramientas disponibles
