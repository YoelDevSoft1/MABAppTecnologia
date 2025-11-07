# 🎨 Cómo Usar la Personalización - Guía Completa

## ⚠️ IMPORTANTE: Permisos de Administrador

La personalización (Paso 3) **REQUIERE** que la aplicación se ejecute como **Administrador** porque realiza operaciones del sistema:

- ✅ Cambiar fondos de pantalla del sistema
- ✅ Configurar pantalla de bloqueo (requiere modificar registro HKLM)
- ✅ Establecer imágenes de perfil de usuario
- ✅ Modificar configuraciones del registro de Windows

---

## 📋 Antes de Empezar

### 1. Verificar que las Imágenes Están en su Lugar

Las imágenes deben estar en `C:\MAB-Resources`:

```
C:\MAB-Resources\
├── ProfileImages\
│   ├── admin_profile.jpg  ✅
│   └── mab_profile.jpg    ✅
└── Wallpapers\
    ├── admin_lockscreen.jpg  ✅
    ├── admin_wallpaper.png   ✅
    ├── mab_lockscreen.jpg    ✅
    └── mab_wallpaper.png     ✅
```

**Estado Actual:** ✅ Todas las imágenes están en su lugar

---

## 🚀 Cómo Ejecutar Correctamente

### Opción 1: Clic Derecho → Ejecutar como Administrador

1. Navega a: `MABAppTecnologia\Publish\`
2. **Clic derecho** en `MABAppTecnologia.exe`
3. Selecciona **"Ejecutar como administrador"**
4. Acepta el UAC (Control de Cuentas de Usuario)

### Opción 2: Usar el Script de Ejecución

Ya existe un script que lo hace automáticamente:

```powershell
# Desde PowerShell como Administrador:
cd C:\Users\Admin\Documents\MAB-EQUIPOS\MABAppTecnologia
.\Ejecutar_Release.bat
```

---

## 📝 Proceso Paso a Paso

### Paso 1: Nomenclatura ✅
- No requiere permisos especiales
- Renombra el equipo

### Paso 2: Usuarios ✅ (Requiere Admin)
- Crea usuario ADMIN con contraseña
- Crea usuario MAB sin contraseña
- Configura PINs

### Paso 3: Personalización ⚠️ (REQUIERE ADMIN)

Este paso realiza 3 operaciones:

#### Para Usuario ADMIN:
1. **Wallpaper:** `admin_wallpaper.png`
   - Se configura mediante PowerShell y registro
   - Se aplica inmediatamente

2. **Lockscreen:** `admin_lockscreen.jpg`
   - Se configura en: `HKLM:\SOFTWARE\Policies\Microsoft\Windows\Personalization`
   - Requiere permisos de administrador

3. **Imagen de Perfil:** `admin_profile.jpg`
   - Se copia a: `C:\ProgramData\Microsoft\User Account Pictures\`
   - Se configura en el registro para cada resolución (32, 40, 48, 96, 192, 240, 448)
   - Requiere obtener el SID del usuario

#### Para Usuario MAB:
- Mismo proceso con las imágenes `mab_*`

---

## 🔍 Verificar si Funcionó

### 1. Revisar el Log

El log se encuentra en: `MABAppTecnologia\Publish\Logs\`

**Busca líneas como estas:**

✅ **Éxito:**
```
[INFO] Imagen encontrada: C:\MAB-Resources\Wallpapers\admin_wallpaper.png
[INFO] Configurando fondo de pantalla para ADMIN: C:\MAB-Resources\Wallpapers\admin_wallpaper.png
[SUCCESS] Fondo de pantalla configurado para ADMIN
[INFO] Configurando pantalla de bloqueo: C:\MAB-Resources\Wallpapers\admin_lockscreen.jpg
[SUCCESS] Pantalla de bloqueo configurada
[INFO] Configurando imagen de perfil para ADMIN: C:\MAB-Resources\ProfileImages\admin_profile.jpg
[SUCCESS] Imagen de perfil configurada para ADMIN
```

❌ **Error:**
```
[WARNING] No se encontró el fondo de pantalla: admin_wallpaper.jpg
[ERROR] Error al configurar pantalla de bloqueo: Acceso denegado
```

### 2. Verificar Visualmente

#### Fondo de Pantalla:
- El escritorio debe mostrar la imagen configurada
- Windows + D para ver el escritorio

#### Lockscreen:
- Windows + L para bloquear
- Deberías ver la imagen configurada

#### Imagen de Perfil:
- Abre "Configuración de Windows"
- Ve a "Cuentas" → "Tu información"
- Deberías ver la imagen del usuario

---

## 🐛 Problemas Comunes y Soluciones

### ❌ "Error: Acceso denegado"
**Causa:** La aplicación no está corriendo como administrador  
**Solución:** Ejecutar como administrador

### ❌ "No se encontró la imagen..."
**Causa:** Las imágenes no están en `C:\MAB-Resources`  
**Solución:** 
1. Asegúrate de ejecutar la aplicación primero (copia recursos automáticamente)
2. Verifica que las imágenes estén en `MABAppTecnologia\Publish\Resources\`

### ❌ "Algunas operaciones de personalización fallaron"
**Causa:** Una o más de las 3 operaciones falló (wallpaper, lockscreen, o perfil)  
**Solución:** Revisar el log para ver cuál específicamente falló

### ❌ El wallpaper no cambia
**Causa:** Posible GPO (Group Policy) o configuración corporativa  
**Solución:** 
```powershell
# Verificar si hay GPO bloqueando:
Get-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Policies\System"
```

### ❌ La imagen de perfil no aparece
**Causa:** El usuario no tiene SID asignado o no está creado aún  
**Solución:** Asegurarse de ejecutar Paso 2 (Usuarios) antes del Paso 3 (Personalización)

---

## 🧪 Prueba Rápida

Para probar solo la personalización sin hacer todo el proceso:

```powershell
# Como Administrador:
cd C:\Users\Admin\Documents\MAB-EQUIPOS\MABAppTecnologia\Publish

