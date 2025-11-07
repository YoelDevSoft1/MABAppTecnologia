# 🖥️ MAB APP TECNOLOGIA

<div align="center">

![.NET Version](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![WPF](https://img.shields.io/badge/WPF-0078D4?style=for-the-badge&logo=windows&logoColor=white)
![Windows](https://img.shields.io/badge/Windows-10%2F11-0078D4?style=for-the-badge&logo=windows&logoColor=white)
![License](https://img.shields.io/badge/License-Proprietary-red?style=for-the-badge)
![Version](https://img.shields.io/badge/Version-1.0.0-blue?style=for-the-badge)

**Aplicación profesional de configuración automatizada de equipos de cómputo**

*Reduce el tiempo de configuración de equipos de 2-3 horas a solo 15-30 minutos*

[Características](#-características-principales) • [Instalación](#-instalación) • [Uso](#-uso-rápido) • [Documentación](#-documentación) • [Contribuir](#-contribuir)

</div>

---

## 📋 Tabla de Contenidos

- [Descripción](#-descripción)
- [Características Principales](#-características-principales)
- [Capturas de Pantalla](#-capturas-de-pantalla)
- [Requisitos](#-requisitos)
- [Instalación](#-instalación)
- [Uso Rápido](#-uso-rápido)
- [Arquitectura](#-arquitectura)
- [Tecnologías](#-tecnologías)
- [Estructura del Proyecto](#-estructura-del-proyecto)
- [Documentación](#-documentación)
- [Contribuir](#-contribuir)
- [Licencia](#-licencia)

---

## 🎯 Descripción

**MAB APP TECNOLOGIA** es una aplicación de escritorio desarrollada en WPF (.NET 8) que automatiza la configuración completa de equipos de cómputo para MAB Ingeniería. La herramienta permite configurar nomenclatura, usuarios, personalización, instalación de software y optimización del sistema en un proceso guiado de 5 pasos.

### ✨ ¿Por qué usar MAB APP TECNOLOGIA?

- ⚡ **Ahorro de tiempo**: Reduce el tiempo de configuración de 2-3 horas a 15-30 minutos
- 🎯 **Estandarización**: Garantiza configuraciones consistentes en todos los equipos
- 🔒 **Seguridad**: Operaciones 100% locales, sin recopilación de datos
- 📊 **Trazabilidad**: Logs detallados de todas las operaciones
- 🛠️ **Flexibilidad**: Permite ejecutar pasos individuales o proceso completo

---

## 🌟 Características Principales

### 1. 📝 Nomenclatura Inteligente
- Generación automática de nombres según consorcio y tipo de equipo
- Soporte para 93 consorcios configurados
- Tipos: Propio, Alquiler, Home Office
- Detección automática de serial y fabricante

### 2. 👥 Gestión de Usuarios
- Renombrado automático del usuario actual a ADMIN
- Configuración de contraseña y PIN desde CSV
- Creación de usuario MAB estándar
- Validación de operaciones

### 3. 🎨 Personalización del Sistema
- Fondos de pantalla personalizados por usuario
- Pantallas de bloqueo personalizadas
- Imágenes de perfil de Windows
- Soporte para múltiples formatos (PNG, JPG, BMP, GIF)

### 4. 💿 Instalación de Software
- Detección automática de 33+ aplicaciones
- Instalación silenciosa cuando es posible
- Fallback a modo interactivo
- Progreso en tiempo real
- Categorización automática (17 categorías)

### 5. ⚡ Optimización del Sistema
- 7 módulos de optimización configurables
- Limpieza de escritorio y barra de tareas
- Optimizaciones de privacidad y rendimiento
- Deshabilitación de telemetría
- Script avanzado OptimizerMAB.ps1 integrado

---

## 📸 Capturas de Pantalla

<div align="center">

### Interfaz Principal

![Interfaz Principal](https://via.placeholder.com/800x500/0078D4/FFFFFF?text=MAB+APP+TECNOLOGIA+Interface)

*Vista general de la aplicación con los 5 pasos de configuración*

### Paso 1: Nomenclatura

![Nomenclatura](https://via.placeholder.com/800x500/28A745/FFFFFF?text=Paso+1%3A+Nomenclatura)

*Selección de consorcio y tipo de equipo*

### Paso 4: Instalación de Software

![Software](https://via.placeholder.com/800x500/FFC107/000000?text=Paso+4%3A+Instalaci%C3%B3n+de+Software)

*Lista de aplicaciones detectadas automáticamente*

</div>

> 💡 **Nota**: Las capturas de pantalla son placeholders. Reemplázalas con imágenes reales de la aplicación.

---

## 📋 Requisitos

### Requisitos del Sistema
- **OS**: Windows 10/11 (64-bit)
- **.NET Runtime**: .NET 8.0 o superior
- **Permisos**: Administrador (obligatorio)
- **RAM**: Mínimo 4 GB recomendado
- **Espacio en disco**: ~500 MB para la aplicación + espacio para software a instalar

### Requisitos Previos
- Acceso a carpeta `Software/` con aplicaciones a instalar
- Archivos de recursos en `Resources/` (opcional)
- Archivo `Config/consorcios.csv` con consorcios configurados

---

## 🚀 Instalación

### Opción 1: Descargar Release

1. Ve a la sección [Releases](https://github.com/tu-usuario/MAB-EQUIPOS/releases)
2. Descarga la última versión (`MABAppTecnologia-v1.0.0.zip`)
3. Extrae el archivo ZIP
4. Asegúrate de tener .NET 8 Runtime instalado
5. Ejecuta `MABAppTecnologia.exe` como administrador

### Opción 2: Compilar desde Código

```bash
# Clonar el repositorio
git clone https://github.com/tu-usuario/MAB-EQUIPOS.git
cd MAB-EQUIPOS/MABAppTecnologia

# Restaurar dependencias
dotnet restore

# Compilar en modo Release
dotnet build -c Release

# El ejecutable estará en:
# bin/Release/net8.0-windows/MABAppTecnologia.exe
```

### Verificar Instalación

```powershell
# Verificar que .NET 8 está instalado
dotnet --version

# Debe mostrar: 8.0.x o superior
```

---

## 💻 Uso Rápido

### Ejecución Básica

1. **Ejecutar como Administrador**
   ```powershell
   # Clic derecho en MABAppTecnologia.exe
   # → "Ejecutar como administrador"
   ```

2. **Seguir los 5 Pasos**
   - **Paso 1**: Seleccionar consorcio y tipo de equipo → Ejecutar
   - **Paso 2**: Configurar usuarios → Ejecutar
   - **Paso 3**: Aplicar personalización → Ejecutar
   - **Paso 4**: Seleccionar software → Ejecutar
   - **Paso 5**: Seleccionar optimizaciones → Ejecutar

3. **O Ejecutar Todo**
   - Clic en **"Ejecutar Todo"** para automatizar los 5 pasos

### Ejemplo de Uso

```powershell
# 1. Ejecutar la aplicación como administrador
.\MABAppTecnologia.exe

# 2. La aplicación cargará automáticamente:
#    - Consorcios desde Config/consorcios.csv
#    - Software desde carpeta Software/
#    - Información del equipo (serial, fabricante)

# 3. Seleccionar consorcio y tipo de equipo
# 4. Clic en "Ejecutar Todo" o ejecutar paso por paso
# 5. Esperar a que complete la configuración
# 6. Reiniciar el equipo (recomendado)
```

### Nomenclatura de Equipos

La aplicación genera nombres automáticamente según el tipo:

```
Equipo Propio:      SIGLAS-XXXX
Equipo Alquiler:    SIGLAS-RUB-XXXX
Equipo Home Office: SIGLAS-HOME-XXXX
```

Donde:
- `SIGLAS`: Siglas del consorcio seleccionado
- `XXXX`: 4 dígitos del serial (primeros para Dell, últimos para otros)

---

## 🏗️ Arquitectura

El proyecto sigue el patrón **MVVM** (Model-View-ViewModel) con una arquitectura modular:

```
┌─────────────────────────────────────────┐
│           MainWindow (View)             │
│         (Interfaz de Usuario)           │
└─────────────────┬───────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│        MainViewModel (ViewModel)         │
│      (Lógica de Presentación)           │
└─────────────────┬───────────────────────┘
                  │
      ┌───────────┼───────────┐
      │           │           │
┌─────▼─────┐ ┌──▼───┐ ┌─────▼─────┐
│ Services  │ │Models│ │  Helpers  │
│ (Lógica)  │ │(Data)│ │ (Utils)   │
└───────────┘ └──────┘ └───────────┘
```

### Servicios Principales

- **ConfigService**: Gestión de configuraciones y CSV
- **SystemService**: Operaciones del sistema (renombrar, optimizar)
- **UserService**: Gestión de usuarios Windows
- **PersonalizationService**: Personalización del sistema
- **SoftwareService**: Detección e instalación de software
- **LogService**: Sistema de logging centralizado

---

## 🛠️ Tecnologías

### Framework y Lenguaje
- **.NET 8.0** - Framework principal
- **C# 12** - Lenguaje de programación
- **WPF** - Interfaz de usuario

### Librerías Principales
- **CsvHelper 33.0.1** - Lectura de archivos CSV
- **Microsoft.PowerShell.SDK 7.4.6** - Ejecución de scripts PowerShell
- **Newtonsoft.Json 13.0.3** - Serialización JSON
- **System.Drawing.Common 9.0.0** - Procesamiento de imágenes

### Patrones y Principios
- **MVVM** - Separación de responsabilidades
- **Dependency Injection** - Inyección de dependencias
- **Repository Pattern** - Acceso a datos
- **Command Pattern** - Comandos de UI

---

## 📁 Estructura del Proyecto

```
MABAppTecnologia/
├── 📁 Config/                    # Configuraciones
│   ├── consorcios.csv           # 93 consorcios configurados
│   ├── settings.json            # Configuración de la app
│   └── Nomenclatura Equipos.csv # Reglas de nomenclatura
│
├── 📁 Resources/                 # Recursos de la aplicación
│   ├── ProfileImages/           # Imágenes de perfil
│   └── Wallpapers/              # Fondos y lockscreen
│
├── 📁 Software/                  # Aplicaciones a instalar
│   ├── ANTIVIRUS/              # Software de antivirus
│   ├── OFFICE/                  # Suite de Office
│   ├── Navegadores/             # Navegadores web
│   └── ...                      # 17 categorías más
│
├── 📁 Services/                  # Servicios de la aplicación
│   ├── ConfigService.cs
│   ├── SystemService.cs
│   ├── UserService.cs
│   ├── PersonalizationService.cs
│   ├── SoftwareService.cs
│   └── LogService.cs
│
├── 📁 Models/                    # Modelos de datos
│   ├── AppConfig.cs
│   ├── ConsorcioConfig.cs
│   ├── SoftwareItem.cs
│   └── OperationResult.cs
│
├── 📁 ViewModels/                # ViewModels MVVM
│   ├── MainViewModel.cs
│   └── ViewModelBase.cs
│
├── 📁 Helpers/                   # Utilidades
│   ├── RelayCommand.cs
│   └── StepColorConverter.cs
│
├── 📁 Guias/                     # Documentación
│   ├── README.md
│   ├── GUIA_RAPIDA.md
│   ├── HISTORIA_USUARIO.md
│   ├── DIAGRAMA_FLUJO.md
│   └── ...                      # Más documentación
│
├── 📁 Logs/                      # Logs de ejecución
│
├── 📄 MABAppTecnologia.csproj    # Archivo del proyecto
├── 📄 MainWindow.xaml            # Interfaz principal
├── 📄 OptimizerMAB.ps1          # Script de optimización
└── 📄 README.md                  # Este archivo
```

---

## 📚 Documentación

### Documentación Principal

| Documento | Descripción |
|-----------|-------------|
| [📖 Guía Rápida](Guias/GUIA_RAPIDA.md) | Guía de uso paso a paso |
| [📋 Historia de Usuario](Guias/HISTORIA_USUARIO.md) | Requisitos y casos de uso |
| [🔄 Diagrama de Flujo](Guias/DIAGRAMA_FLUJO.md) | Flujos de la aplicación |
| [📝 Changelog](Guias/CHANGELOG.md) | Historial de versiones |

### Guías Específicas

- [🎨 Personalización](Guias/COMO_USAR_PERSONALIZACION.md) - Configurar fondos e imágenes
- [🖼️ Formatos de Imagen](Guias/FORMATOS_IMAGEN_FLEXIBLES.md) - Formatos soportados
- [⚡ Optimizaciones](Guias/OPTIMIZACIONES.md) - Módulos de optimización
- [🛠️ Solución de Problemas](Guias/SOLUCION_PROBLEMAS.md) - Troubleshooting

---

## 🤝 Contribuir

Las contribuciones son bienvenidas. Por favor:

1. **Fork** el proyecto
2. Crea una **rama** para tu feature (`git checkout -b feature/AmazingFeature`)
3. **Commit** tus cambios (`git commit -m 'Add some AmazingFeature'`)
4. **Push** a la rama (`git push origin feature/AmazingFeature`)
5. Abre un **Pull Request**

### Guías de Contribución

- Sigue las convenciones de código existentes
- Agrega documentación para nuevas características
- Incluye tests cuando sea posible
- Actualiza el CHANGELOG.md

### Reportar Issues

Si encuentras un bug o tienes una sugerencia:

1. Verifica que el issue no exista ya
2. Crea un nuevo issue con:
   - Descripción clara del problema
   - Pasos para reproducir
   - Logs relevantes (si aplica)
   - Versión de la aplicación

---

## 📊 Estadísticas del Proyecto

<div align="center">

| Métrica | Valor |
|---------|-------|
| **Consorcios Configurados** | 93 |
| **Aplicaciones Detectadas** | 33+ |
| **Categorías de Software** | 17 |
| **Módulos de Optimización** | 7 |
| **Líneas de Código** | ~3,500 |
| **Servicios** | 6 |
| **Formatos de Imagen** | 5 |

</div>

---

## 🔒 Seguridad y Privacidad

- ✅ **100% Local**: Todas las operaciones se ejecutan localmente
- ✅ **Sin Telemetría**: No se recopila información del usuario
- ✅ **Logs Locales**: Los logs solo se guardan en disco local
- ✅ **Sin Datos Sensibles**: Las contraseñas no se registran en logs
- ✅ **Código Abierto**: Código auditable y transparente

---

## ⚠️ Advertencias

- **Permisos de Administrador**: La aplicación **DEBE** ejecutarse como administrador
- **Reinicio Requerido**: Algunos cambios requieren reinicio del equipo
- **Backup Recomendado**: Se recomienda crear punto de restauración antes de optimizaciones avanzadas
- **Software Local**: Los instaladores deben estar en la carpeta `Software/`

---

## 📝 Licencia

Este proyecto es **propietario** y está desarrollado exclusivamente para **MAB Ingeniería**.

© 2025 MAB Ingeniería - Todos los derechos reservados

---

## 👥 Autores

**Equipo de Tecnología MAB**

- Desarrollo y Mantenimiento
- Documentación
- Soporte Técnico

---

## 🙏 Agradecimientos

- MAB Ingeniería por el apoyo y recursos
- Comunidad .NET por las herramientas y documentación
- Contribuidores y testers del proyecto

---

## 📞 Contacto y Soporte

Para soporte técnico o consultas:

- 📧 Email: tecnologia@mabingenieria.com
- 📖 Documentación: Ver carpeta [Guias/](Guias/)
- 🐛 Issues: [GitHub Issues](https://github.com/tu-usuario/MAB-EQUIPOS/issues)

---

<div align="center">

### ⭐ Si este proyecto te resulta útil, considera darle una estrella ⭐

**Versión 1.0.0** - Build Final - Noviembre 2025

![Status](https://img.shields.io/badge/Status-Production%20Ready-success?style=for-the-badge)

</div>

