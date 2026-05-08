# 🚀 VALIDACIÓN: Code Reviewer Agent Demo

**Cómo probar el agente Code Reviewer implementado en esta versión**

---

## Verificación Rápida

```bash
# 1. Build
dotnet build AgentOps.Console.sln

# 2. Tests
dotnet test AgentOps.Console.sln --no-build

# 3. Ver commit
git log --oneline -1
# 315ddaf (HEAD -> master) feat(code-reviewer): add governed Code Reviewer agent...
```

**Resultado esperado:** Todos los tests pasan (36/36)

---

## Ejecutar CLI Demo

```bash
dotnet run --project src/AgentOps.CLI
```

**Menú esperado:**
```
AgentOps Governance Console
===============================
1) List Agents
2) Create Agent Definition
3) Exit
4) Evaluate Agent Behavior
5) View Audit Trail
6) Run Code Review (simulated)     ← NUEVO

Select option: 6
```

### Opción 6: Run Code Review (Simulated)

El comando simula un PR diff con patrones de seguridad y evalúa el agente `Code Reviewer`:

**Input (simulado):**
```
+ API_KEY="AKIA1234567890EXAMPLE"
+ eval(user_input)
+ vulnerable-lib==1.0.0
```

**Output esperado:**
```
Evaluating agent 'Code Reviewer' with scenario 'code-review-security-suite-v1'...
Found 3 security findings:
  1. [Critical] Hardcoded API key detected (SecretPatternAnalyzer)
  2. [Medium] eval() usage detected (DangerousFunctionAnalyzer)
  3. [High] Known vulnerable dependency (DependencyRiskAnalyzer)

Evaluation result: FAIL (due to critical findings)
Audit trail updated.
```

---

## Inspeccionar Artefactos Persistidos

```bash
# Ver agentes guardados
ls -la ~/.agentops/agents/

# Ver evaluaciones guardadas
ls -la ~/.agentops/evaluations/

# Ver audit log
cat ~/.agentops/audit.log | tail -5
```

**Archivo de auditoría (truncado):**
```json
{"timestamp":"2026-05-08T12:22:49Z","action":"EvaluateAgent","entity":"Code Reviewer","status":"FAIL","digest":"sha256:abc123..."}
```

---

## Verificar Integridad (Artifacts)

```bash
# El artifact incluye:
# - evaluation_report.json (redactado, sin PII)
# - digest: SHA-256 (inmutable)
# - metrics: score, findings count
# - veto_applied: critical findings caused FAIL
```

---

## Test Vectors Validados

### SecretPatternAnalyzer
- ✅ Detecta: `API_KEY="token123456789"`
- ✅ Detecta: `-----BEGIN PRIVATE KEY-----`
- ✅ Detecta: `1234567890abcdef` (32+ hex chars)

### DangerousFunctionAnalyzer
- ✅ Detecta: `eval()`
- ✅ Detecta: `exec()`
- ✅ Detecta: `popen()`
- ✅ Detecta: `os.system()`
- ✅ Detecta: `shell=True`

### DependencyRiskAnalyzer
- ✅ Detecta: `vulnerable-lib==1.0.0`

---

## Logs Relevantes

```bash
# Build log (si hay problemas)
dotnet build AgentOps.Console.sln 2>&1 | grep -i error

# Test log detallado
dotnet test AgentOps.Console.sln --logger "console;verbosity=detailed"

# Audit trail (último 10 eventos)
tail -10 ~/.agentops/audit.log
```

---

## Estado esperado post-cierre

```
✅ Commit: 315ddaf (Code Reviewer Agent implementation)
✅ Build: Exitoso
✅ Tests: 36/36 pasan
✅ CLI: Ejecutable, opción 6 funcional
✅ Auditoría: Append-only, hashes SHA-256
✅ Código: Limpio, sin cambios innecesarios
✅ Listo para: Review técnica + PR
```

---

## Próximos Pasos (Referencia)

1. **Push a remote**
   ```bash
   git remote add origin <URL>
   git push -u origin feature/code-reviewer
   ```

2. **Crear PR Draft** (desde UI remota)
   - Ver CLOSURE_SUMMARY.md para template

3. **Code Review**
   - Feedback en arquitectura
   - Mejoras de reglas
   - Cobertura de casos

4. **Merge a main**
   - Cuando esté aprobado

---

**Validación de cierre completada:** 2026-05-08

