# 🖥️ MAB APP TECNOLOGIA

**Versión 1.0.0** - Aplicación de Configuración Automatizada de Equipos

> Herramienta profesional para configurar, personalizar y optimizar equipos de cómputo de MAB Ingeniería de manera rápida y eficiente.

---

## 🌟 Características Principales

✅ **Nomenclatura Inteligente** - Renombrado automático según consorcio y tipo de equipo  
✅ **Gestión de Usuarios** - Creación y configuración de usuarios ADMIN y MAB  
✅ **Personalización Completa** - Fondos, lockscreen e imágenes de perfil  
✅ **Instalación de Software** - Detección y instalación automática de 33+ aplicaciones  
✅ **Optimización del Sistema** - 7 módulos de optimización integrados  
✅ **Soporte Flexible** - Múltiples formatos de imagen, 93 consorcios configurados  

---

## 📦 Contenido del Release v1.0.0

- 🎯 **Ejecutable principal:** `MABAppTecnologia.exe`
- 📂 **93 Consorcios configurados** en `Config/consorcios.csv`
- 🖼️ **Imágenes incluidas** para personalización
- 💿 **33 Aplicaciones** detectadas automáticamente
- 🔧 **7 Módulos de optimización** del sistema
- 📝 **Documentación completa** y logs detallados

---

## 🚀 Inicio Rápido

### 1. Requisitos Previos
- Windows 10/11 (64-bit)
- .NET 8 Runtime
- **Permisos de Administrador** (obligatorio)

### 2. Ejecución
```powershell
# Clic derecho en MABAppTecnologia.exe
# → "Ejecutar como administrador"
```

### 3. Pasos de Configuración
1. **Nomenclatura** - Selecciona consorcio y tipo de equipo
2. **Usuarios** - Crea ADMIN y MAB
3. **Personalización** - Aplica fondos e imágenes
4. **Software** - Instala aplicaciones seleccionadas
5. **Optimización** - Optimiza el sistema

---

## 📚 Documentación

| Documento | Descripción |
|-----------|-------------|
| [GUIA_RAPIDA.md](GUIA_RAPIDA.md) | Guía de uso completa |
| [CHANGELOG_v1.0.md](CHANGELOG_v1.0.md) | Cambios y características v1.0 |
| [FORMATOS_IMAGEN_FLEXIBLES.md](FORMATOS_IMAGEN_FLEXIBLES.md) | Soporte de formatos de imagen |
| [CORRECCION_USUARIO_MAB.md](CORRECCION_USUARIO_MAB.md) | Fix de creación del usuario MAB |
| [COMO_USAR_PERSONALIZACION.md](COMO_USAR_PERSONALIZACION.md) | Guía de personalización |
| [SOLUCION_PROBLEMAS.md](SOLUCION_PROBLEMAS.md) | Solución de problemas comunes |

---

## 🎯 Casos de Uso

### Para Técnicos
- ⚡ Configuración rápida de equipos nuevos
- 🔄 Estandarización de configuraciones
- 📊 Instalación masiva de software
- 🛠️ Optimización automática del sistema

### Para Administradores
- 📋 Gestión de 93 consorcios
- 👥 Control de nomenclatura de equipos
- 🎨 Personalización corporativa
- 📈 Logs detallados de operaciones

---

## 💡 Características Destacadas

### Nomenclatura Inteligente
```
Equipo Propio:      TI-XXXX
Equipo Alquiler:    TI-RUB-XXXX
Equipo Home Office: TI-HOME-XXXX
```

### Formatos de Imagen Soportados
- ✅ PNG (recomendado para perfiles)
- ✅ JPG/JPEG (recomendado para fondos)
- ✅ BMP
- ✅ GIF

### Optimizaciones Incluidas
1. **Optimizador Avanzado** - Script completo OptimizerMAB.ps1
2. **Privacidad** - Desactivación de telemetría
3. **Rendimiento** - Ajustes del sistema
4. **UX** - Mejoras de experiencia
5. **Bloatware** - Eliminación de apps innecesarias
6. **Inicio** - Optimización de arranque
7. **Limpieza** - Archivos temporales

---

## 🔧 Estructura del Proyecto

```
MABAppTecnologia/
├── 📄 MABAppTecnologia.exe        ← Ejecutable principal
├── 📁 Config/                     ← Configuraciones (93 consorcios)
├── 📁 Resources/                  ← Imágenes para personalización
│   ├── ProfileImages/             ← Fotos de perfil
│   └── Wallpapers/                ← Fondos y lockscreen
├── 📁 Software/                   ← Aplicaciones a instalar (33+)
├── 📁 Logs/                       ← Registros de ejecución
├── 📄 OptimizerMAB.ps1            ← Script de optimización
├── 📄 VERSION.txt                 ← Info de versión
└── 📄 README.md                   ← Este archivo
```

---

## 📊 Estadísticas

- **Consorcios:** 93
- **Aplicaciones:** 33+
- **Categorías:** 17
- **Formatos:** 5
- **Optimizaciones:** 7 módulos
- **Líneas de código:** ~3,500

---

## 🛡️ Seguridad

- ✅ Sin recopilación de datos
- ✅ Operaciones 100% locales
- ✅ Logs solo en disco local
- ✅ Contraseñas no registradas
- ✅ Código auditable

---

## 📝 Logs

Ubicación: `Logs/MAB_Log_YYYYMMDD_HHMMSS.txt`

Registro completo de:
- Operaciones ejecutadas
- Éxitos y fallos
- Advertencias
- Timestamps

---

## ⚠️ Importante

### Requisitos Obligatorios
- ✅ Ejecutar como **Administrador**
- ✅ Tener **.NET 8 Runtime** instalado
- ✅ Colocar software en carpeta `Software/`
- ✅ Verificar imágenes en `Resources/`

### Orden de Ejecución
**Seguir pasos en orden:** 1 → 2 → 3 → 4 → 5

El Paso 3 (Personalización) requiere que existan los usuarios del Paso 2.

---

## 🆘 Soporte

### Problemas Comunes
- **No abre:** Ejecutar como administrador
- **Falta .NET:** Instalar .NET 8 Runtime
- **Error de usuarios:** Seguir orden de pasos
- **Imágenes no aplican:** Verificar formatos soportados

### Recursos
- 📖 Consultar `GUIA_RAPIDA.md`
- 🔍 Revisar logs en `Logs/`
- 🛠️ Ver `SOLUCION_PROBLEMAS.md`

---

## 🚀 Versiones Futuras

### v1.1 (Planeado)
- Modo oscuro
- Exportar reportes
- Programar ejecución
- Modo desatendido

### v1.2 (Futuro)
- Múltiples idiomas
- Plantillas personalizadas
- Backup automático
- Integración AD

---

## 👥 Desarrollo

**Desarrollado para:** MAB Ingeniería  
**Fecha:** Noviembre 2025  
**Tecnología:** C# / .NET 8 / WPF / PowerShell  
**Arquitectura:** MVVM  

---

## 📄 Licencia

© 2025 MAB Ingeniería - Todos los derechos reservados

---

## ✅ Estado: LISTO PARA PRODUCCIÓN

**v1.0.0** - Build Final - 6 de Noviembre de 2025

🎉 **MAB APP TECNOLOGIA está lista para usarse en producción** 🎉
