# AgentOps Console

> El portero que decide qué agentes IA pueden desplegarse.

## ¿Qué hace?

AgentOps Console es un **agente guardián** que supervisa otros agentes IA. Cuando alguien sube un agente IA nuevo a GitHub:

1. ✅ Lo analiza automáticamente
2. 🔍 Comprueba si cumple las reglas de seguridad
3. 📋 Publica un reporte detallado en el PR
4. 🛡️ Bloquea o aprueba el despliegue

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

**Version:** 0.2.0 | **Status:** Production Ready ✅
