# peak-service

Catálogo mundial de montañas y consultas geoespaciales. PostgreSQL 16 + PostGIS
(`peaker_peaks`). Ver `.claude/docs/DESIGN.md` §6.

## Puesta en marcha local

La cadena de conexión **no se versiona** (`ARCHITECTURE.md` §11, regla innegociable 8):
`appsettings.json` y `appsettings.Development.json` la dejan vacía. Fuera de Docker se
configura con `dotnet user-secrets`:

```bash
cd PeakService/PeakService.API
dotnet user-secrets set "ConnectionStrings:PeakDatabase" \
  "Server=localhost;Port=5433;Database=peaker_peaks;Username=<usuario>;Password=<contraseña>"
```

Dentro de Docker la aporta `services-deployment/config/peak-service.env` mediante
`ConnectionStrings__PeakDatabase`.

El esquema no se aplica en el arranque (`ARCHITECTURE.md` §12). Fuera de Docker:

```bash
dotnet ef database update \
  --project PeakService/PeakService.Infrastructure \
  --startup-project PeakService/PeakService.API
```
