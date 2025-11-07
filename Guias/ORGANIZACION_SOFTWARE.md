# 📁 Organización de Software con Subcarpetas

## ✨ Nueva Funcionalidad

La aplicación ahora **detecta automáticamente instaladores en subcarpetas**, permitiéndote organizar el software por categorías.

---

## 📂 Estructura Recomendada

```
Software/
├── Navegadores/
│   ├── GoogleChrome_Setup.exe
│   ├── Firefox_Setup.exe
│   └── Edge_Setup.exe
│
├── Office/
│   ├── Office_2021_Setup.exe
│   ├── AdobeReader_Setup.exe
│   └── LibreOffice_Setup.exe
│
├── Diseño/
│   ├── AutoCAD/
│   │   ├── AutoCAD_2024.exe
│   │   └── AutoCAD_Plugins.exe
│   └── Revit/
│       └── Revit_2024.exe
│
├── Herramientas/
│   ├── TeamViewer_Setup.exe
│   ├── AnyDesk_Setup.exe
│   └── VLC_Setup.exe
│
├── Antivirus/
│   ├── Kaspersky_Setup.exe
│   └── Norton_Setup.exe
│
├── Comunicación/
│   ├── Teams_Setup.exe
│   ├── Zoom_Setup.exe
│   └── Slack_Setup.exe
│
├── Multimedia/
│   ├── Premiere_Setup.exe
│   └── OBS_Setup.exe
│
└── EssentialApp.exe  ← Archivos en raíz se marcan como "General"
```

---

## 🎯 Ventajas de Usar Subcarpetas

✅ **Organización Visual**
- Software agrupado por categoría en la interfaz
- Fácil de encontrar el programa que necesitas

✅ **Etiquetas de Categoría**
- Cada instalador muestra su categoría con una etiqueta azul
- Las categorías se ordenan alfabéticamente

✅ **Jerarquía Multinivel**
- Puedes crear subcarpetas dentro de subcarpetas
- Ejemplo: `Diseño > AutoCAD > Plugins`
- Se muestra como: `Diseño > AutoCAD > Plugins`

✅ **Flexible**
- Archivos en la raíz de `Software/` se categorizan como "General"
- No necesitas cambiar nada en tu organización actual

---

## 🎨 Cómo se Muestra en la Aplicación

En el **Paso 4: Instalación de Software**, verás:

```
☑ [Antivirus] Kaspersky
   Kaspersky_Setup.exe

☐ [Comunicación] Teams
   Teams_Setup.exe

☐ [Comunicación] Zoom
   Zoom_Setup.exe

☐ [Diseño] AutoCAD
   AutoCAD_2024.exe

☑ [Navegadores] Chrome
   GoogleChrome_Setup.exe

☐ [Office] Adobe Reader
   AdobeReader_Setup.exe
```

- **Etiqueta azul** con el nombre de la categoría
- **Nombre del software** en negrita
- **Nombre del archivo** en gris
- **Checkbox** para seleccionar
- **Ordenado** por categoría y luego por nombre

---

## 🔧 Ejemplos de Organización

### Ejemplo 1: Por Tipo de Software
```
Software/
├── Navegadores/
├── Office/
├── Diseño/
├── Desarrollo/
└── Herramientas/
```

### Ejemplo 2: Por Departamento
```
Software/
├── Gerencia/
├── Técnico/
├── Comercial/
├── Financiero/
└── Todos/ (software común)
```

### Ejemplo 3: Por Prioridad
```
Software/
├── Esenciales/
├── Importantes/
├── Opcionales/
└── Específicos_Proyecto/
```

### Ejemplo 4: Multinivel
```
Software/
├── Diseño/
│   ├── 2D/
│   │   ├── AutoCAD.exe
│   │   └── DraftSight.exe
│   └── 3D/
│       ├── Revit.exe
│       └── SketchUp.exe
└── Office/
    ├── Microsoft/
    │   └── Office_2021.exe
    └── Adobe/
        └── Acrobat_Reader.exe
```

---

## 📝 Reglas de Detección

### ✅ Se Detectan:
- Archivos `.exe` en cualquier nivel
- Archivos `.msi` en cualquier nivel
- Carpetas anidadas (subcarpetas dentro de subcarpetas)

### ❌ Se Ignoran:
- Archivos que no sean `.exe` o `.msi`
- Carpetas vacías
- Archivos ocultos del sistema

