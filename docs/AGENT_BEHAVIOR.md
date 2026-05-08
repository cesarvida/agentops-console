# Agent Behavior Specification - AgentOps Console

## Propósito

Este documento define el comportamiento esperado de la consola, menús, comandos y respuestas de la aplicación AgentOps Console.

**NOTA**: Este documento define lo que hace la CONSOLA, no los agentes IA que maneja.

---

## Estructura General de la Aplicación

### Estado de Inicio

Cuando se inicia AgentOps Console:

1. Cargar configuración desde `appsettings.json`
2. Inicializar logging
3. Verificar carpetas de datos (crear si no existen)
4. Validar que no hay cambios sin guardar
5. Mostrar pantalla de bienvenida
6. Mostrar menú principal

### Pantalla de Bienvenida

```
╔══════════════════════════════════════════════════════════════╗
║                    AgentOps Console v1.0                     ║
║          AI Agent Governance & Validation Platform           ║
║                                                              ║
║     Security First • Precision • Traceability • Audit       ║
╚══════════════════════════════════════════════════════════════╝

[INFO] Configuration loaded from: appsettings.json
[INFO] Audit repository initialized
[INFO] Last session: 2026-05-07 14:32:00 UTC
```

---

## Menú Principal

```
═══════════════════════════════════════════════════════════════
                      AGENT OPS CONSOLE
                        Main Menu
═══════════════════════════════════════════════════════════════

1. [AGENTS]     Create/Manage Agent Definitions             [MVP]
2. [VALIDATE]   Validate Prompts & Rules                     [MVP]
3. [EVALUATE]   Run Agent Evaluations                        [MVP]
4. [AUDIT]      View Audit Logs                              [MVP]
5. [REPORTS]    Generate Technical Reports                   [MVP]
6. [CONFIG]     View/Edit Configuration                      [MVP+1]
7. [HELP]       Show Documentation                           [MVP]
8. [EXIT]       Exit Console                                 [MVP]

Select option (1-8): _

[MVP] = Disponible en Mínimo Viable Product (release inicial)
[MVP+1] = Disponible después del MVP en versión +1
[FUTURO] = Planeado para fases posteriores
```

---

## Submenú 1: AGENTS (Crear/Gestionar Definiciones) [MVP]

### Opción 1.1: Create New Agent Definition [MVP]

**Flujo**:
1. Solicitar nombre del agente
2. Solicitar descripción
3. Solicitar propósito principal
4. Solicitar reglas de comportamiento (lista)
5. Solicitar herramientas disponibles (lista)
6. Permitir seleccionar configuración predeterminada
7. Mostrar resumen
8. Preguntar confirmación
9. Guardar y registrar en auditoría
10. Mostrar ID generado

**Validaciones Obligatorias**:
- Nombre no vacío, longitud 3-100 caracteres
- Sin caracteres especiales peligrosos
- ID único (no duplicado)
- Descripción longitud mínima 10 caracteres
- Propósito claro y específico

**Ejemplo de Definición Guardada** (agentops-agent-001.json):
```json
{
  "id": "agentops-agent-001",
  "name": "DocumentReviewAgent",
  "description": "Agent specialized in reviewing technical documents for consistency",
  "purpose": "Review and validate technical documentation for security policies compliance",
  "version": "1.0",
  "createdAt": "2026-05-07T14:32:00Z",
  "rules": [
    "Must not modify original documents",
    "Must flag inconsistencies in security policies",
    "Must maintain context of document scope"
  ],
  "tools": [
    "TextAnalysis",
    "SecurityPolicyValidator",
    "DocumentMetadataReader"
  ],
  "configuration": {
    "maxTokensPerRequest": 4000,
    "temperatureDefault": 0.3,
    "allowHallucination": false,
    "requiresAudit": true
  }
}
```

### Opción 1.2: List All Agents [MVP]

**Comportamiento**:
1. Listar todos los agentes definidos
2. Mostrar tabla con: ID, Name, Purpose, Created, Status
3. Permitir filtro por nombre
4. Mostrar estadísticas

**Formato de Salida**:
```
ID                          Name                    Purpose                     Created
────────────────────────────────────────────────────────────────────────────────────────
agentops-agent-001         DocumentReviewAgent     Review technical docs       2026-05-07
agentops-agent-002         CodeAnalyzerAgent       Analyze code for issues     2026-05-06
agentops-agent-003         ComplianceCheckAgent    Check compliance rules      2026-05-05

Total agents: 3
```

### Opción 1.3: View Agent Details [MVP]

**Comportamiento**:
1. Solicitar ID del agente (con autocomplete)
2. Mostrar definición completa en formato legible
3. Mostrar fecha de creación/modificación
4. Mostrar historial de cambios resumido
5. Opción de editar o eliminar

### Opción 1.4: Edit Agent Definition [MVP+1]

**Disponibilidad**: Después del MVP

**Restricciones**:
- No se puede cambiar ID
- Se registra versión anterior en auditoría
- Cambios importantes requieren confirmación
- No se permiten cambios que violen seguridad

