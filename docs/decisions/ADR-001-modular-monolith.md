# ADR-001 — Monolito modular como estilo arquitectónico

- **Estado:** Aceptado
- **Fecha:** 2026-11-08
- **Decisores:** Arquitectura, Producto
- **Reemplaza a:** —

## 1. Contexto

ÁVILA ERP debe cubrir una suite amplia (ventas, compras, inventario, facturación fiscal, tesorería, contabilidad, POS, nómina, producción, CRM) para PYMEs, con un equipo pequeño y un mercado que exige tiempo de comercialización corto. Los dominios están fuertemente relacionados: una factura afecta inventario, cartera y contabilidad; un pago afecta caja, cartera y contabilidad. La consistencia transaccional es un requisito de negocio, no un detalle técnico.

Al mismo tiempo, el producto debe evolucionar por releases y permitir que equipos distintos trabajen en módulos distintos sin degradar la arquitectura. La tentación de microservicios introduciría costos operativos (red, consistencia distribuida, observabilidad, despliegues) que no se justifican en esta etapa y que retrasarían el primer producto vendible.

## 2. Decisión

Se adopta un **monolito modular**:

1. **Un solo despliegue lógico de backend** compuesto por dos hosts: `Avila.Api` (HTTP) y `Avila.Worker` (procesos en segundo plano).
2. **Módulos como unidades de negocio** con ensamblados propios:
   - `Avila.Modules.<Módulo>` (dominio, aplicación e infraestructura internos).
   - `Avila.Modules.<Módulo>.Contracts` (DTOs, interfaces y eventos públicos).
3. **Un esquema PostgreSQL por módulo**, con historial de migraciones propio. Ningún módulo accede a tablas de otro.
4. **Comunicación en proceso** mediante contratos y eventos; no hay llamadas HTTP internas ni colas externas en la etapa inicial.
5. **Transacciones locales** dentro de un caso de uso; los efectos secundarios y las integraciones usan [transactional outbox](ADR-005-transactional-outbox.md).
6. **Sin microservicios, sin service mesh y sin bases de datos por módulo** hasta que se cumplan los criterios de extracción de §6.

## 3. Consecuencias

### Positivas
- Consistencia fuerte donde el negocio la exige (factura + stock + cartera + asiento).
- Un solo pipeline, un solo despliegue, una sola base de datos que operar.
- Depuración y trazas end-to-end simples.
- Menor costo de infraestructura y de equipo.
- Los límites de módulo preparan una eventual extracción sin reescribir el dominio.

### Negativas
- Un despliegue afecta a todos los módulos; se mitiga con pruebas, despliegue gradual y compatibilidad hacia atrás.
- Riesgo de "monolito enredado" si no se aplican los límites; se mitiga con pruebas de arquitectura y revisión.
- Escalamiento por módulo imposible; se escala el conjunto.
- Un módulo ruidoso puede afectar a otros; se mitiga con cuotas, aislamiento de jobs y límites de recursos.

### Neutras
- La base de datos compartida requiere disciplina de esquemas y migraciones coordinadas.

## 4. Alternativas consideradas

| Alternativa | Por qué se descarta |
|---|---|
| Microservicios desde el inicio | Complejidad operativa y consistencia distribuida sin necesidad demostrada; retrasa R2 |
| Monolito en capas clásico | No protege límites de dominio; termina en acoplamiento global |
| Monolito modular con una base por módulo | Multiplica operación, respaldos y transacciones; innecesario para el tamaño inicial |
| Serverless por función | Difícil de razonar para transacciones y jobs; costo variable impredecible |

## 5. Reglas de dependencia

Los módulos se organizan por niveles; un módulo solo puede depender de módulos de nivel inferior (o del shared kernel):

| Nivel | Módulos |
|---|---|
| 0 | `SharedKernel`, `Platform` (tenancy, identidad, autorización, auditoría, numeración, outbox) |
| 1 | `Administration` |
| 2 | `Inventory` |
| 3 | `Treasury` |
| 4 | `Billing` |
| 5 | `Sales`, `Purchasing` |
| 6 | `Accounting` (consume eventos de facturación, tesorería y compras; ningún módulo depende de él) |
| 7 | `POS`, `Payroll`, `Production`, `CRM`, `Integration` |

Excepciones permitidas y documentadas: `Billing` → `Treasury` (crear partida por cobrar) y `Sales`/`Purchasing` → `Billing` (solicitar emisión) se ejecutan dentro de la misma transacción local mediante contratos.

**Prohibido:** ciclos entre módulos, acceso a `DbContext` ajeno, referencias a entidades internas, SQL contra esquemas de otro módulo.

## 6. Criterios para extraer un módulo a servicio

Solo se considerará la extracción si se cumple **al menos dos** de estos criterios, con ADR aprobado:

1. El módulo tiene un perfil de carga radicalmente distinto y consume recursos de forma desproporcionada.
2. Requiere un ciclo de despliegue independiente por razones regulatorias o de negocio.
3. Su equipo es autónomo y el costo de coordinación supera el de operar un servicio.
4. La consistencia fuerte con el resto deja de ser necesaria.
5. Existe una medición que demuestra que el monolito no puede cumplir los NFR con escalamiento vertical/horizontal.

## 7. Verificación

- Prueba de arquitectura en CI que falla si un módulo referencia otro fuera de sus contratos.
- Revisión de PR con checklist de límites.
- Mapa de dependencias actualizado en [modular-monolith.md](../architecture/modular-monolith.md).
- Pruebas de integración que demuestran transacciones locales y outbox.

## 8. Referencias

- [modular-monolith.md](../architecture/modular-monolith.md)
- [architecture-overview.md](../architecture/architecture-overview.md)
- [ADR-005](ADR-005-transactional-outbox.md)
