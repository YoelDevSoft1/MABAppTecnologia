# 📋 Historia de Usuario - MAB APP TECNOLOGIA

## 🎯 Historia Principal

**Como** técnico de TI de MAB Ingeniería  
**Quiero** configurar equipos de cómputo de forma automatizada y estandarizada  
**Para** reducir el tiempo de configuración, minimizar errores humanos y mantener consistencia en todos los equipos de la organización

---

## 📊 Criterios de Aceptación

### ✅ Funcionalidad Principal
- [x] La aplicación debe ejecutarse con permisos de administrador
- [x] Debe detectar automáticamente el serial y fabricante del equipo
- [x] Debe cargar 93 consorcios desde CSV
- [x] Debe detectar automáticamente 33+ aplicaciones en la carpeta Software
- [x] Debe generar logs detallados de todas las operaciones

### ✅ Paso 1: Nomenclatura
- [x] Mostrar lista de consorcios disponibles
- [x] Permitir seleccionar tipo de equipo (Propio, Alquiler, Home Office)
- [x] Generar nombre de equipo automáticamente según nomenclatura
- [x] Renombrar el equipo con el nombre generado
- [x] Validar que se seleccionó un consorcio antes de continuar

### ✅ Paso 2: Gestión de Usuarios
- [x] Renombrar usuario actual a ADMIN
- [x] Configurar contraseña y PIN de ADMIN desde CSV
- [x] Crear usuario MAB (estándar, sin contraseña)
- [x] Validar que los usuarios se crearon correctamente

### ✅ Paso 3: Personalización
- [x] Aplicar fondo de pantalla para ADMIN y MAB
- [x] Aplicar pantalla de bloqueo personalizada
- [x] Configurar imágenes de perfil de Windows
- [x] Soportar múltiples formatos de imagen (PNG, JPG, BMP, GIF)
- [x] Copiar recursos a C:\MAB-Resources automáticamente

### ✅ Paso 4: Instalación de Software
- [x] Mostrar lista de aplicaciones detectadas automáticamente
- [x] Permitir selección múltiple de software
- [x] Instalar aplicaciones en modo silencioso cuando sea posible
- [x] Fallback a modo interactivo si falla instalación silenciosa
- [x] Mostrar progreso de instalación en tiempo real
- [x] Categorizar software automáticamente (17 categorías)

### ✅ Paso 5: Optimización
- [x] Ofrecer 7 módulos de optimización configurables
- [x] Limpiar iconos del escritorio
- [x] Limpiar y configurar barra de tareas para ADMIN y MAB
  - [x] Eliminar todos los iconos existentes de la barra de tareas
  - [x] Añadir iconos predefinidos para ADMIN (método directo COM)
  - [x] Crear Scheduled Task para MAB (ejecución al login)
  - [x] Manejar limitaciones de Windows 11 (pinning no funciona vía .lnk)
  - [x] Logging detallado del proceso de pinning
- [x] Aplicar optimizaciones de privacidad
- [x] Aplicar optimizaciones de rendimiento
- [x] Deshabilitar servicios de telemetría
- [x] Optimizar experiencia de usuario
- [x] Opción de eliminar bloatware
- [x] Optimizar inicio del sistema
- [x] Limpiar archivos temporales
- [x] Ejecutar OptimizerMAB.ps1 avanzado (opcional)

---

## 👤 Personas y Roles

### Persona Principal: Técnico de TI
- **Rol**: Configurador de equipos
- **Conocimientos**: Intermedio en Windows, básico en administración de sistemas
- **Necesidades**: 
  - Configurar equipos rápidamente
  - Estandarizar configuraciones
  - Reducir errores manuales
  - Mantener trazabilidad de cambios

### Persona Secundaria: Administrador de TI
- **Rol**: Supervisor y gestor de configuraciones
- **Conocimientos**: Avanzado en Windows, gestión de usuarios, políticas
- **Necesidades**:
  - Gestionar múltiples consorcios
  - Controlar nomenclatura de equipos
  - Personalización corporativa
  - Logs detallados para auditoría

---

## 📝 Escenarios de Uso

### Escenario 1: Configuración Completa de Equipo Nuevo
1. Técnico ejecuta la aplicación como administrador
2. Selecciona consorcio y tipo de equipo
3. Ejecuta "Ejecutar Todo" para automatizar los 5 pasos
4. La aplicación configura el equipo completamente
5. Técnico reinicia el equipo y entrega al usuario final

**Tiempo estimado**: 15-30 minutos (dependiendo del software seleccionado)

### Escenario 2: Configuración Paso a Paso
1. Técnico ejecuta la aplicación
2. Completa Paso 1 (Nomenclatura) y ejecuta
3. Completa Paso 2 (Usuarios) y ejecuta
4. Completa Paso 3 (Personalización) y ejecuta
5. Selecciona software específico en Paso 4 y ejecuta
6. Aplica optimizaciones selectivas en Paso 5
7. Reinicia el equipo

**Tiempo estimado**: 20-45 minutos

