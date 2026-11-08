# Estrategia de pruebas

**Versión:** 1.0 · **Fecha:** 2026-11-08 · **Estado:** línea base

## 1. Objetivo

Demostrar que ÁVILA ERP cumple sus requisitos, especialmente los que protegen dinero, aislamiento entre tenants, numeración fiscal, inventario y contabilidad. La estrategia prioriza **riesgo**, no porcentaje de cobertura.

## 2. Pirámide y tipos

```text
        E2E (Playwright)          pocos, flujos críticos
      Contrato / API             consumidores y partners
    Integración (PostgreSQL)     módulos, RLS, concurrencia
  Unitarias (dominio)            reglas, cálculos, invariantes
```

| Nivel | Herramienta | Qué cubre | Velocidad |
|---|---|---|---|
| Unitarias | xUnit v3 | Agregados, value objects, reglas, redondeo, estados | Muy rápida |
| Integración | xUnit + Testcontainers (PostgreSQL real) | Repositorios, migraciones, RLS, transacciones, concurrencia | Media |
| Contrato | Pruebas de esquema/OpenAPI + eventos | Compatibilidad de API y eventos | Rápida |
| E2E | Playwright | Flujos de usuario críticos | Lenta |
| Carga | k6 | NFR de rendimiento y concurrencia | Lenta |
| Seguridad | SAST/DAST + pruebas de autorización | Vulnerabilidades y accesos indebidos | Media |
| Recuperación | Simulacros | Restauración y continuidad | Periódica |

## 3. Pruebas obligatorias por área

### 3.1 Aislamiento multi-tenant
- Acceso por ID a recurso de otro tenant → 404/403.
- Filtro manipulado en listados y reportes → sin datos ajenos.
- Exportación con tenant cambiado → rechazo.
- URL firmada de archivo ajeno → rechazo.
- Job con tenant manipulado → rechazo.
- Token con `tid` alterado → rechazo.
- Introspección de esquema: RLS y `tenant_id` en todas las tablas.

### 3.2 Concurrencia
- Numeración: 10.000 emisiones simultáneas sin duplicados ni huecos.
- Stock: ventas concurrentes del mismo artículo sin sobreventa.
- Pagos: aplicación concurrente sin exceder el saldo.
- Cierres: dos cierres simultáneos del mismo período → uno falla.
- Idempotencia: misma clave repetida en paralelo → un solo efecto.

### 3.3 Dinero
- Casos dorados de redondeo (mitad, negativos, descuentos, impuestos).
- Suma de líneas = total.
- Conversión con tasa snapshot; histórico inmutable.
- Diferencias cambiarias y aplicación de pagos.
- Pruebas de propiedad sobre aritmética decimal.

### 3.4 Fiscal
- Factura con líneas gravadas, exentas y descuentos.
- Nota de crédito parcial y total.
- Snapshot de datos fiscales.
- Regla por vigencia: documento antiguo no cambia.
- Numeración por empresa, sucursal y serie.
- Casos dorados validados por especialista (`PENDIENTE`).

### 3.5 Inventario
- Entrada, salida, transferencia, ajuste y devolución.
- Costo promedio ponderado con casos calculados manualmente.
- Lotes y vencimientos.
- Kardex cuadra con saldo.
- Reservas y liberación.

### 3.6 Contabilidad
- Partida doble obligatoria.
- Período cerrado rechaza asientos.
- Asiento automático por documento.
- Reversión por contraasiento.
- Balanza y estados cuadran.
- Conciliación subledger-GL.

### 3.7 Nómina
- Cálculo determinista y recálculo idéntico.
- Parámetros por vigencia.
- Casos dorados por concepto (validados externamente).
- Liquidación y prestaciones.
- Provisión y asiento.

### 3.8 POS / offline
- Venta sin conexión y sincronización.
- Duplicados y conflictos.
- Cierre de caja y arqueo.
- Impresión y gaveta (prueba de hardware).

