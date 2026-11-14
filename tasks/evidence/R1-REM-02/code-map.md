# R1-REM-02 — Code map

## 1. Bootstrap

| Artifact | Path | Purpose |
|---|---|---|
| Roles SQL | `ops/postgres/roles.sql` | Idempotent, password-free roles/grants |
| Role doc | `docs/operations/roles-and-grants.md` | Matrix, order, operational rules |

## 2. Test integration

| Artifact | Path | Purpose |
|---|---|---|
| Fixture | `tests/Avila.ProductionTests/ProductionFixture.cs` | Executes exact copied `roles.sql` |
| Test project | `tests/Avila.ProductionTests/Avila.ProductionTests.csproj` | Copies `roles.sql` to output |
| Role test | `tests/Avila.ProductionTests/RoleBootstrapTests.cs` | Idempotence, attributes, grants |

## 3. Evidence

| Artifact | Path |
|---|---|
| Execution log | `tasks/evidence/R1-REM-02/execution-log.md` |
| Verification | `tasks/evidence/R1-REM-02/verification.md` |
| Full test output | `tasks/evidence/R1-REM-02/raw/test-output.txt` |
| Environment | `tasks/evidence/R1-REM-02/raw/environment.txt` |

## 4. Boundaries

- No production role/credential application.
- No secrets in `roles.sql`.
- No RLS disablement.
- No G1 opening.
