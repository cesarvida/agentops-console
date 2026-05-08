# Definition of Done - AgentOps Console

## Propósito

Este documento define qué significa que una característica, un fix o un documento esté **completamente hecho**.

Una tarea no está "hecha" hasta que cumpla TODOS los puntos en este checklist.

---

## Definition of Done Estándar

Una característica se considera "Done" cuando:

### Código

- [ ] ✅ Código escrito siguiendo CODING_STANDARDS.md
- [ ] ✅ Código compila sin errores ni warnings
- [ ] ✅ No hay TODO comments sin tracker
- [ ] ✅ No hay dead code
- [ ] ✅ No hay commented-out code
- [ ] ✅ Naming sigue convenciones
- [ ] ✅ Métodos tienen máximo 30 líneas
- [ ] ✅ Clases tienen máximo 500 líneas
- [ ] ✅ No hay duplicación de código (DRY principle)
- [ ] ✅ Complejidad ciclomática < 10

### Testing

- [ ] ✅ Tests unitarios escritos
- [ ] ✅ Tests pasan exitosamente
- [ ] ✅ Cobertura de tests ≥ 70%
- [ ] ✅ Tests negativos incluidos (casos de error)
- [ ] ✅ Tests usan AAA pattern
- [ ] ✅ Mocks/stubs usados apropiadamente
- [ ] ✅ Tests son independientes
- [ ] ✅ Tests no tienen dependencias en orden
- [ ] ✅ Unit tests corren rápido (< 1s por test, objetivo < 500ms)
- [ ] ✅ Integration tests pueden ser más lentos (ejecutar en pipeline dedicado)
- [ ] ✅ Tests son determinísticos

### Seguridad

- [ ] ✅ Validación de entrada implementada
- [ ] ✅ No hay secretos en código
- [ ] ✅ Auditoría está implementada
- [ ] ✅ Logging no expone datos sensibles
- [ ] ✅ Principio del menor privilegio aplicado
- [ ] ✅ Security review completado
- [ ] ✅ No hay vulnerabilidades conocidas

### Documentación

- [ ] ✅ Métodos públicos tienen documentación XML
- [ ] ✅ Clases públicas tienen documentación XML
- [ ] ✅ Interfaces documentadas
- [ ] ✅ Parámetros documentados con `<param>`
- [ ] ✅ Retorno documentado con `<returns>`
- [ ] ✅ Excepciones documentadas con `<exception>`
- [ ] ✅ Ejemplos incluidos donde es complejo
- [ ] ✅ README actualizado (si es necesario)
- [ ] ✅ `.md` en `/docs` actualizados (si es necesario)

### Code Review

- [ ] ✅ Código revisado por al menos 1 persona
- [ ] ✅ Todos los comentarios de review resueltos
- [ ] ✅ Review aprobado explícitamente
- [ ] ✅ No hay conflictos de merge pendientes

### Performance

- [ ] ✅ Sin N+1 queries (si aplica)
- [ ] ✅ Sin memory leaks evidentes
- [ ] ✅ Operaciones no toman > 5 segundos
- [ ] ✅ Logging no afecta performance
- [ ] ✅ Benchmarking completado (si es crítico)

### Integración

- [ ] ✅ Compila con otros proyectos
- [ ] ✅ No rompe tests existentes
- [ ] ✅ Integración visible en menú CLI (si aplica)
- [ ] ✅ Configuration actualizada (si es necesario)
- [ ] ✅ Dependencies actualizadas en .csproj

### Auditoría

- [ ] ✅ Operaciones son auditadas
- [ ] ✅ Auditoría incluye timestamp
- [ ] ✅ Auditoría incluye detalles relevantes
- [ ] ✅ Errores son auditados
- [ ] ✅ Auditoría es inmutable

---

## Definition of Done por Tipo de Trabajo

### Para Nuevas Características

Además de lo estándar:

- [ ] ✅ Requisito documentado en `.md`
- [ ] ✅ Behavior documento actualizado
- [ ] ✅ Arquiectura documento actualizado (si aplica)
- [ ] ✅ Tests de integración incluidos
- [ ] ✅ Ejemplo de uso documentado
- [ ] ✅ Backward compatibility verificada

### Para Bug Fixes

Además de lo estándar:

- [ ] ✅ Root cause documentado
- [ ] ✅ Test reproduce el bug
- [ ] ✅ Test pasa después del fix
- [ ] ✅ Regression test incluido (caso similar)
- [ ] ✅ Cambios relacionados identificados

### Para Refactoring

Además de lo estándar:

- [ ] ✅ Comportamiento no cambió (tests pasan)
- [ ] ✅ Performance no empeoró
- [ ] ✅ Cobertura de tests no bajó
- [ ] ✅ Razón del refactoring documentada
- [ ] ✅ Antes/después analizado

