# CLAUDE.md - Instrucciones para Claude Code

Este archivo proporciona contexto a Claude cuando trabaja en el repositorio AyudaRemota.

## Proyecto: AyudaRemota

**Descripción**: Aplicación de soporte remoto para ayudar a personas mayores con sus dispositivos Android desde otro dispositivo Android.

**Usuario**: David Pedraza
**Experiencia**: Backend C#
**Objetivo**: MVP familiar → posible comercial

---

## Stack Tecnológico

### Backend
- **Framework**: ASP.NET Core 8
- **Lenguaje**: C#
- **Base de datos**: SQLite (desarrollo), SQL Server Express (producción)
- **ORM**: Entity Framework Core
- **Real-time**: SignalR + WebSockets
- **Autenticación**: JWT Bearer tokens
- **Hosting**: Dondomino.es

### Frontend
- **Plataforma**: Android (Kotlin nativo)
- **Senior App**: Captura de pantalla + Generador QR
- **Support App**: Scanner QR + Visor de pantalla

---

## Requisitos Especiales

### Internacionalización (i18n)
- Idiomas: Español (es), English (en), Catalan (ca)
- Sistema: Archivos JSON + LocalizationService
- Diseñado para agregar idiomas fácilmente sin cambiar código

### Seguridad
- HTTPS/TLS 1.3 obligatorio
- JWT con 15 minutos de expiración
- Passwords con bcrypt (12 rounds)
- Auditoría completa (retención: 3 meses)
- Rate limiting en endpoints de auth

### Arquitectura
- Capas: Controllers → Services → Repositories → Data
- Patrón Repository + Service
- Dependency Injection obligatorio
- SOLID principles
- Async/await en todo

---

## Estructura del Repositorio

```
AyudaRemota/
├── src/
│   ├── RemoteCare.Api/              # ASP.NET Core backend
│   │   ├── Controllers/
│   │   ├── Services/
│   │   ├── Data/
│   │   ├── Models/
│   │   ├── Dtos/
│   │   ├── Hubs/                    # SignalR
│   │   ├── Middleware/
│   │   ├── Localization/            # i18n
│   │   ├── Exceptions/
│   │   └── Program.cs
│   └── RemoteCare.Common/           # Utilidades compartidas
│
├── tests/
│   └── RemoteCare.Api.Tests/        # Unit + Integration tests
│
├── android-senior/                  # App Senior
│   └── app/src/main/
│
├── android-support/                 # App Support
│   └── app/src/main/
│
├── docs/                            # Documentación completa
│   ├── ARCHITECTURE.md
│   ├── API_SPECIFICATION.md
│   ├── DATABASE_SCHEMA.md
│   ├── SECURITY_CONSIDERATIONS.md
│   ├── DEVELOPMENT_GUIDE.md
│   ├── i18n_IMPLEMENTATION.md
│   └── DEPLOYMENT_GUIDE.md
│
├── .gitignore
├── README.md
├── CLAUDE.md                        # Este archivo
├── RemoteCare.sln
└── LICENSE (MIT)
```

---

## Convenciones de Código

### C# / .NET
- **Naming**: PascalCase para classes, camelCase para variables
- **Async**: Todo async por defecto
- **DTOs**: En carpeta `Dtos/` organizados por feature
- **Services**: Interfaz + Implementación
- **Repositories**: Generic + Specific repositories
- **Migrations**: Nombrar descriptivamente: `Add[Feature]` o `Update[Table]`
- **Exceptions**: Custom exceptions en carpeta `Exceptions/`

### Android / Kotlin
- **Naming**: camelCase para variables/funciones, PascalCase para clases
- **Structure**: MVVM o Clean Architecture
- **Strings**: En `values/strings.xml` (nunca hardcoded)
- **Layout**: XML descriptivo

---

## Workflow de Desarrollo

### Agregar Nueva Feature

1. **Crear Entity** (si aplica)
   ```
   Models/MyFeature.cs → DbContext → Migration
   ```

2. **Crear DTO**
   ```
   Dtos/MyFeature/CreateRequest.cs
   Dtos/MyFeature/MyFeatureDto.cs
   ```

3. **Crear Repository**
   ```
   Data/Repositories/IMyFeatureRepository.cs
   Data/Repositories/MyFeatureRepository.cs
   ```

4. **Crear Service**
   ```
   Services/IMyFeatureService.cs
   Services/MyFeatureService.cs
   ```

5. **Crear Controller**
   ```
   Controllers/MyFeatureController.cs
   ```

6. **Crear Tests**
   ```
   Tests/Unit/Services/MyFeatureServiceTests.cs
   ```

### Commits

Usar Conventional Commits:
```
feat: Add new feature description
fix: Fix bug description
docs: Update documentation
refactor: Refactor code
test: Add tests
chore: Maintenance
```

### Branches

```
master          # Production
├── feature/auth-system
├── feature/device-pairing
└── bugfix/login-issue
```

---

## Pautas para Claude

Cuando trabajes en este proyecto:

### ✅ HACES
- Seguir la estructura en capas
- Usar DI y interfaces
- Escribir tests para lógica crítica
- Documentar cambios
- Mantener seguridad en mente
- Usar async/await
- Validar inputs
- Loguear eventos importantes

### ❌ NO HACES
- Hardcodear valores/strings
- Ignorar validaciones
- Cambiar estructura de carpetas sin avisar
- Commitear secretos o credenciales
- Saltarse tests
- Olvidar i18n
- Ignorar seguridad

---

## Documentación

Documentación completa en `/docs/`:

1. **ARCHITECTURE.md** - Arquitectura general y diagrama de flujo
2. **API_SPECIFICATION.md** - Todos los endpoints REST + WebSocket
3. **DATABASE_SCHEMA.md** - Diseño de tablas y relaciones
4. **SECURITY_CONSIDERATIONS.md** - JWT, encriptación, validaciones
5. **DEVELOPMENT_GUIDE.md** - Cómo desarrollar, debugging, testing
6. **i18n_IMPLEMENTATION.md** - Sistema de traducciones
7. **DEPLOYMENT_GUIDE.md** - Cómo desplegar

---

## Cambios Especiales

### Retención de Auditoría
- Se redujo a **3 meses** (en lugar de 1 año)
- Implementar política de limpieza de datos antiguos

---

## Recursos Útiles

- **ASP.NET Core Docs**: https://docs.microsoft.com/en-us/aspnet/core/
- **Entity Framework Core**: https://docs.microsoft.com/en-us/ef/core/
- **SignalR**: https://docs.microsoft.com/en-us/aspnet/core/signalr/
- **JWT**: https://jwt.io/
- **Kotlin Android**: https://developer.android.com/kotlin

---

## Primeros Pasos (Sprint 1)

```bash
# 1. Clonar repo
git clone https://github.com/DavidPedraza/AyudaRemota.git
cd AyudaRemota

# 2. Restaurar dependencias
dotnet restore

# 3. Configurar BD local
cd src/RemoteCare.Api
dotnet ef database update

# 4. Ejecutar servidor
dotnet run

# 5. API disponible en https://localhost:5001
```

---

**Última actualización**: Marzo 6, 2026
**Versión**: 0.1.0-alpha