## 4. Datos de prueba

- Sintéticos, reproducibles y versionados.
- Sin PII real ni datos de clientes.
- Generador de tenants con volúmenes configurables.
- Fixtures de casos fiscales y laborales con fuente y estado (`PENDIENTE` si aplica).
- Restauración de base por prueba (Respawn o recreación de contenedor).

## 5. Pruebas de contrato y API

- OpenAPI validado contra implementación en CI.
- Esquemas de eventos versionados; compatibilidad hacia atrás.
- Webhooks con firma y reintentos simulados.
- Pruebas de partner en sandbox antes de certificar.

## 6. E2E críticos (mínimo)

1. Aprovisionar organización → invitar usuario → iniciar sesión → cambiar de organización.
2. Crear artículo → cotizar → pedido → factura → cobro → consulta de cartera.
3. Compra → recepción → factura → pago.
4. Nota de crédito y reversión.
5. Ajuste de inventario y kardex.
6. Cierre de caja.
7. Cierre contable (R3).
8. Venta POS offline y sincronización (R4).
9. Nómina y liquidación (R5).
10. Exportación completa de datos.

## 7. Rendimiento y capacidad

- Escenarios k6 por flujo (lectura, emisión, cobro, kardex).
- Perfil CO-1 y 3× CO-1.
- Medición de p95/p99, errores y saturación.
- Prueba de contención por tenant ruidoso.
- Prueba de outbox bajo carga.
- Resultados archivados y comparados entre releases.

## 8. Seguridad

- Pruebas de autorización por rol y alcance.
- Pruebas de inyección, XSS, CSRF, IDOR y rate limiting.
- SAST en cada PR; DAST en staging.
- Escaneo de dependencias e imágenes.
- Prueba de penetración externa antes de R2.
- Revisión de configuración cloud y secretos.

## 9. Recuperación y continuidad

- Restauración PITR trimestral.
- Simulacro de caída de base de datos y de worker.
- Verificación de RPO/RTO.
- Prueba de despliegue y reversión.
- Informe con hallazgos y acciones.

## 10. CI/CD — compuertas

> **Estado 2026-11-08:** el workflow base [`.github/workflows/ci.yml`](../../.github/workflows/ci.yml) cubre build, pruebas y migraciones limpias mediante Testcontainers. SAST, contrato, E2E, rendimiento y seguridad siguen pendientes y no se declaran cubiertos por este workflow.

| Etapa | Debe pasar |
|---|---|
| Build | Compilación sin warnings críticos |
| Unitarias | 100 % verdes |
| Integración | 100 % verdes con PostgreSQL real |
| Arquitectura | Sin dependencias prohibidas |
| Migraciones | Base vacía + base con datos |
| SAST/dependencias | Sin críticos/altos sin excepción aprobada |
| Contrato | OpenAPI y eventos compatibles |
| E2E | Flujos críticos verdes en staging |
| Rendimiento | Dentro de umbral (release) |
| Seguridad | Checklist y pen test (R2) |

## 11. Política de pruebas inestables

- Una prueba inestable se corrige o se excluye con justificación y ticket.
- Prohibido deshabilitar pruebas para "pasar" el PR.
- Toda exclusión se revisa semanalmente y se elimina al corregir.
- Los fallos intermitentes se investigan; no se reintentan silenciosamente en producción.

## 12. Cobertura y trazabilidad

- Matriz requisito → prueba → evidencia.
- Cobertura de reglas e invariantes ≥ 90 % (NFR-MAI-002).
- Cobertura de líneas es informativa, no un objetivo.
- Los criterios de aceptación [AC](../requirements/acceptance-criteria.md) enlazan sus pruebas.

## 13. Documentos relacionados

- [Requisitos no funcionales](../requirements/non-functional-requirements.md)
- [Criterios de aceptación](../requirements/acceptance-criteria.md)
- [Arquitectura](architecture-overview.md)
- [Base de datos](database-design.md)