**Flujo**:
1. Seleccionar agente
2. Mostrar menú de campos editables
3. Permitir edición de cada campo
4. Validar cambios
5. Mostrar diff con versión anterior
6. Pedir confirmación
7. Guardar y auditar

### Opción 1.5: Delete Agent Definition [MVP+1]

**Disponibilidad**: Después del MVP

**Restricciones**:
- Pedir confirmación doble (nombre + confirmación)
- Registrar eliminación en auditoría
- Guardar backup antes de eliminar
- No permitir si está en uso

**Flujo**:
1. Seleccionar agente
2. Mostrar impacto (evaluaciones asociadas, etc)
3. Pedir confirmación: "Type agent name to confirm:"
4. Pedir confirmación final: "Really delete? (yes/no):"
5. Guardar backup en carpeta de auditoría
6. Eliminar
7. Mostrar confirmación

---

## Submenú 2: VALIDATE (Validar Prompts y Reglas) [MVP]

### Opción 2.1: Validate Prompt [MVP]

**Flujo**:
1. Seleccionar agente (o crear nuevo)
2. Ingresar/cargar prompt a validar
3. Ejecutar validación
4. Mostrar resultados detallados

**Validaciones Ejecutadas**:
- ✅ Síntaxis correcta
- ✅ Token count
- ✅ Detección de prompt injection
- ✅ Detección de patrones de alucinación
- ✅ Cumplimiento con reglas del agente
- ✅ Límites de contexto

**Formato de Salida**:
```
═══════════════════════════════════════════════════════════════
                    PROMPT VALIDATION REPORT
                    Agent: DocumentReviewAgent
═══════════════════════════════════════════════════════════════

Prompt Submitted:
───────────────────────────────────────────────────────────────
"Please review this document for security policy compliance..."

Basic Metrics:
  Length: 156 characters
  Tokens (estimated): 28 tokens
  Language: English (detected)

Security Analysis:
  [✓] No prompt injection patterns detected
  [✓] No hallucination risk patterns detected
  [✓] Context boundaries respected
  [⚠] MEDIUM: Generic agent instructions (consider being more specific)

Rule Compliance:
  [✓] Rule 1: Must not modify original documents
  [✓] Rule 2: Must flag inconsistencies
  [✓] Rule 3: Maintain document scope

Overall Assessment: ✓ VALID
Confidence: 95%

Recommendations:
  • Consider adding specific security policies to check
  • Add success criteria for the review

═══════════════════════════════════════════════════════════════
Validation completed at: 2026-05-07T14:35:00Z
```

### Opción 2.2: Batch Validate Prompts [MVP+1]

**Disponibilidad**: Después del MVP

**Comportamiento**:
1. Cargar archivo con múltiples prompts (JSON/CSV)
2. Validar cada uno
3. Generar reporte agregado
4. Guardar resultados

### Opción 2.3: Check Agent Rules Consistency [FUTURO]

**Comportamiento**:
1. Seleccionar agente
2. Analizar reglas internas
3. Detectar contradicciones
4. Detectar reglas redundantes
5. Mostrar reporte

---

## Submenú 3: EVALUATE (Ejecutar Evaluaciones) [MVP]

### Opción 3.1: Run Simple Evaluation [MVP]

**Nota MVP**: Simulada sin conexión real a Azure OpenAI

**Flujo**:
1. Seleccionar agente
2. Seleccionar prompt (o crear nuevo)
3. (Futuro) Ejecutar en Agent Framework
4. Capturar respuesta
5. Evaluar respuesta contra expectativas
6. Generar reporte
7. Guardar en auditoría

**Nota MVP**: En MVP, esta opción es preparatoria. Simula ejecución sin conectar a OpenAI real.

### Opción 3.2: Run Comprehensive Evaluation [MVP+1]

**Comportamiento**:
1. Suite de evaluaciones pre-definidas
2. Evaluar: Precisión, Seguridad, Consistencia
3. Crear reporte completo
4. Permitir comparación con evaluaciones anteriores

### Opción 3.3: View Evaluation History [MVP]

**Comportamiento**:
1. Seleccionar agente
2. Mostrar tabla de evaluaciones previas
3. Mostrar tendencias
4. Permitir comparar resultados

---

## Submenú 4: AUDIT (Ver Registros de Auditoría) [MVP]

### Opción 4.1: View Recent Audit Logs [MVP]

**Comportamiento**:
1. Mostrar últimos 20 registros
2. Mostrar: Timestamp, User, Action, Entity, Status
3. Permitir filtros por fecha/tipo de acción

**Formato**:
```
Timestamp                   User    Action              Entity                  Status
──────────────────────────────────────────────────────────────────────────────────────
2026-05-07T14:35:00Z       system  VALIDATE_PROMPT     agentops-agent-001      SUCCESS
2026-05-07T14:32:00Z       system  CREATE_AGENT        agentops-agent-001      SUCCESS
2026-05-07T14:30:00Z       system  LOAD_CONFIG         /appsettings.json       SUCCESS
```

