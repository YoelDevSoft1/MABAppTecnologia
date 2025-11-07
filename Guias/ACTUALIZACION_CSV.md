# ✅ Actualización de Consorcios desde Nomenclatura Equipos

## 📊 Resumen de la Conversión

**Fecha:** 2024-11-06

### Archivo Origen:
- **Nombre:** `Config/Nomenclatura Equipos.csv`
- **Contenido:** Base de datos completa con todos los proyectos y áreas de MAB
- **Columnas:** ÁREA/PROYECTO, Siglas, Hostname, Contraseña, PIN

### Archivo Destino:
- **Nombre:** `Config/consorcios.csv`
- **Formato:** Consorcio, Siglas, ContraseñaAdmin, PinAdmin
- **Total registros:** 93 consorcios/proyectos

---

## 🎯 Registros Convertidos

### Oficinas Principales (18 registros):
- Área Gerencia General (GAL)
- Área SubGerencia General (SGAL)
- Área Gerencia Técnica (GCA)
- Área SubGerencia Técnica (SGCA)
- Área Talento Humano (TNO)
- Área RSE (RSE)
- Área HSE (HSE)
- Área Recepción (RION)
- Área Calidad (CAD)
- Área BIM (BIM)
- Área APU (APU)
- MABTEC (MEC)
- Área Liquidaciones (LES)
- ... y más

### Proyectos Activos (75+ registros):
- 002-C.M.A.PASTO (CHAPAL)
- Alsacia (ALCA)
- 476-GIC (GIC)
- CATATUMBO APC (APC)
- 147-4G Barranquilla (4G)
- IDU 607 (IDU607)
- IDU 617 (IDU617)
- Ruta 190 - La Macarena (RUT190)
- Metro de Bogota (PLMB)
- INVIAS BOYACA (BOYACA)
- TRIPLE A (TRIPLEA)
- PERU (MABPERU)
- AVIANCA (AVIANCA)
- LATAM (LTM)
- EPM (EPM)
- ... y muchos más

---

## 🔒 Credenciales Aplicadas

**Contraseña Admin:** `xX$T3cH_M4b$Xx` (para todos los equipos)
**PIN Admin:** `86138` (para todos los equipos)

---

## 📁 Archivos Actualizados

✅ `Config/consorcios.csv` (origen)
✅ `bin/Debug/net8.0-windows/Config/consorcios.csv`
✅ `bin/Release/net8.0-windows/Config/consorcios.csv`
✅ `Publish/Config/consorcios.csv`

**Todos con 94 líneas:** 1 encabezado + 93 registros

---

## 🛠️ Herramientas Creadas

### 1. `convertir_nomenclatura.py`
Script Python que automatiza la conversión de "Nomenclatura Equipos.csv" a "consorcios.csv"

**Funciones:**
- Lee el archivo fuente con manejo de encoding UTF-8
- Filtra encabezados y separadores automáticamente
- Extrae: Nombre, Siglas, Contraseña y PIN
- Limpia texto (elimina saltos de línea extras)
- Valida que los registros tengan datos válidos
- Genera el CSV con formato correcto

### 2. `Convertir_Nomenclatura.bat`
Script BAT para ejecutar fácilmente la conversión

**Uso:**
```bash
.\Convertir_Nomenclatura.bat
```

---

## 🚀 Cómo Actualizar en el Futuro

### Si se agregan nuevos proyectos a "Nomenclatura Equipos.csv":

1. **Editar el archivo fuente:**
   - Abrir `Config/Nomenclatura Equipos.csv`
   - Agregar la nueva fila con el formato existente
   - Guardar

2. **Ejecutar conversión:**
   ```bash
   cd MABAppTecnologia
   .\Convertir_Nomenclatura.bat
   ```

3. **Recompilar:**
   ```bash
   dotnet build --configuration Release
   dotnet publish -c Release -r win-x64 --self-contained false -o Publish
   ```

4. **Verificar:**
   ```bash
   .\Ejecutar_Debug.bat
   # O
   cd Publish
   .\Ejecutar_MAB_App.bat
   ```

---

## 📝 Formato del CSV Original

```
ÁREA/PROYECTO, Siglas, 4 DÍGITOS S/N, HOSTNAME MAB, HOSTNAME ALQUILER, HOSTNAME ALQUILER CASA, CONTRASEÑA, PIN
```

## 📝 Formato del CSV Convertido

```
Consorcio, Siglas, ContraseñaAdmin, PinAdmin
```

---

## ✨ Ventajas de la Conversión Automática

✅ No hay que editar manualmente 93 registros
✅ Elimina errores de copiado/pegado
✅ Mantiene la consistencia de datos
✅ Fácil de actualizar cuando se agreguen proyectos
✅ Un solo archivo fuente (Nomenclatura Equipos.csv)
✅ Conversión instantánea (menos de 1 segundo)

---

## 🧪 Estado Actual

| Versión | Consorcios | Estado |
|---------|-----------|---------|
| Debug | 93 | ✅ Listo |
| Release | 93 | ✅ Listo |
| Publish | 93 | ✅ Listo |

---

## 📊 Antes vs Después

| Métrica | Antes | Después |
|---------|-------|---------|
| Consorcios | 5 (ejemplo) | 93 (reales) |
| Edición manual | Sí | No |
| Actualización | Manual | Automática |
| Consistencia | Baja | Alta |
| Tiempo de actualización | 30+ min | < 1 min |

---

## 🎓 Ejemplo de Uso

```bash
# 1. Actualizar Nomenclatura Equipos.csv (agregar nuevos proyectos)
# 2. Ejecutar conversión
cd MABAppTecnologia
.\Convertir_Nomenclatura.bat

# 3. Recompilar
dotnet build

# 4. Probar
.\Ejecutar_Debug.bat
```

---

## 📞 Soporte

Si necesitas agregar, modificar o eliminar consorcios:

1. Edita `Config/Nomenclatura Equipos.csv` (archivo maestro)
2. Ejecuta `.\Convertir_Nomenclatura.bat`
3. Recompila el proyecto

**NO edites directamente `consorcios.csv`** - se sobrescribirá en la próxima conversión.

---

**Última actualización:** 2024-11-06
**Registros totales:** 93 consorcios/proyectos
**Script de conversión:** `convertir_nomenclatura.py`
