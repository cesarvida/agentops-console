# Evaluation Plan - AgentOps Console

## Propósito

Este documento define cómo se evalúan los agentes IA construidos dentro de AgentOps Console.

Evaluación ≠ Ejecución. Evaluación es medición y validación contra criterios documentados.

---

## Principios de Evaluación

1. **Basado en Criterios Explícitos** - Qué se mide está documentado
2. **Reproducible** - Misma evaluación produce resultado similar
3. **Trazable** - Toda evaluación es auditada
4. **Escalable** - Puede crecer sin cambiar arquitectura
5. **Predecible** - Sin sorpresas
6. **Automático** - No requiere intervención manual

---

## Tipos de Evaluaciones

### Evaluación 1: Definición Validation

**Qué se evalúa**: ¿La definición del agente es válida y segura?

**Criterios**:
- ✅ Formato JSON/YAML válido
- ✅ Todos los campos requeridos presentes
- ✅ Valores en rangos permitidos
- ✅ Reglas son claras y unívocas
- ✅ No contradicciones en políticas
- ✅ Herramientas referenciadas existen
- ✅ Cumple con políticas de seguridad

**Métrica**: Pass/Fail (booleano)

**Tiempo**: < 1 segundo

**Auditoría**: Registra Pass/Fail + razón si Fail

**Ejemplo**:
```
Definition Validation: agentops-agent-001
├── Format: ✓ JSON valid
├── Required Fields: ✓ All present
├── Field Values: ✓ Valid ranges
├── Rules Coherence: ✓ No contradictions
├── Tools Exist: ✓ All registered
├── Security Policies: ✓ Compliant

Result: ✓ PASS (score: 100)
```

---

### Evaluación 2: Prompt Validation

**Qué se evalúa**: ¿El prompt es válido, seguro y alineado con el agente?

**Criterios**:
- ✅ Sintaxis correcta
- ✅ Longitud dentro de límites (< 10,000 caracteres)
- ✅ Token count estimado (< 4,000 tokens)
- ✅ Sin patrones de prompt injection
- ✅ Sin patrones de alucinación
- ✅ Sin PII expuesto
- ✅ Cumple con reglas del agente
- ✅ Autorización válida

**Métricas**:
- Validity Score: 0-100
- Risk Score: 0-100
- Compliance Score: 0-100
- Overall Pass/Fail

**Tiempo**: < 2 segundos

**Auditoría**: Registra scores + riesgos detectados

**Ejemplo**:
```
Prompt Validation Report
Prompt: "Please review this document..."
Agent: DocumentReviewAgent

Security Analysis:
├── Injection Patterns: ✓ None detected
├── Hallucination Risk: ⚠ Medium (generic instructions)
├── PII Detected: ✓ None
├── Escape Sequences: ✓ None

Metrics:
├── Validity Score: 92/100
├── Risk Score: 15/100 (Low)
├── Compliance Score: 100/100

Result: ✓ VALID (risks are low and acceptable)
```

---

### Evaluación 3: Security Assessment

**Qué se evalúa**: ¿Cuáles son los riesgos de seguridad del agente?

**Análisis**:
1. **Configuration Review**
   - ¿Token limits son razonables?
   - ¿Temperature está configurado para consistencia?
   - ¿Rate limits están activos?

2. **Rule Analysis**
   - ¿Reglas son claras?
   - ¿Reglas son ejecutables?
   - ¿Reglas cubren security concerns?

3. **Tool Risk Assessment**
   - ¿Herramientas disponibles son necesarias?
   - ¿Herramientas tienen validación?
   - ¿Herramientas son auditadas?

4. **Historical Risk Analysis**
   - ¿Prompts previos tuvieron riesgos?
   - ¿Qué tipo de riesgos?
   - ¿Tendencia?

**Métricas**:
- Security Score: 0-100
- Risk Level: Critical/High/Medium/Low
- Vulnerabilities Found: lista
- Remediation Recommendations: lista

**Tiempo**: < 5 segundos

**Auditoría**: Registro completo con recomendaciones

**Ejemplo**:
```
Security Assessment: DocumentReviewAgent

Configuration:
├── Max Tokens: 4000 ✓ Reasonable
├── Temperature: 0.3 ✓ Good for consistency
├── Rate Limits: ✓ Configured

Rule Analysis:
├── Rule Clarity: ✓ Clear rules
├── Executability: ✓ Rules are actionable
├── Security Coverage: ⚠ Missing rule: "No access to confidential docs"

Tool Risk:
├── Available Tools: ✓ Minimal set
├── Tool Validation: ✓ All have input validation
├── Tool Audit: ✓ All logged

Historical Analysis:
├── Prompts Evaluated: 5
├── Risks Detected: 1 Medium (in prompt 3)
├── Trend: ↓ Decreasing (improving)

Security Score: 82/100
Risk Level: MEDIUM

Recommendations:
1. Add rule about document confidentiality
2. Consider stricter temperature (0.1-0.2)
3. Monitor tool usage patterns
```

---

### Evaluación 4: Functional Evaluation (Futuro)

**Disponible en**: MVP+1

**Qué se evalúa**: ¿El agente produce respuestas correctas y útiles?

**Criterios** (ejemplos):
- ✅ Respuesta aborda la pregunta
- ✅ Respuesta es factuales
- ✅ Respuesta es completa
- ✅ Respuesta respeta las reglas
- ✅ Respuesta es del largo aproppiado

**Métrica**: Success Rate (0-100%)

