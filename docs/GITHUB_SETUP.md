# Configuración Inicial - GitHub Setup

Instrucciones paso a paso para configurar el repositorio AyudaRemota en GitHub.

---

## ✅ Paso 1: Crear repositorio en GitHub

1. Ve a **https://github.com/new**
2. Completa los datos:
   - **Repository name**: `AyudaRemota`
   - **Description**: `App de soporte remoto para personas mayores`
   - **Visibility**: `Private` (o Public, según prefieras)
   - **Initialize repository**: NO marques nada (vamos a hacer push inicial)

3. Haz clic en **"Create repository"**

---

## ✅ Paso 2: Descargar archivos que he preparado

Descargar desde `C:\temp\`:
- `.gitignore`
- `CLAUDE.md`
- `README.md` (ya existe)
- `ARCHITECTURE.md`
- `API_SPECIFICATION.md`
- `DATABASE_SCHEMA.md`
- `SECURITY_CONSIDERATIONS.md`
- `DEVELOPMENT_GUIDE.md`
- `i18n_IMPLEMENTATION.md`

---

## ✅ Paso 3: Crear estructura local

En tu máquina:

```bash
# Crear carpeta del proyecto
mkdir AyudaRemota
cd AyudaRemota

# Crear estructura de carpetas
mkdir -p src/RemoteCare.Api
mkdir -p src/RemoteCare.Common
mkdir -p tests/RemoteCare.Api.Tests
mkdir -p docs
mkdir -p android-senior
mkdir -p android-support

# Copiar archivos descargados
# - .gitignore (a raíz)
# - CLAUDE.md (a raíz)
# - README.md (a raíz)
# - Todos los .md de documentación a carpeta /docs
```

Estructura resultante:
```
AyudaRemota/
├── .gitignore
├── CLAUDE.md
├── README.md
├── docs/
│   ├── ARCHITECTURE.md
│   ├── API_SPECIFICATION.md
│   ├── DATABASE_SCHEMA.md
│   ├── SECURITY_CONSIDERATIONS.md
│   ├── DEVELOPMENT_GUIDE.md
│   ├── i18n_IMPLEMENTATION.md
│   └── DEPLOYMENT_GUIDE.md
├── src/
│   ├── RemoteCare.Api/
│   └── RemoteCare.Common/
├── tests/
│   └── RemoteCare.Api.Tests/
├── android-senior/
└── android-support/
```

---

## ✅ Paso 4: Inicializar Git local

```bash
cd AyudaRemota

# Inicializar Git
git init

# Configurar usuario (si es la primera vez)
git config user.name "David Pedraza"
git config user.email "tu@email.com"

# Agregar todos los archivos
git add .

# Primer commit
git commit -m "chore: Initial project structure and documentation"

# Verificar
git log --oneline
```

---

## ✅ Paso 5: Conectar con repositorio remoto en GitHub

```bash
# Agregar remote (reemplaza 'DavidPedraza' con tu usuario si es diferente)
git remote add origin https://github.com/DavidPedraza/AyudaRemota.git

# Verificar que se agregó
git remote -v

# Cambiar rama a 'main' (GitHub usa main por defecto)
git branch -M main

# Hacer push inicial
git push -u origin main
```

---

## ✅ Paso 6: Verificar en GitHub

1. Ve a https://github.com/DavidPedraza/AyudaRemota
2. Verifica que ves:
   - Carpetas: `src/`, `tests/`, `docs/`, `android-*/`
   - Archivos: `.gitignore`, `CLAUDE.md`, `README.md`
   - Carpeta `docs/` con toda la documentación

---

## ✅ Paso 7: Crear rama de desarrollo

```bash
# Crear rama 'develop' para desarrollo
git checkout -b develop

# Hacer push
git push -u origin develop

# Cambiar a develop por defecto
git checkout develop
```

---

## Estructura de Branches

Después de esto, tu flujo será:

```
main           ← Producción (releases)
├── develop    ← Desarrollo (integración)
│   ├── feature/auth-system
│   ├── feature/device-pairing
│   └── bugfix/...
```

---

## 🎯 Siguiente paso (Después de configurar GitHub)

Una vez que el repo esté en GitHub, podemos:

1. **Crear proyecto ASP.NET Core 8**
   ```bash
   dotnet new globaljson --sdk-version 8.0.0 --roll-forward latestFeature
   dotnet new sln -n AyudaRemota
   dotnet new webapi -n RemoteCare.Api -o src/RemoteCare.Api
   dotnet new classlib -n RemoteCare.Common -o src/RemoteCare.Common
   dotnet new xunit -n RemoteCare.Api.Tests -o tests/RemoteCare.Api.Tests
   dotnet sln add src/RemoteCare.Api/RemoteCare.Api.csproj
   dotnet sln add src/RemoteCare.Common/RemoteCare.Common.csproj
   dotnet sln add tests/RemoteCare.Api.Tests/RemoteCare.Api.Tests.csproj
   ```

2. **Instalar dependencias principales**
   ```bash
   dotnet add src/RemoteCare.Api/RemoteCare.Api.csproj package Microsoft.EntityFrameworkCore.Sqlite
   dotnet add src/RemoteCare.Api/RemoteCare.Api.csproj package System.IdentityModel.Tokens.Jwt
   dotnet add src/RemoteCare.Api/RemoteCare.Api.csproj package QRCoder
   dotnet add src/RemoteCare.Api/RemoteCare.Api.csproj package Microsoft.AspNetCore.SignalR
   ```

3. **Hacer commit**
   ```bash
   git add .
   git commit -m "feat: Add ASP.NET Core 8 project structure"
   git push
   ```

---

## 🆘 Troubleshooting

### Error: "fatal: remote origin already exists"
```bash
git remote remove origin
git remote add origin https://github.com/DavidPedraza/AyudaRemota.git
```

### Error: "Please tell me who you are"
```bash
git config --global user.name "David Pedraza"
git config --global user.email "tu@email.com"
```

### ¿Olvidaste agregar archivos?
```bash
git add .
git commit -m "docs: Add missing documentation files"
git push
```

---

## ✅ Verificación Final

Después de todo, ejecuta:

```bash
git log --oneline -5
git branch -a
git remote -v
```

Deberías ver:
- Al menos 1 commit
- Ramas: `main`, `develop`
- Remote: `origin` apuntando a GitHub

---

**¿Listo?** Avísame cuando hayas hecho estos pasos y continuamos con Sprint 1. 🚀
