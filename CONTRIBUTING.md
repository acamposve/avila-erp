# CONTRIBUTING.md — Guía de contribución a ÁVILA ERP

Este documento describe cómo se trabaja en el repositorio. Los agentes de IA **DEBEN** cumplir además [AGENTS.md](AGENTS.md). Esta guía aplica a personas y agentes.

## 1. Principios de colaboración

1. **La documentación precede al código.** Ningún cambio funcional se implementa sin requisito (`RF`), criterio de aceptación (`AC`) y, si aplica, decisión registrada.
2. **Cambios pequeños y revisables.** Un PR debe tener un propósito; los cambios de arquitectura se separan de las funcionalidades.
3. **La revisión es técnica y de producto.** Un PR requiere al menos una aprobación humana; los cambios de arquitectura o fiscales requieren además revisión del rol correspondiente.
4. **Nada se considera terminado sin evidencia.** Ver "Definición de terminado".
5. **El historial es sagrado.** No se reescribe `main` ni se borran documentos o migraciones ya aplicadas en producción.

## 2. Flujo de trabajo

```text
main (protegida, siempre verde)
  └── feature/R2-123-facturacion-desde-pedido
        ├── commits atómicos
        ├── pruebas + documentación
        └── Pull Request → revisión → squash merge
```

1. Toma una historia del [sprint backlog](backlog/sprint-backlog.md) o un issue aprobado.
2. Crea la rama desde `main`: `feature/<id>-<slug>`, `fix/<id>-<slug>`, `docs/<slug>` o `chore/<slug>`.
3. Implementa con pruebas y actualiza la documentación afectada.
4. Abre el PR completando la plantilla.
5. Atiende la revisión; no hagas merge tú mismo si eres autor.
6. El merge a `main` es **squash merge** y requiere CI verde y una aprobación.

**Prohibido:** commit directo a `main`, force push a ramas compartidas, merge sin revisión, deploy desde una rama local.

## 3. Commits

