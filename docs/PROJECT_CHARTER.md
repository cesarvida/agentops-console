# Project Charter - AgentOps Console

## Propósito del Proyecto

Crear una aplicación de consola enterprise-grade para gobernar, crear, validar, evaluar y documentar agentes de IA construidos con Microsoft Agent Framework.

Esta NO es una herramienta genérica de IA. Es una **herramienta especializada para equipos técnicos** que necesitan crear agentes IA seguros, auditables y alineados con documentación.

## Objetivo Principal

Proporcionar a los desarrolladores y arquitectos de IA una plataforma centralizada para:
- Definir comportamiento esperado de agentes
- Validar prompts antes de ejecución
- Detectar riesgos de seguridad (prompt injection, alucinación)
- Evaluar rendimiento de agentes
- Mantener auditoría completa de operaciones
- Preparar integración futura con Azure OpenAI y Azure AI Foundry

## Principios Fundamentales

| Principio | Descripción |
|-----------|------------|
| **Seguridad First** | Toda decisión prioriza seguridad sobre funcionalidad |
| **Documentación Inmutable** | Los `.md` en `/docs` son el contrato del proyecto |
| **Trazabilidad Completa** | Cada operación es auditada y registrada |
| **Precisión Sobre Velocidad** | Preferimos correctitud a features rápidas |
| **Testing Obligatorio** | Código sin tests no es aceptado |
| **Modularidad Enterprise** | Arquitectura limpia, preparada para escalabilidad |
| **Sin Secretos Reales** | Configuración predeterminada con placeholders |

## Prioridades del Proyecto

1. **Seguridad** - Prevención de ataques y riesgos
2. **Precisión** - Exactitud en validaciones y evaluaciones
3. **Trazabilidad** - Auditoría completa de todas las operaciones
4. **Documentación** - Claridad en diseño y comportamiento
5. **Testing** - Cobertura funcional completa
6. **Mantenibilidad** - Código limpio y modular
7. **Escalabilidad Futura** - Preparado para crecimiento

## Alcance MVP (Mínimo Viable)

### ✅ Incluido

- Creación de definiciones de agentes mediante YAML/JSON
- Validación de prompts (sintaxis, tokens, patrones)
- Detección de patrones de riesgo (prompt injection, alucinación)
- Generación de reportes técnicos
- Auditoría local en archivo o base de datos local
- Configuración preparada para Azure OpenAI (sin credenciales reales)
- Estructura de herramientas (Tools) documentada pero básica

### ❌ Excluido en MVP

- Integración real con Azure OpenAI
- Interfaz gráfica (GUI)
- Machine Learning personalizado
- Base de datos remota
- SSO o autenticación compleja
- Monitoreo en tiempo real

## Tecnología Stack

| Componente | Versión/Tipo |
|-----------|------------|
| Runtime | .NET 8+ |
| Lenguaje | C# 12+ |
| Interfaz | Consola (CLI) |
| Logging | Microsoft.Extensions.Logging |
| Configuración | Microsoft.Extensions.Configuration |
| Testing | xUnit |
| Observabilidad | OpenTelemetry (preparado, no activado) |
| Agent Framework | Microsoft Agent Framework (preview) |
| Azure Integration | Configuración preparada, sin credenciales |

## Entregables del Proyecto

1. Solución .NET con estructura clara
2. Documentación completa en `/docs` (contrato inmutable)
3. Código fuente modular y testeable
4. Tests unitarios con cobertura mínima 70%
5. README con instrucciones de uso
6. Ejemplos de definiciones de agentes
7. Configuración de ejemplo

## Restricciones y Reglas

1. **No hay trucos rápidos** - Todo debe ser profesional y enterprise
2. **Documentación primero** - Los `.md` definen el proyecto, no el código
3. **Modularidad obligatoria** - Cada componente es independiente
4. **Seguridad en cada capa** - No hay paso sin validación
5. **Tests antes que features** - Testing es parte de la definición
6. **Sin dependencias opcionales** - Todo lo que se incluye es necesario
7. **Configuración explícita** - Nada de valores mágicos

## Éxito del Proyecto

El proyecto se considera exitoso cuando:

✅ Todos los documentos `.md` están completos y coherentes  
✅ Todas las clases principales están implementadas  
✅ Tests unitarios pasan con cobertura ≥70%  
✅ Auditoría registra todas las operaciones  
✅ Validación de prompts detecta riesgos documentados  
✅ Documentación es clara y actualizada  
✅ El proyecto puede ser escalado a Azure sin cambios arquitectónicos  

## Responsabilidades

| Rol | Responsabilidad |
|-----|-----------------|
| Arquitecto | Asegurar coherencia de diseño |
| Developer | Implementar según especificación |
| QA | Validar cobertura de tests |
| Tech Lead | Revisar cumplimiento de reglas |

---

**Documento aprobado**: 2026-05-07  
**Versión**: 1.0  
**Estado**: Active
