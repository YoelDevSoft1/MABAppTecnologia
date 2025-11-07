# 🧪 Cómo Probar MAB APP TECNOLOGIA

## 📋 Resumen Rápido

| Versión | Dónde está | Cuándo usarla | Cómo ejecutar |
|---------|-----------|---------------|---------------|
| **Debug** | `bin\Debug\net8.0-windows\` | Durante desarrollo y pruebas | `Ejecutar_Debug.bat` |
| **Release** | `bin\Release\net8.0-windows\` | Prueba antes de publicar | `Ejecutar_Release.bat` |
| **Publish** | `Publish\` | Distribución final a técnicos | `Publish\Ejecutar_MAB_App.bat` |

---

## 🔧 Modo DEBUG (Desarrollo)

### ¿Cuándo usar?
- Durante el desarrollo
- Para probar cambios rápidos
- Cuando necesitas logs detallados

### Cómo ejecutar:
```bash
# Opción 1: Script
.\Ejecutar_Debug.bat

# Opción 2: Compilar y ejecutar
dotnet build --configuration Debug
cd bin\Debug\net8.0-windows
.\MABAppTecnologia.exe
```

### Ubicación:
```
MABAppTecnologia\
└── bin\Debug\net8.0-windows\
    ├── MABAppTecnologia.exe
    ├── Config\
    ├── Resources\
    ├── Software\
    └── Logs\
```

---

## 🚀 Modo RELEASE (Pre-distribución)

### ¿Cuándo usar?
- Prueba final antes de distribuir
- Verificar que todo funciona como en producción
- Versión optimizada (más rápida)

### Cómo ejecutar:
```bash
# Opción 1: Script
.\Ejecutar_Release.bat

# Opción 2: Compilar y ejecutar
dotnet build --configuration Release
cd bin\Release\net8.0-windows
.\MABAppTecnologia.exe
```

### Ubicación:
```
MABAppTecnologia\
└── bin\Release\net8.0-windows\
    ├── MABAppTecnologia.exe
    ├── Config\
    ├── Resources\
    ├── Software\
    └── Logs\
```

---

## 📦 Modo PUBLISH (Distribución)

### ¿Cuándo usar?
- Versión FINAL para técnicos
- Lista para copiar a USB/red
- Incluye todas las dependencias necesarias

### Cómo crear:
```bash
# Opción 1: Script
.\Publicar.bat

# Opción 2: Manual
dotnet publish -c Release -r win-x64 --self-contained false -o Publish
```

### Cómo ejecutar:
```bash
cd Publish
.\Ejecutar_MAB_App.bat
```

### Estructura:
```
Publish\                          ← Esta carpeta es la que distribuyes
├── MABAppTecnologia.exe
├── Ejecutar_MAB_App.bat         ← Launcher
├── Verificar_Configuracion.bat  ← Verificador
├── Config\
│   ├── consorcios.csv
│   └── settings.json
├── Resources\
│   ├── Wallpapers\
│   └── ProfileImages\
├── Software\
├── Logs\
├── LEEME_PRIMERO.txt           ← Instrucciones
├── GUIA_RAPIDA.md
├── README.md
└── SOLUCION_PROBLEMAS.md
```

---

## ✅ Checklist de Prueba

### Antes de publicar:

- [ ] **1. Probar en DEBUG**
  ```bash
  .\Ejecutar_Debug.bat
  ```
  - ✓ Dropdown muestra consorcios
  - ✓ Botones visibles
  - ✓ No hay errores en logs

- [ ] **2. Verificar configuración**
  ```bash
  .\Verificar_Configuracion.bat
  ```
  - ✓ Todos los archivos presentes

- [ ] **3. Probar en RELEASE**
  ```bash
  .\Ejecutar_Release.bat
  ```
  - ✓ Funciona igual que Debug
  - ✓ No hay errores

- [ ] **4. Publicar**
  ```bash
  .\Publicar.bat
  ```
  - ✓ Se crea carpeta Publish/

- [ ] **5. Probar PUBLISH**
  ```bash
  cd Publish
  .\Ejecutar_MAB_App.bat
  ```
  - ✓ Funciona correctamente
  - ✓ CSV se carga
  - ✓ Todos los pasos funcionan

- [ ] **6. Verificar PUBLISH**
  ```bash
  cd Publish
  .\Verificar_Configuracion.bat
  ```
  - ✓ Todos los archivos presentes

---

## 🔍 Comparación de Versiones

### DEBUG vs RELEASE vs PUBLISH

| Característica | Debug | Release | Publish |
|----------------|-------|---------|---------|
| Tamaño | Grande | Mediano | Optimizado |
| Velocidad | Lenta | Rápida | Rápida |
| Logs | Muy detallados | Normales | Normales |
| Optimización | No | Sí | Sí |
| Símbolos debug | Sí | No | No |
| Para distribución | ❌ | ⚠️ | ✅ |

---

## 🐛 Si Release no funciona pero Debug sí

### Problema común: Archivos no se copian

**Solución:**
```bash
# Limpiar todo
dotnet clean

# Restaurar dependencias
dotnet restore

# Compilar Release
dotnet build --configuration Release

# Verificar que se copiaron los archivos
dir bin\Release\net8.0-windows\Config\
dir bin\Release\net8.0-windows\Resources\
dir bin\Release\net8.0-windows\Software\
```

### Problema: "No se encuentran consorcios"

**Verificar:**
```bash
# ¿Existe el CSV?
type bin\Release\net8.0-windows\Config\consorcios.csv

# ¿Tiene contenido?
```

Si NO existe, el `.csproj` no está copiando los archivos.

**Verificar en MABAppTecnologia.csproj:**
```xml
<ItemGroup>
  <!-- DEBE tener esto -->
  <None Update="Config\**\*.*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
  <None Update="Resources\**\*.*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
  <None Update="Software\**\*.*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

---

## 📝 Logs de Prueba

Los logs están en cada carpeta:
- Debug: `bin\Debug\net8.0-windows\Logs\`
- Release: `bin\Release\net8.0-windows\Logs\`
- Publish: `Publish\Logs\`

**Revisar el log más reciente:**
```
MAB_Log_YYYYMMDD_HHMMSS.txt
```

Busca:
- `[ERROR]` → Errores críticos
- `[SUCCESS] Se cargaron X consorcios` → Debe ser 5 (o tu cantidad)

---

## 🎯 Proceso Recomendado

```
1. Desarrollar → probar con DEBUG
2. Funciona? → probar con RELEASE
3. Release OK? → PUBLICAR
4. Probar PUBLISH
5. PUBLISH OK? → Distribuir a técnicos
```

---

## 📞 ¿Necesitas ayuda?

- Debug funciona, Release no → Ver "Si Release no funciona" arriba
- Publish funciona, problemas en otros PCs → Instalar .NET 8 Runtime
- Dropdown vacío → Ver [SOLUCION_PROBLEMAS.md](SOLUCION_PROBLEMAS.md)
- Botones no visibles → Redimensionar ventana o ver solución arriba

---

**Última actualización:** 2024-11-06