### Opción 4.2: Search Audit Logs [MVP+1]

**Comportamiento**:
1. Filtro por fecha
2. Filtro por tipo de acción
3. Filtro por entidad
4. Filtro por estado (SUCCESS/ERROR)
5. Mostrar resultados

### Opción 4.3: Export Audit Trail [FUTURO]

**Comportamiento**:
1. Seleccionar formato (CSV, JSON, TXT)
2. Seleccionar rango de fechas
3. Generar archivo
4. Mostrar ubicación

---

## Submenú 5: REPORTS (Generar Reportes) [MVP]

### Opción 5.1: Generate Agent Summary Report [MVP]

**Contenido**:
- Metadata del agente
- Reglas definidas
- Herramientas disponibles
- Evaluaciones realizadas
- Estadísticas de uso

**Formato**: Markdown → HTML → PDF (futuro)

### Opción 5.2: Generate Security Assessment Report [MVP]

**Contenido**:
- Riesgos detectados
- Validaciones pasadas/fallidas
- Patrones de riesgo encontrados
- Recomendaciones
- Cumplimiento con políticas

### Opción 5.3: Generate Compliance Report [MVP+1]

**Contenido**:
- Checklist de requisitos
- Estado de cumplimiento
- Auditoría de cambios
- Recomendaciones de mejora

---

## Submenú 6: CONFIG (Ver/Editar Configuración) [MVP+1]

### Opción 6.1: View Current Configuration [MVP+1]

**Muestra**:
- Ubicación de appsettings.json
- Variables de entorno
- Valores cargados
- Estado de validación

### Opción 6.2: View Security Policies [MVP+1]

**Muestra**:
- Políticas activas
- Palabras clave sospechosas
- Patrones de riesgo
- Límites de contexto

### Opción 6.3: Edit Configuration (restringido) [FUTURO]

**Restricciones**:
- Solo configuración no-crítica
- Confirmar cambios
- Registrar en auditoría
- No permitir edición de secretos

---

## Submenú 7: HELP (Mostrar Documentación) [MVP]

### Opción 7.1: Show Command Reference [MVP]

Muestra lista de todos los comandos y opciones.

### Opción 7.2: Show Architecture Overview [MVP]

Muestra diagrama simplificado de la arquitectura.

### Opción 7.3: Show Examples [MVP]

Muestra ejemplos de:
- Crear definición de agente
- Validar prompt
- Ejecutar evaluación

---

## Comportamiento General de Errores

### Manejo de Errores Globales

```
[ERROR] Operation failed: Validation error on field 'agentName'
[INFO]  Reason: Name length must be between 3 and 100 characters
[INFO]  Current value: 'A'
[HELP]  Use menu option HELP for more information
```

### Validaciones de Entrada

- Nunca procesar entrada sin validación
- Mostrar error claro con razón
- Sugerir formato correcto
- Permitir reintentos sin perder sesión

### Excepciones No Manejadas

```
[CRITICAL] Unexpected error occurred
[INFO]     Error ID: EXC-20260507-143500
[INFO]     Check audit logs for details
[HELP]     Please contact support with Error ID
```

---

## Comandos Especiales

### Comando: `help`

Disponible en cualquier momento. Muestra ayuda contextual.

### Comando: `back`

Regresa al menú anterior.

### Comando: `exit` o `quit`

Salida limpia. Verifica cambios sin guardar.

### Comando: `clear`

Limpia la pantalla.

### Comando: `status`

Muestra estado actual de la sesión.

---

## Restricciones de Comportamiento

| Restricción | Razón |
|------------|--------|
| No editar definiciones en uso | Evitar inconsistencias |
| Confirmar acciones destructivas | Prevenir errores |
| Auditar todo | Trazabilidad obligatoria |
| Validar entrada siempre | Seguridad first |
| No mostrar secretos | Confidencialidad |
| Logging estructurado | Facilita debugging |
| Mensajes claros | UX profesional |

---

## Flujo de Salida (Exit)

1. Verificar cambios sin guardar
2. Si hay cambios: Preguntar si guardar
3. Mostrar resumen de sesión:
   - Acciones ejecutadas
   - Cambios guardados
   - Auditoría registrada
4. Mostrar ubicación de logs
5. Despedida profesional
6. Exit con código 0 (success) o 1 (error)

```
═══════════════════════════════════════════════════════════════
                    SESSION SUMMARY
═══════════════════════════════════════════════════════════════

Actions Executed: 5
  • Created 1 agent definition
  • Validated 3 prompts
  • Generated 1 report

Audit Trail Location: ./data/audit.log
Configuration File: ./appsettings.json

Thank you for using AgentOps Console.
═══════════════════════════════════════════════════════════════

[INFO] Exiting normally (code 0)
```

---

**Documento versión**: 1.0  
**Aprobado**: 2026-05-07  
**Estado**: Active - Define todos los comportamientos CLI
