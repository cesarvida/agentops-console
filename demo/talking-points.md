# Puntos Clave para la Presentación

## Por qué keywords solas no bastan

El sistema no busca solo "ignore previous instructions".
Antes de aplicar reglas, normaliza el contenido:
- Decodifica base64
- Resuelve concatenaciones de strings (`"os.re" + "move"` → `"os.remove"`)
- Normaliza unicode escapes (`\u006f\u0073` → `os`)
- Detecta ROT13

Esto hace casi imposible evadir la detección con ofuscación simple.

---

## El clasificador de contexto reduce falsos positivos

Un prompt de documentación que menciona "ignore" en un ejemplo
no debería bloquearse. El `ContextClassifier` detecta si el archivo
es documentación o un prompt activo y ajusta el `ConfidenceScore`:

- Archivo de documentación (`# Example`, `## Usage`) → confianza -0.3
- Archivo de tests (`def test_`, `unittest`) → confianza -0.2
- Prompt activo (`You are a`, `Act as`) → sin reducción

---

## Por qué es determinístico y auditable

No usa LLMs para decidir. Cada decisión tiene:
- `RuleId` exacto que la disparó (ej: `PI-001`, `DE-001`)
- Línea exacta del archivo donde se encontró la amenaza
- `ConfidenceScore` explícito (0.0 – 1.0)
- Razón del bloqueo en texto claro
- JSON de auditoría guardado en disco

Esto es crítico para auditoría y compliance regulatorio.

---

## Integración con GitHub Actions

Cuando alguien abre un PR con un `.md` o `.py` malicioso:
1. El workflow `.github/workflows/prompt-analysis.yml` se activa automáticamente
2. El analizador corre en CI sobre cada archivo cambiado
3. Si el veredicto es `BLOCK` → exit code 1 → **merge bloqueado**
4. Comentario automático en el PR con el reporte detallado (Markdown)

El equipo de seguridad ve exactamente qué detectó y por qué.

---

## Extensibilidad — nuevas reglas en minutos

Añadir una nueva regla = crear una clase que implemente `IPromptDetector`:

```csharp
public class MyNewRule : IPromptDetector
{
    public string DetectorName => "My Rule";
    public string[] SupportedTypes => ["markdown", "python"];
    public List<Finding> Analyze(ExtractedContent content, ContentContext ctx)
    {
        // Tu lógica aquí
    }
}
```

El contenedor DI la recoge automáticamente. No hay que tocar el pipeline.

---

## Preguntas frecuentes de audiencias técnicas

**¿Por qué no usar un LLM para detectar prompts maliciosos?**
Los LLMs son no-determinísticos, lentos y costosos. Para CI/CD necesitas
una decisión en < 1 segundo con 0 llamadas a APIs externas.

**¿Puede evadir el sistema un atacante sofisticado?**
El sistema es una primera línea de defensa en el repositorio, no la única.
Detecta los vectores más comunes con alta precisión. Para evasiones avanzadas
(ROT13 anidado, esteganografía) se pueden añadir capas adicionales.

**¿Genera falsos positivos en prompts legítimos con lenguaje técnico?**
El ContextClassifier reduce la confianza para archivos de documentación y tests.
En las 9 pruebas QA con prompts reales de GitHub: 0 falsos positivos.