### Para Documentación

- [ ] ✅ Contenido es correcto y actualizado
- [ ] ✅ Ejemplos testeados (si aplica)
- [ ] ✅ Links funcionan
- [ ] ✅ Formato es consistente
- [ ] ✅ Ortografía verificada
- [ ] ✅ Referenciado desde otros docs

---

## Checklist antes de Commit

```
Antes de hacer git commit:

CÓDIGO:
  [ ] ¿Compila sin errores/warnings?
  [ ] ¿Sigue naming conventions?
  [ ] ¿Documentación XML presente?
  
TESTS:
  [ ] ¿Todos los tests pasan?
  [ ] ¿Cobertura >= 70%?
  [ ] ¿Casos negativos incluidos?
  
SEGURIDAD:
  [ ] ¿Sin secretos?
  [ ] ¿Validación de entrada?
  [ ] ¿Auditoría implementada?
  
DOCUMENTACIÓN:
  [ ] ¿.md actualizado?
  [ ] ¿README actualizado?
  [ ] ¿Ejemplos incluidos?
  
INTEGRACIÓN:
  [ ] ¿Compila con otros proyectos?
  [ ] ¿No rompe tests existentes?
  [ ] ¿Configuration actualizado?
  
REVIEW:
  [ ] ¿Code review completado?
  [ ] ¿Aprobación obtenida?
```

---

## Checklist antes de Merge a Main

```
Antes de merge a main:

CÓDIGO:
  [ ] Build exitoso en CI/CD
  [ ] Todos los tests pasan
  [ ] Sin warnings
  [ ] Code quality OK
  
TESTS:
  [ ] Cobertura >= 70%
  [ ] Integración tests pasan
  [ ] Performance tests OK
  
DOCUMENTACIÓN:
  [ ] .md en /docs actualizado
  [ ] README actualizado
  [ ] Changelog actualizado
  
SEGURIDAD:
  [ ] Security scan completado
  [ ] Secrets scan completado
  [ ] Dependency scan completado
  
RELEASE:
  [ ] Version number bumped
  [ ] Release notes preparadas
  [ ] Backward compatibility OK
  
APROBACIÓN:
  [ ] 2 approvals mínimo
  [ ] Tech lead approval
  [ ] Security review (si aplica)
```

---

## Escala de Complejidad

### Tarea Simple (1 punto)

**Definición**: Cambio en 1 archivo, < 50 líneas, sin dependencias

**Definition of Done reducida**:
- [ ] ✅ Código correcto y compilable
- [ ] ✅ Test unitario básico presente (< 1s)
- [ ] ✅ Documentación mínima (1-2 líneas de comentario)
- [ ] ✅ 1 code review

**Ejemplos**: Typo fix, constante nueva, comment mejora

### Tarea Media (3-5 puntos)

**Definición**: Cambio en 2-3 archivos, 50-200 líneas, con algunas dependencias

**Definition of Done estándar completa**

**Ejemplos**: Nuevo servicio, mejora de validator, nuevo command CLI

### Tarea Compleja (8-13 puntos)

**Definición**: Cambio en 5+ archivos, 200+ líneas, múltiples dependencias, requiere design

**Definition of Done estándar + extra rigour**:
- [ ] ✅ Design document creado
- [ ] ✅ Architecture review completado
- [ ] ✅ Performance benchmarking realizado
- [ ] ✅ 2+ reviews completados
- [ ] ✅ Integration tests incluidos

**Ejemplos**: Nuevo componente mayor, refactoring arquitectónico, integración Azure

---

## Métricas de Calidad

Para que un PR sea mergeable:

| Métrica | Mínimo | Ideal |
|---------|--------|-------|
| Test Coverage | 70% | 85%+ |
| Cyclomatic Complexity | < 10 | < 5 |
| Lines per Method | < 30 | < 20 |
| Lines per Class | < 500 | < 300 |
| Code Duplication | < 5% | < 2% |
| Bug Density | N/A | 0 |

---

## Expedición de Done

No se puede cambiar Definition of Done sin:

1. Consenso del equipo
2. Documentación de cambio
3. Actualización de este documento
4. Notificación a todos los colaboradores

La tendencia debe ser hacer Definition of Done MÁS estricto, no menos.

---

## Auditoría de Cumplimiento

Semanalmente se auditará:

- % de tareas que cumplen Definition of Done
- Variaciones por tipo de tarea (Simple/Media/Compleja)
- Tendencias de calidad
- Áreas de mejora identificadas
- Tiempo promedio para cumplir DoD por tipo de tarea

**Objetivo**: 100% de tareas cumplen Definition of Done

---

**Documento versión**: 1.0  
**Aprobado**: 2026-05-07  
**Estado**: Active - Estándar de calidad