Se usa [Conventional Commits](https://www.conventionalcommits.org/):

```text
feat(sales): emitir factura desde pedido confirmado
fix(inventory): impedir stock negativo en transferencias
docs(tenancy): documentar validación de membresía
test(billing): cubrir numeración concurrente
chore(ci): actualizar cache de pnpm
```

- Tipos permitidos: `feat`, `fix`, `docs`, `test`, `refactor`, `perf`, `build`, `ci`, `chore`, `security`.
- Alcance: código de módulo (`sales`, `inventory`, `billing`, `tenancy`, `web`, `infra`…).
- El cuerpo explica **qué** y **por qué**, no "cómo".
- Referencia el ID de historia/requisito (`Refs: VEN-006`, `Closes: #123`).

## 4. Pull Requests

La plantilla debe incluir:

- **Contexto:** problema y enlace a requisito/historia.
- **Cambio:** resumen técnico y decisiones relevantes.
- **Riesgo:** impacto en tenants, dinero, fiscalidad, rendimiento o migraciones.
- **Pruebas:** qué se ejecutó y evidencia (salida, capturas, resultados).
- **Documentación:** archivos actualizados o justificación de por qué no aplica.
- **Checklist:** el de [AGENTS.md §12](AGENTS.md#12-checklist-antes-de-abrir-un-pr).

Reglas de revisión:

- No se aprueba un PR con pruebas rojas, conflictos sin resolver o secretos detectados.
- Los cambios que tocan tenancy, numeración, dinero, impuestos, nómina o migraciones requieren revisión adicional del owner del módulo y de arquitectura.
- El autor responde cada comentario; los comentarios se resuelven, no se ignoran.
- El merge lo hace una persona distinta del autor cuando sea posible.

## 5. Estándares de código

### Backend (C# / .NET 10)
- `nullable` habilitado, warnings como errores en CI, analyzers de .NET y reglas del repositorio.
- `async`/`await` de extremo a extremo; `CancellationToken` en toda operación de I/O.
- Sin `DateTime.Now`, `Guid.NewGuid()` para claves de negocio, `float`/`double` para dinero ni SQL concatenado.
- Inyección de dependencias por constructor; sin service locator.
- Errores de dominio como resultados explícitos (`Result`/excepciones de dominio acotadas), no códigos mágicos.
- Los controladores/endpoints son delgados: validan entrada, autorizan y delegan en el caso de uso.
- Cada módulo expone solo sus `Contracts`; lo interno es `internal` cuando sea posible.

### Frontend (React / TypeScript)
- TypeScript `strict`; sin `any` implícito ni `@ts-ignore` sin justificación.
- Componentes funcionales, hooks, estado del servidor con TanStack Query; estado local mínimo.
- Validación de formularios con Zod; los mensajes se traducen con i18next.
- Accesibilidad WCAG 2.2 AA: foco, teclado, etiquetas, contraste.
- Sin lógica de negocio duplicada del backend; solo validaciones de experiencia.
- Pruebas con Vitest + Testing Library; E2E con Playwright en flujos críticos.

### General
- Nombres en inglés en código; textos de interfaz en español (i18n).
- Sin números mágicos: constantes nombradas o configuración versionada.
- Sin comentarios que repitan el código; documentar decisiones no obvias.
- Formateo automático (Prettier/`dotnet format`) y lint en CI.

## 6. Base de datos y migraciones

- Motor: **PostgreSQL 18**; un esquema por módulo; migraciones EF Core por módulo con historial propio. Ver [database-design.md](docs/architecture/database-design.md).
- Migraciones **forward-only** y compatibles hacia atrás durante el despliegue (expand/contract).
- Prohibido borrar columnas/tablas con datos en el mismo release que deja de usarlas; primero se deja de escribir, luego se elimina en un release posterior.
- Toda migración se prueba: base vacía + base con datos representativos + rollback lógico documentado.
- Los índices y constraints de tenancy son obligatorios; el revisor debe verificarlos.
- Datos semilla: solo catálogos técnicos, nunca datos de clientes ni valores fiscales/laborales sin aprobación.

## 7. Pruebas

- Pirámide: unitarias rápidas, integración con PostgreSQL real (Testcontainers), contratos, E2E de flujos críticos.
- Cobertura no es un objetivo en sí; sí lo es la cobertura de **reglas e invariantes**.
- Obligatorias: aislamiento multi-tenant, concurrencia de numeración, concurrencia de stock, idempotencia de pagos, reversión de documentos, redondeo de dinero.
- Prohibido tests dependientes de hora real, red o orden de ejecución.
- Los datos de prueba son sintéticos y anonimizados.
- Ver [testing-strategy.md](docs/architecture/testing-strategy.md).

## 8. Documentación

- Todo cambio de alcance actualiza [functional-scope.md](docs/product/functional-scope.md), [functional-requirements.md](docs/requirements/functional-requirements.md) y [releases.md](backlog/releases.md).
- Toda decisión arquitectónica nueva se registra como ADR en [docs/decisions/](docs/decisions/) antes de implementar.
- Toda decisión no tomable se registra como `DEC-NNN` en [docs/decisions/README.md](docs/decisions/README.md).
- Los documentos se escriben en español, con la nomenclatura de [GLOSSARY.md](docs/GLOSSARY.md).
- Los enlaces internos deben ser relativos y funcionar.

## 9. Seguridad

- No subir secretos, claves, tokens, certificados ni cadenas de conexión.
- No usar datos reales en pruebas, ejemplos o capturas.
- Reportar vulnerabilidades de forma privada al responsable de seguridad; **no** abrir un issue público con detalles explotables.
- Revisar dependencias nuevas (licencia, mantenimiento, CVEs) antes de incorporarlas.
- Cambios de autenticación, autorización, cifrado o tenancy requieren revisión de seguridad.

## 10. ADR y decisiones

Proceso:

1. Copia la plantilla de un ADR existente en [docs/decisions/](docs/decisions/).
2. Estado inicial `Propuesto`.
3. Discusión en el PR; aprobación del responsable de arquitectura.
4. Estado `Aceptado` y enlace desde los documentos afectados.
5. Para reemplazar un ADR: nuevo ADR que lo supersede; el anterior pasa a `Reemplazado por ADR-NNN`.

Los agentes de IA pueden **proponer** ADR, nunca aprobarlos.

## 11. Releases y despliegue

- Un release se cierra cuando se cumplen sus criterios de salida en [releases.md](backlog/releases.md).
- El despliegue lo ejecuta una persona autorizada con ventana y plan de reversión.
- Prohibido desplegar, migrar producción o ejecutar operaciones destructivas sin autorización explícita.
- Después de cada despliegue: verificación de salud, métricas y smoke tests.

## 12. Definición de terminado (Definition of Done)

Una historia está terminada cuando:

- [ ] Cumple sus criterios de aceptación (`AC`) y las pruebas lo demuestran.
- [ ] Tiene pruebas automatizadas en el nivel adecuado, incluidas aislamiento/concurrencia cuando aplica.
- [ ] Pasa CI (build, lint, tests, análisis de seguridad, migraciones en limpio).
- [ ] No introduce secretos, PII ni datos reales.
- [ ] Actualiza documentación, requisitos y trazabilidad.
- [ ] Fue revisada y aprobada por una persona.
- [ ] No requiere pasos manuales no documentados para desplegar.
- [ ] Deja evidencia en el PR (salidas, resultados, capturas).

## 13. Contactos y roles (a completar)

| Rol | Responsabilidad | Persona |
|---|---|---|
| Arquitectura | ADR, límites de módulos, tenancy | `PENDIENTE` |
| Producto | Alcance, priorización, criterios de aceptación | `PENDIENTE` |
| Fiscal/Contable | Validación de reglas tributarias y contables | `PENDIENTE` (especialista externo) |
| Laboral | Validación de nómina y prestaciones | `PENDIENTE` (especialista externo) |
| Seguridad | Threat model, revisión de cambios sensibles | `PENDIENTE` |
| Operaciones | Infraestructura, despliegue, respaldo | `PENDIENTE` |