# Verificar que las imágenes estén:
Get-ChildItem "C:\MAB-Resources\ProfileImages"
Get-ChildItem "C:\MAB-Resources\Wallpapers"

# Ejecutar la aplicación:
Start-Process .\MABAppTecnologia.exe -Verb RunAs
```

Luego en la app:
1. Selecciona cualquier consorcio
2. Ve directamente al **Paso 3: Personalización**
3. Haz clic en "Ejecutar Paso"
4. Revisa el log

---

## 📊 Estado Actual del Sistema

### Imágenes Disponibles: ✅

| Imagen | Ubicación | Tamaño | Estado |
|--------|-----------|--------|--------|
| admin_profile.jpg | C:\MAB-Resources\ProfileImages\ | 79 KB | ✅ |
| mab_profile.jpg | C:\MAB-Resources\ProfileImages\ | 85 KB | ✅ |
| admin_wallpaper.png | C:\MAB-Resources\Wallpapers\ | 1.4 MB | ✅ |
| admin_lockscreen.jpg | C:\MAB-Resources\Wallpapers\ | 316 KB | ✅ |
| mab_wallpaper.png | C:\MAB-Resources\Wallpapers\ | 1.4 MB | ✅ |
| mab_lockscreen.jpg | C:\MAB-Resources\Wallpapers\ | 316 KB | ✅ |

### Código de Detección Flexible: ✅

El código ahora busca automáticamente:
- ✅ `.png`
- ✅ `.jpg`
- ✅ `.jpeg`
- ✅ `.bmp`

No necesitas renombrar tus archivos.

---

## 🎯 Resumen

1. **Ejecuta la aplicación como Administrador** (obligatorio para Paso 3)
2. Las imágenes ya están en su lugar ✅
3. El código está funcionando correctamente ✅
4. Revisa el log después de ejecutar para ver cualquier error específico

---

## 📞 Siguiente Paso

Ejecuta la aplicación **como administrador** y prueba el Paso 3. Si sigue fallando, comparte conmigo las últimas líneas del log que mencionen "personalización" o "ADMIN".

