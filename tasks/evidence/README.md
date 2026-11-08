# Evidencia obligatoria por tarea

Este directorio define cómo una tarea de [`tasks/`](../README.md) demuestra que fue ejecutada y verificada. **Una tarea no está terminada porque exista código, un PR o una prueba aislada.** Debe dejar un paquete de evidencia reproducible.

## 1. Paquete mínimo

Cada tarea debe crear:

```text
tasks/evidence/T-XXX/
├── execution-log.md   # qué se ejecutó, cuándo, dónde y con qué resultado
├── verification.md    # criterios de aceptación → evidencia concreta
└── code-map.md        # archivos, migraciones, pruebas y PR que implementan la tarea
```

Para tareas de decisión, revisión o compuerta, el paquete puede sustituir `code-map.md` por `decision-record.md` o `gate-matrix.md`, pero **no** puede omitir `execution-log.md` ni `verification.md`.

## 2. Reglas

1. La evidencia se escribe en el mismo PR que la implementación o la revisión.
2. No se declara una tarea `verified` sin los tres artefactos y sin aprobación humana.
3. El log debe ser reproducible: comando, versión, entorno, resultado y código de salida.
4. No se incluyen secretos, tokens, cadenas de conexión, PII ni datos reales de clientes.
5. Los logs largos se guardan como texto saneado en `raw/`; el resumen va en `execution-log.md`.
6. Un fallo no se oculta: se registra el resultado y el bloqueo. “No ejecutado” es un estado válido y explícito.
7. Los IDs de requisitos, AC, ADR, DEC y riesgos se enlazan; no se inventan.
8. La ausencia de evidencia significa **no validado**, aunque el código exista.

## 3. Estados de tarea

| Estado | Significado |
|---|---|
| `not-started` | Sin trabajo iniciado |
| `in-progress` | Rama/PR abiertos, sin aceptación |
| `implemented` | Código y pruebas existen, falta verificación formal |
| `verified` | Evidencia completa, pruebas verdes y revisión humana |
| `blocked` | Dependencia, decisión o validación externa pendiente |

Solo `verified` cuenta para una compuerta de release. Un PR aprobado no cambia el estado por sí solo.

## 4. `execution-log.md`

Plantilla:

```markdown
# T-XXX — Execution log

- Fecha:
- Responsable:
- Rama:
- Commit:
- PR:
- Entorno: (por ejemplo, PostgreSQL 18 Testcontainer / .NET 10.0.403)
- Estado: implemented | verified | blocked

## Comandos ejecutados

| # | Comando | Resultado | Evidencia |
|---|---|---|---|
| 1 | `dotnet test ...` | 21/21 passed | `raw/test-output.txt` |

## Resultado

Resumen verificable, no una opinión.

## Bloqueos y pendientes

- ...
```

## 5. `verification.md`

Plantilla:

```markdown
# T-XXX — Verification

| Criterio de aceptación | Evidencia | Resultado |
|---|---|---|
| `AC-...` | test, consulta, captura o documento | pass/fail/blocked |

## Qué se probó

## Qué no se probó

## Riesgos residuales
```

Debe cubrir **todos** los criterios de la sección “Aceptación y evidencia” de la tarea.

## 6. `code-map.md`

Plantilla:

```markdown
# T-XXX — Code map

| Artefacto | Ruta | Propósito |
|---|---|---|
| Módulo | `src/...` | ... |
| Migración | `.../Migrations/...` | ... |
| Prueba | `tests/...` | ... |
| Documento | `docs/...` | ... |

## Contratos y límites

## Fuera de alcance
```

## 7. Compuertas

Las tareas de compuerta (`T-013`, `T-022`, `T-026`, `T-029`, `T-032`, `T-037`) deben incluir además:

```text
tasks/evidence/T-XXX/gate-matrix.md
```

con todos los criterios del release, su evidencia, responsable y estado. Cualquier pendiente mantiene la compuerta cerrada.

## 8. Validación

La validación de una tarea comprueba:

- [ ] Existe `execution-log.md` con fecha, commit y comandos.
- [ ] Existe `verification.md` con todos los criterios.
- [ ] Existe `code-map.md` o el sustituto aprobado.
- [ ] Las pruebas referenciadas pasan y el resultado es reproducible.
- [ ] No hay secretos, PII ni datos reales.
- [ ] Los enlaces a RF/AC/ADR/DEC son válidos.
- [ ] El PR fue revisado por una persona distinta del autor.
- [ ] El estado declarado coincide con la evidencia.

## 9. Relación con los informes existentes

- [Validación actual de tareas](../../docs/tasks-validation.md): 0 de 37 completamente validadas.
- [Plan de ejecución](../../docs/project-execution-plan.md): fases, dependencias y compuertas.
- [Plan de hardening](../../docs/architecture/architecture-hardening-plan.md): pruebas de aislamiento, concurrencia y migraciones.
- [Estrategia de pruebas](../../docs/architecture/testing-strategy.md): niveles y CI.

## 10. Primer caso de uso

El primer paquete debe ser **T-005** porque ya existen UoW, RLS y pruebas en el scaffold y en la implementación de producción. Debe enlazar, como mínimo:

- `T-04`, `T-07`, `T-07b`, `T-08`, `T-09`, `T-09b`, `T-16` del plan de hardening;
- `US-PLT-002`, `AC-SEC-01`, ADR-007;
- `src/Avila.Platform/Tenancy/*`;
- las migraciones con RLS y las pruebas de aislamiento.

No crear este paquete hasta que el responsable autorice el inicio de la implementación de tareas.