---

### Evaluación 5: Compliance Check

**Qué se evalúa**: ¿El agente cumple con policies corporativas?

**Checklist** (ejemplo):
- ✅ No genera código malicioso
- ✅ No expone información confidencial
- ✅ Respeta límites de tokens
- ✅ Seguimiento de auditoría activado
- ✅ No acceso a datos innecesarios

**Métrica**: Compliance Score (0-100%)

**Ejemplo**:
```
Compliance Check: DocumentReviewAgent

Policy Requirements:
├── No Malicious Code Generation: ✓ PASS
├── No Confidential Data Exposure: ✓ PASS
├── Token Limits Respected: ✓ PASS
├── Audit Trail Enabled: ✓ PASS
├── Principle of Least Privilege: ✓ PASS

Compliance Score: 100/100
Status: ✓ COMPLIANT
```

---

## Ciclo de Evaluación

### Fase 1: Pre-Deployment Evaluation

**Cuándo**: Antes de usar agente en cualquier contexto

**Evaluaciones**:
1. Definition Validation
2. Prompt Validation (con ejemplos)
3. Security Assessment
4. Compliance Check

**Criterios de Pase**:
- ✓ Definition Validation: PASS
- ✓ Prompt Validation: PASS (todos los ejemplos)
- ✓ Security Assessment: Risk Level ≤ MEDIUM
- ✓ Compliance Check: Score ≥ 90%

**Si alguno falla**:
- 🔴 RECHAZAR deployment
- Generar reporte de issues
- Requiere remediación antes de retry

---

### Fase 2: Periodic Re-evaluation

**Cuándo**: Cada 30 días (configurable)

**Evaluaciones**: Todas las de Pre-Deployment

**Razón**: Detectar degeneración o cambios de contexto

---

### Fase 3: Change Impact Evaluation

**Cuándo**: Después de cualquier cambio a agente

**Evaluaciones**: Todas las de Pre-Deployment

**Razón**: Asegurar que cambio no introduce riesgos

---

## Métricas de Evaluación

### Métrica 1: Validity Score

```
Rango: 0-100
Fórmula: (campos válidos / campos totales) * 100

Interpretar:
  90-100: Excelente
  75-89:  Bueno
  50-74:  Aceptable (revisar)
  <50:    Rechazar
```

### Métrica 2: Security Score

```
Rango: 0-100
Componentes:
  - No injection patterns detected: +40
  - No hallucination risks: +30
  - No PII exposed: +20
  - Complies with policies: +10

Interpretar:
  90-100: Excelente
  70-89:  Bueno
  50-69:  Aceptable (revisar)
  <50:    Alto riesgo (rechazar)
```

### Métrica 3: Compliance Score

```
Rango: 0-100
Fórmula: (requisitos cumplidos / requisitos totales) * 100

Criterio de pase: ≥ 90%
```

---

## Reportes de Evaluación

### Formato Estándar de Reporte

```markdown
# Evaluation Report

**Report ID**: EVL-20260507-143500-001
**Agent**: agentops-agent-001
**Evaluated At**: 2026-05-07T14:35:00Z
**Evaluator**: System
**Overall Status**: ✓ PASS

## Executive Summary

Agent "DocumentReviewAgent" has been evaluated against all security and compliance criteria.
The agent is ready for deployment with recommendations noted below.

## Evaluation Details

### 1. Definition Validation
Status: ✓ PASS
Score: 100/100

### 2. Prompt Validation
Status: ✓ PASS
Score: 92/100
- Valid: 156 chars, 28 tokens
- Risks: 0 critical, 1 medium (acceptable)

### 3. Security Assessment
Status: ✓ PASS
Score: 82/100
- Security concerns identified: 1 (documented below)
- Risk level: MEDIUM

### 4. Compliance Check
Status: ✓ PASS
Score: 95/100

## Risks Detected

| ID | Type | Severity | Description | Remediation |
|---|---|---|---|---|
| R001 | Rule Clarity | Medium | Rule about document confidentiality missing | Add rule before sensitive use |

## Recommendations

1. Add explicit rule about handling confidential documents
2. Monitor first 10 prompts for consistency
3. Review rules after first 100 prompts

## Approval

✓ This agent is approved for use.

Evaluation completed by: AgentOps Console v1.0
```

---

## Restricciones en MVP

**En MVP, las evaluaciones son simuladas, no reales**:

- ✅ Definition Validation: REAL
- ✅ Prompt Validation: REAL (sin Azure OpenAI)
- ✅ Security Assessment: REAL (detección de patrones)
- ✅ Compliance Check: REAL (validación local)
- ⏰ Functional Evaluation: FUTURE (requiere Azure OpenAI)

---

## Evaluación de la Evaluación

**Cómo sabemos que la evaluación es buena**:

1. **Tests**: Toda evaluación tiene tests unitarios
2. **Regression Tests**: Cambios no rompen evaluaciones previas
3. **Audit Trail**: Evaluaciones previas son auditables
4. **Consistency**: Mismas evaluaciones producen mismos resultados
5. **False Positive Rate**: Bajo (< 5%)

---

## Future Enhancements

- [ ] Machine learning para mejorar detection
- [ ] Custom evaluators por agente
- [ ] Comparative analysis (vs benchmarks)
- [ ] Automated remediation recommendations
- [ ] Integration with CI/CD pipelines

---

**Documento versión**: 1.0  
**Aprobado**: 2026-05-07  
**Estado**: Active - Define evaluaciones completas
