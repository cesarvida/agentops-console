# AgentOps Console — Demo Script
## Prompt Security Analyzer

### El problema que resuelve

La gente sube prompts a GitHub para automatizar tareas con LLMs.
Esos prompts pueden contener:
- Instrucciones ocultas que manipulan el LLM
- Código que exfiltra archivos del usuario
- Payloads ofuscados en base64
- Instrucciones para borrar archivos del sistema

Este sistema los detecta **ANTES** de que lleguen al LLM.

---

### Arquitectura en 6 capas

| Capa | Nombre | Qué hace |
|------|--------|----------|
| 1 | SafeContentExtractor | Extrae contenido sin ejecutar nada |
| 2 | ContentNormalizer | Decodifica base64, unicode escapes, string concat |
| 3 | ContextClassifier | Reduce falsos positivos (docs vs prompt activo) |
| 4 | Detectors (×6) | Aplica reglas PI-001, TA-001, DE-001, HI-001, OB-001, PS-001 |
| 5 | Risk Scorer | CRITICAL+30, HIGH+20, MEDIUM+10, LOW+5, obfuscation+20 |
| 6 | Decision Engine | PASS / REVIEW / BLOCK con razón explícita |

---

### Comandos de la demo (en orden)

**Paso 1 — Prompt limpio (baseline)**
```
dotnet run --project src/AgentOps.CLI -- analyze tests/prompt-samples/clean-assistant.md
```
Esperado: PASS, Score 0/100

**Paso 2 — Prompt injection con instrucción oculta en HTML comment**
```
dotnet run --project src/AgentOps.CLI -- analyze tests/prompt-samples/injection-hidden.md
```
Esperado: BLOCK, Score 80/100 — HI-001 + PI-001

**Paso 3 — Exfiltración de datos (webhook.site)**
```
dotnet run --project src/AgentOps.CLI -- analyze tests/prompt-samples/data-collector.md
```
Esperado: BLOCK, Score 40/100 — DE-001 CRITICAL

**Paso 4 — Payload ofuscado en base64**
```
dotnet run --project src/AgentOps.CLI -- analyze tests/prompt-samples/obfuscated-payload.md
```
Esperado: BLOCK, Score 50/100 — OB-001 decodifica + PI-001 detecta

**Paso 5 — Script Python malicioso (exfiltración + env dump)**
```
dotnet run --project src/AgentOps.CLI -- analyze tests/prompt-samples/malicious-python.py
```
Esperado: BLOCK, Score 100/100 — DE-001 ×3 CRITICAL + PS-001 HIGH

---

### Para ejecutar la demo interactiva completa

```powershell
cd demo
./run-demo.ps1
```

---

### Resultados de las pruebas QA (9/9)

| Archivo | Esperado | Obtenido | Score | Correcto |
|---------|----------|----------|-------|----------|
| clean-assistant.md | PASS | PASS | 0/100 | ✅ |
| injection-hidden.md | BLOCK | BLOCK | 80/100 | ✅ |
| data-collector.md | BLOCK | BLOCK | 40/100 | ✅ |
| tool-abuse.md | BLOCK | BLOCK | 30/100 | ✅ |
| obfuscated-payload.md | BLOCK | BLOCK | 50/100 | ✅ |
| policy-bypass.md | BLOCK | BLOCK | 70/100 | ✅ |
| clean-python.py | PASS | PASS | 0/100 | ✅ |
| malicious-python.py | BLOCK | BLOCK | 100/100 | ✅ |
| obfuscated-python.py | BLOCK | BLOCK | 90/100 | ✅ |

**Precisión: 9/9 | Falsos positivos: 0 | Falsos negativos: 0**
