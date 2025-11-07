# Changelog - MAB APP TECNOLOGIA

## [1.0.0] - 2024-11-06

### Características Iniciales

#### ✨ Funcionalidades Principales
- **Nomenclatura Automática**: Generación de nombres de equipo según consorcio y fabricante
  - Dell: Primeros 4 dígitos del serial
  - Otras marcas: Últimos 4 dígitos del serial

- **Gestión de Usuarios**:
  - Renombrado de usuario actual a ADMIN
  - Configuración de contraseña y PIN desde CSV
  - Creación de usuario MAB (estándar, sin contraseña)

- **Personalización del Sistema**:
  - Fondos de pantalla por usuario (ADMIN y MAB)
  - Pantallas de bloqueo personalizadas
  - Imágenes de perfil de Windows

- **Instalación de Software**:
  - Selección múltiple de aplicaciones
  - Instalación silenciosa automática (.exe, .msi)
  - Fallback a modo interactivo si falla instalación silenciosa
  - Barra de progreso en tiempo real

- **Optimización del Sistema**:
  - Limpieza de iconos del escritorio
  - Limpieza de barra de tareas
  - Configuraciones de rendimiento

#### 🎨 Interfaz de Usuario
- Wizard moderno de 5 pasos
- Indicador visual de progreso
- Diseño responsive y moderno
- Barra de estado con mensajes en tiempo real
- Botón "Ejecutar Todo" para automatización completa

#### 🔧 Configuración
- Archivo CSV para gestión de consorcios
- Configuración JSON para paths y recursos
- Sistema de logging completo
- Copia automática de recursos a C:\MAB-Resources

#### 📚 Documentación
- README.md completo
- Guía rápida de uso
- Instrucciones en carpetas de recursos
- Scripts de ejecución y publicación

### Tecnologías
- .NET 8 (WPF)
- C# 12
- PowerShell SDK 7.4
- CsvHelper 33.0
- Newtonsoft.Json 13.0

### Arquitectura
- Patrón MVVM
- Servicios modulares independientes
- Logging centralizado
- Gestión de errores robusta

---

## Próximas Características (Roadmap)

### [1.1.0] - Planificado
- [ ] Instalación de actualizaciones de Windows
- [ ] Configuración de políticas de grupo locales
- [ ] Backup automático antes de aplicar cambios
- [ ] Interfaz para editar CSV desde la aplicación
- [ ] Perfiles de configuración personalizables
- [ ] Soporte para múltiples idiomas

### [1.2.0] - Futuro
- [ ] Modo servidor/cliente para configuración remota
- [ ] Base de datos para tracking de equipos configurados
- [ ] Reportes de inventario
- [ ] Integración con Active Directory
- [ ] API REST para integración con otros sistemas

---

## Notas de la Versión 1.0.0

### Requisitos
- Windows 11
- .NET 8 Runtime
- Permisos de Administrador

### Limitaciones Conocidas
- Requiere reinicio para aplicar nombre de equipo
- PIN de Windows Hello requiere configuración manual post-instalación
- Algunos instaladores pueden no soportar instalación silenciosa

### Mejoras Futuras
- Detección automática de argumentos de instalación silenciosa
- Validación de archivos de recursos antes de ejecución
- Modo "dry-run" para previsualizar cambios sin aplicarlos

---

**Mantenido por:** Equipo de Tecnología MAB
**Última actualización:** 2024-11-06