### 📋 Categorización:
- **Raíz de Software/**: Categoría = "General"
- **Software/Navegadores/**: Categoría = "Navegadores"
- **Software/Diseño/AutoCAD/**: Categoría = "Diseño > AutoCAD"

---

## 🚀 Migración desde Organización Plana

Si actualmente tienes todos los instaladores en `Software/` sin subcarpetas:

### Opción 1: No hacer nada
- Los archivos en la raíz seguirán funcionando
- Se mostrarán con categoría "General"

### Opción 2: Organizar gradualmente
```bash
# 1. Crear carpetas
mkdir Software/Navegadores
mkdir Software/Office

# 2. Mover archivos
move Software/Chrome*.exe Software/Navegadores/
move Software/Office*.exe Software/Office/

# 3. Recompilar
dotnet build
```

---

## 🔍 Logs y Diagnóstico

La aplicación registra en los logs:
```
[INFO] Se encontraron 15 aplicaciones para instalar
[INFO] Categorías detectadas: General, Navegadores, Office, Diseño, Herramientas
```

Si una categoría no aparece:
- Verifica que la carpeta contenga archivos `.exe` o `.msi`
- Revisa el log para errores de lectura
- Confirma permisos de lectura en las subcarpetas

---

## 💡 Consejos

### 1. Nombres Descriptivos
✅ `Navegadores/GoogleChrome_Setup.exe`
❌ `Nav/chrome.exe`

### 2. Evitar Caracteres Especiales
✅ `Office/Adobe_Reader.exe`
❌ `Office/Adobe (Reader) [2024].exe`

### 3. Mantener Estructura Simple
✅ Máximo 2-3 niveles de profundidad
❌ No crear más de 5 niveles

### 4. Documentar
- Incluye archivos LEEME.txt en cada categoría
- Explica qué software debe ir en cada carpeta

---

## 🎓 Ejemplo Completo para MAB

Basado en tus proyectos y áreas:

```
Software/
├── _Esenciales/              ← Software que SIEMPRE se instala
│   ├── Chrome.exe
│   ├── Office_2021.exe
│   └── Teams.exe
│
├── Gerencia/
│   ├── PowerBI.exe
│   └── Visio.exe
│
├── Técnico/
│   ├── AutoCAD_2024.exe
│   ├── Revit_2024.exe
│   └── SAP2000.exe
│
├── Comunicaciones/
│   ├── Adobe_Creative_Cloud.exe
│   └── Canva_Desktop.exe
│
├── Tecnología/
│   ├── Visual_Studio.exe
│   ├── Git.exe
│   └── Docker_Desktop.exe
│
└── Proyectos/
    ├── BIM/
    │   ├── Revit_Plugins.exe
    │   └── Navisworks.exe
    └── Calidad/
        └── QualityControl_Tools.exe
```

---

## 🔄 Actualización Automática

Cada vez que ejecutas la aplicación:
1. Escanea la carpeta `Software/` y todas las subcarpetas
2. Detecta automáticamente nuevos instaladores
3. Asigna categorías según la estructura de carpetas
4. Ordena por categoría y nombre
5. Muestra en la interfaz

**No necesitas recompilar** si solo agregas/quitas instaladores.

---

## 📞 Preguntas Frecuentes

**P: ¿Puedo tener subcarpetas dentro de subcarpetas?**
R: Sí, se detectan hasta 10 niveles de profundidad. Ejemplo: `Software/Diseño/CAD/AutoCAD/Plugins/`

**P: ¿Qué pasa con los archivos en la raíz de Software/?**
R: Se muestran con categoría "General" y funcionan normalmente.

**P: ¿Se puede cambiar el nombre de las carpetas?**
R: Sí, el nombre de la carpeta se convierte en la categoría automáticamente.

**P: ¿La instalación funciona igual?**
R: Sí, la ruta completa se guarda, la instalación funciona idéntico a antes.

**P: ¿Puedo usar tildes y ñ en nombres de carpetas?**
R: Sí, se soportan caracteres UTF-8.

---

## ✅ Estado de la Funcionalidad

| Característica | Estado |
|---------------|---------|
| Detección recursiva | ✅ Implementado |
| Categorización automática | ✅ Implementado |
| UI con etiquetas | ✅ Implementado |
| Ordenamiento | ✅ Implementado |
| Multinivel | ✅ Implementado |
| Logs detallados | ✅ Implementado |

---

**Versión:** 1.1.0
**Fecha:** 2024-11-06
**Mejora:** Detección de software en subcarpetas
