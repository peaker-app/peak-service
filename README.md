# peak-service

Catálogo mundial de montañas y consultas geoespaciales. PostgreSQL 16 + PostGIS
(`peaker_peaks`). Ver `.claude/docs/DESIGN.md` §6.

## Puesta en marcha local

La cadena de conexión **no se versiona** (`ARCHITECTURE.md` §11, regla innegociable 8):
`appsettings.json` y `appsettings.Development.json` la dejan vacía. Dentro de Docker la aporta
`services-deployment/config/peak-service.env` mediante `ConnectionStrings__PeakDatabase`.

Fuera de Docker se carga esa misma variable desde el `.env`, sin copiar la contraseña a ningún
sitio. Sirve tanto para `dotnet run` como para `dotnet ef`:

```powershell
cd ..\services-deployment
. .\scripts\Use-DevDatabase.ps1 peak        # bash: source ./scripts/use-dev-database.sh peak
```

`dotnet user-secrets` es una alternativa válida para `dotnet run`, pero **`dotnet ef` la ignora**:
`PeakDbContextFactory` tiene prioridad sobre el host de la API y solo lee la variable de entorno.

El esquema no se aplica en el arranque (`ARCHITECTURE.md` §12). Fuera de Docker:

```bash
dotnet ef database update \
  --project PeakService/PeakService.Infrastructure \
  --startup-project PeakService/PeakService.API
```