### Escenario 3: Solo Instalación de Software
1. Técnico ejecuta la aplicación
2. Navega directamente al Paso 4
3. Selecciona aplicaciones necesarias
4. Ejecuta instalación
5. Cierra la aplicación sin completar otros pasos

**Tiempo estimado**: 10-20 minutos

### Escenario 4: Solo Optimización
1. Técnico ejecuta la aplicación
2. Navega directamente al Paso 5
3. Selecciona módulos de optimización deseados
4. Ejecuta optimizaciones
5. Cierra la aplicación

**Tiempo estimado**: 5-15 minutos

### Escenario 5: Reconfiguración de Barra de Tareas
1. Técnico identifica que los iconos de la barra de tareas no se aplicaron correctamente
2. Ejecuta la aplicación como administrador
3. Navega al Paso 5 (Optimización)
4. Ejecuta solo la configuración de barra de tareas
5. Para usuario MAB: Verifica que la Scheduled Task se creó correctamente
6. Usuario MAB hace logout/login para ejecutar la tarea programada
7. Técnico verifica en logs el resultado del pinning

**Tiempo estimado**: 5-10 minutos + tiempo de login de MAB

**Troubleshooting**:
- Revisar logs en C:\MABAppTecnologia\Logs para ver DEBUG de apps encontradas
- Verificar que Scheduled Task existe: `schtasks /query /tn "MAB_PinTaskbarApps"`
- Verificar que el script PowerShell existe en C:\MAB-Resources\Pin-TaskbarApps-MAB.ps1
- En Windows 11, el método COM puede reportar éxito pero no aplicar cambios

---

## 🎯 Valor de Negocio

### Beneficios Cuantificables
- **Reducción de tiempo**: De 2-3 horas a 15-30 minutos por equipo
- **Reducción de errores**: 95% menos errores de configuración manual
- **Estandarización**: 100% de equipos con misma configuración base
- **Trazabilidad**: Logs completos de todas las operaciones

### Beneficios Cualitativos
- Mayor satisfacción del técnico (menos trabajo repetitivo)
- Mayor satisfacción del usuario final (equipos listos más rápido)
- Mejor imagen corporativa (personalización consistente)
- Facilita auditorías y cumplimiento

---

## 🔒 Requisitos Técnicos

### Requisitos Previos
- Windows 10/11 (64-bit)
- .NET 8 Runtime instalado
- Permisos de Administrador (obligatorio)
- Acceso a carpeta Software con aplicaciones

### Restricciones
- Debe ejecutarse como administrador
- Requiere reinicio para aplicar nombre de equipo
- Algunos instaladores pueden no soportar modo silencioso
- PIN de Windows Hello requiere configuración manual post-instalación

### Consideraciones Técnicas de Windows 11
- **Barra de Tareas**: Windows 11 no permite pinning directo mediante copia de archivos .lnk
  - Solución: Uso de COM Shell.Application para usuario actual
  - Solución: Scheduled Tasks para otros usuarios (ejecuta al login)
- **Método COM**: Reporta éxito pero puede no aplicar cambios realmente en Windows 11
  - Solución: Logging detallado para debugging
  - Solución: Scheduled Task como método alternativo más confiable
- **Permisos**: Operaciones en otros usuarios requieren elevación y contexto correcto
  - Solución: Scripts PowerShell ejecutados como el usuario destino

---

## 📈 Métricas de Éxito

### KPIs Principales
- **Tiempo promedio de configuración**: < 30 minutos
- **Tasa de éxito de instalaciones**: > 90%
- **Satisfacción del técnico**: > 4/5
- **Reducción de tickets de soporte**: > 50%

### Métricas Técnicas
- Tiempo de carga inicial: < 5 segundos
- Tiempo de detección de software: < 10 segundos
- Tiempo de instalación promedio por aplicación: < 2 minutos
- Tasa de errores en logs: < 5%

---

## 🚀 Priorización

### Must Have (P0)
- ✅ Nomenclatura automática
- ✅ Gestión de usuarios
- ✅ Instalación de software
- ✅ Logs detallados

### Should Have (P1)
- ✅ Personalización del sistema
- ✅ Optimizaciones básicas
- ✅ Interfaz intuitiva

### Nice to Have (P2)
- ⏳ Modo desatendido
- ⏳ Exportar reportes
- ⏳ Programar ejecución
- ⏳ Integración con Active Directory (MAB no cuenta con AD)

---

## 📚 Referencias

- [README.md](README.md) - Documentación principal
- [GUIA_RAPIDA.md](GUIA_RAPIDA.md) - Guía de uso
- [DIAGRAMA_FLUJO.md](DIAGRAMA_FLUJO.md) - Diagramas de flujo completos
- [CHANGELOG.md](CHANGELOG.md) - Historial de cambios
- [SOLUCION_PROBLEMAS.md](SOLUCION_PROBLEMAS.md) - Troubleshooting

---

**Versión**: 1.2.0
**Última actualización**: Noviembre 2025
**Autor**: Equipo de Tecnología MAB
**Cambios recientes**:
- Añadido escenario de reconfiguración de barra de tareas
- Documentadas limitaciones de Windows 11
- Añadidas consideraciones técnicas para taskbar pinning

