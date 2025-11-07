# 🔧 Solución de Problemas - MAB APP TECNOLOGIA

## ❌ Problema: El dropdown de consorcios está vacío

### Posibles Causas:
1. El archivo CSV no está en la ubicación correcta
2. El formato del CSV es incorrecto
3. El archivo tiene problemas de encoding

### Soluciones:

#### 1. Verificar ubicación del CSV
- El archivo debe estar en: `Config\consorcios.csv`
- Al ejecutar la aplicación, se copia a la carpeta de ejecución
- **Ubicación en Debug:** `bin\Debug\net8.0-windows\Config\consorcios.csv`
- **Ubicación en Release:** `bin\Release\net8.0-windows\Config\consorcios.csv`

#### 2. Verificar formato del CSV
El archivo debe tener EXACTAMENTE este formato:
```csv
Consorcio,Siglas,ContraseñaAdmin,PinAdmin
Santa Ana,STA,Admin2024!,1234
Miraflores,MIR,Admin2024!,1234
```

**⚠ Importante:**
- Primera línea DEBE ser el encabezado
- NO debe haber espacios antes o después de las comas
- NO debe haber líneas vacías al principio
- Usar codificación UTF-8

#### 3. Revisar los logs
Busca en la carpeta `Logs\` el archivo más reciente:
- Si dice "No se encontró el archivo CSV" → Verifica la ubicación
- Si dice "Error al leer el archivo CSV" → Verifica el formato

#### 4. Solución rápida
1. Cierra la aplicación
2. Elimina el archivo `Config\consorcios.csv`
3. Crea uno nuevo con el formato correcto (ver arriba)
4. Guarda con codificación UTF-8 (sin BOM)
5. Ejecuta: `dotnet build`
6. Vuelve a ejecutar la aplicación

---

## ❌ Problema: Los botones de abajo no se ven

### Causa:
La ventana es muy pequeña o los botones están fuera del área visible.

### Soluciones:

#### 1. Redimensionar la ventana
- Maximiza la ventana
- O arrastra desde las esquinas para agrandarla

#### 2. Cambiar resolución mínima
Si tu pantalla es pequeña, edita `MainWindow.xaml`:
```xml
Height="700" Width="1000"  <!-- Cambia a valores más pequeños -->
```

Por ejemplo:
```xml
Height="600" Width="900"
```

#### 3. Ya corregido en la última versión
- La altura de la fila de botones se aumentó de 70px a 110px
- Recompila el proyecto con `dotnet build`

---

## ❌ Problema: Error "No se puede ejecutar porque requiere administrador"

### Solución:
1. Clic derecho en `MABAppTecnologia.exe`
2. Selecciona "Ejecutar como administrador"
3. O usa el script: `Ejecutar_Debug.bat` (ya lo hace automáticamente)

---

## ❌ Problema: Error al renombrar el equipo

### Posibles Causas:
1. El equipo está unido a un dominio
2. No tienes permisos de administrador
3. El nombre ya existe en la red

### Soluciones:
1. Ejecuta como administrador
2. Si está en dominio, desinscríbelo primero
3. Revisa que el nombre generado sea único

---

## ❌ Problema: Software no se instala

### Soluciones:

#### Instalación silenciosa falla:
- Es normal, algunos instaladores no soportan modo silencioso
- La aplicación automáticamente intentará modo interactivo
- Instala manualmente cuando se abra el instalador

#### Archivo no encontrado:
- Verifica que el archivo .exe o .msi esté en `Software\`
- No uses subcarpetas
- El nombre del archivo no debe tener caracteres especiales

---

## ❌ Problema: Fondos de pantalla no se aplican

### Soluciones:

#### 1. Verifica que los archivos existen:
```
Resources\Wallpapers\admin_wallpaper.jpg
Resources\Wallpapers\admin_lockscreen.jpg
Resources\Wallpapers\mab_wallpaper.jpg
Resources\Wallpapers\mab_lockscreen.jpg
```

#### 2. Verifica nombres de archivo:
- Deben ser **exactamente** como se indica
- Extensiones: `.jpg`, `.jpeg` o `.png`
- Si usas `.jpeg` o `.png`, edita `Config\settings.json`

#### 3. Los archivos se copian a:
`C:\MAB-Resources\Wallpapers\`

#### 4. Revisa permisos:
- Ejecuta como administrador
- Verifica que puedes escribir en C:\

---

## ❌ Problema: La aplicación se cierra inmediatamente

### Soluciones:

#### 1. Revisa los logs:
- Busca en `Logs\MAB_Log_*.txt`
- El último archivo tiene la información del error

#### 2. Falta .NET 8:
- Descarga e instala .NET 8 Runtime Desktop
- Link: https://dotnet.microsoft.com/download/dotnet/8.0

#### 3. Error de dependencias:
```bash
cd MABAppTecnologia
dotnet restore
dotnet build
```

---

## 📝 Cómo revisar los Logs

Los logs están en: `Logs\MAB_Log_YYYYMMDD_HHMMSS.txt`

**Busca estos indicadores:**
- `[ERROR]` - Errores críticos
- `[WARNING]` - Advertencias
- `[SUCCESS]` - Operaciones exitosas
- `[INFO]` - Información general

**Ejemplo de log útil:**
```
[2024-11-06 13:00:00] [INFO] Intentando cargar CSV desde: C:\...\Config\consorcios.csv
[2024-11-06 13:00:01] [SUCCESS] Se cargaron 5 consorcios desde CSV
[2024-11-06 13:00:01] [INFO] Consorcio cargado: Santa Ana (STA)
```

---

## 🔄 Cómo recompilar correctamente

### Método 1: Usando el script
```bash
cd MABAppTecnologia
.\Publicar.bat
```

### Método 2: Manual
```bash
cd MABAppTecnologia
dotnet clean
dotnet restore
dotnet build --configuration Release
```

### Verificar que todo se copió:
```bash
dir bin\Release\net8.0-windows\Config\
dir bin\Release\net8.0-windows\Resources\
dir bin\Release\net8.0-windows\Software\
```

Debes ver los archivos CSV, imágenes y software.

---

## 🆘 Si nada funciona:

1. **Elimina las carpetas bin y obj:**
   ```bash
   rmdir /s /q bin
   rmdir /s /q obj
   ```

2. **Restaura y recompila:**
   ```bash
   dotnet restore
   dotnet build
   ```

3. **Verifica requisitos:**
   - Windows 11
   - .NET 8 Runtime
   - Permisos de administrador

4. **Revisa el log más reciente** en la carpeta `Logs\`

5. **Contacta a soporte técnico** con:
   - Archivo de log
   - Mensaje de error
   - Captura de pantalla

---

## 📞 Información de Soporte

- **Logs:** `Logs\MAB_Log_*.txt`
- **Config:** `Config\consorcios.csv` y `Config\settings.json`
- **Versión:** 1.0.0

---

**Última actualización:** 2024-11-06
