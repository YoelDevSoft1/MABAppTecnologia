using System.Management.Automation;
using MABAppTecnologia.Models;

namespace MABAppTecnologia.Services
{
    public class UserService
    {
        private readonly LogService _logService;

        public UserService(LogService logService)
        {
            _logService = logService;
        }

        public OperationResult ConfigureAdminUser(string newPassword, string pin)
        {
            try
            {
                _logService.LogInfo("Configurando usuario ADMIN...");

                using var ps = PowerShell.Create();

                // Obtener el usuario actual
                ps.AddScript("$env:USERNAME");
                var currentUser = ps.Invoke()[0].ToString();

                // Renombrar usuario actual a ADMIN si no lo es
                if (currentUser.ToUpper() != "ADMIN")
                {
                    _logService.LogInfo($"Renombrando usuario {currentUser} a ADMIN");

                    ps.Commands.Clear();
                    ps.AddScript($@"
                        $user = Get-LocalUser -Name '{currentUser}'
                        Rename-LocalUser -Name '{currentUser}' -NewName 'ADMIN'
                    ");
                    ps.Invoke();

                    if (ps.HadErrors)
                    {
                        var errors = string.Join(", ", ps.Streams.Error.Select(e => e.ToString()));
                        return OperationResult.Fail("Error al renombrar usuario a ADMIN", errors);
                    }
                }

                // Cambiar contraseña del usuario ADMIN
                ps.Commands.Clear();
                ps.AddScript($@"
                    $Password = ConvertTo-SecureString '{newPassword}' -AsPlainText -Force
                    Set-LocalUser -Name 'ADMIN' -Password $Password
                ");
                ps.Invoke();

                if (ps.HadErrors)
                {
                    var errors = string.Join(", ", ps.Streams.Error.Select(e => e.ToString()));
                    return OperationResult.Fail("Error al cambiar contraseña de ADMIN", errors);
                }

                // Configurar PIN (esto requiere interacción del usuario o Group Policy)
                _logService.LogInfo($"PIN para ADMIN configurado: {pin} (debe configurarse manualmente en Windows Hello)");

                _logService.LogSuccess("Usuario ADMIN configurado correctamente");
                return OperationResult.Ok("Usuario ADMIN configurado exitosamente");
            }
            catch (Exception ex)
            {
                _logService.LogError("Error al configurar usuario ADMIN", ex);
                return OperationResult.Fail("Error al configurar usuario ADMIN", ex.Message, ex);
            }
        }

        public OperationResult CreateMABUser()
        {
            try
            {
                _logService.LogInfo("Creando usuario MAB...");

                using var ps = PowerShell.Create();

                // Verificar si el usuario MAB ya existe
                ps.AddScript("Get-LocalUser -Name 'MAB' -ErrorAction SilentlyContinue");
                var existingUser = ps.Invoke();

                if (existingUser.Count > 0)
                {
                    _logService.LogInfo("Usuario MAB ya existe");
                    return OperationResult.Ok("Usuario MAB ya existe");
                }

                // Crear usuario MAB sin contraseña usando net user (permite contraseñas vacías)
                ps.Commands.Clear();
                ps.AddScript(@"
                    # Crear usuario con net user (permite contraseña vacía)
                    net user MAB """" /add /fullname:""Usuario MAB"" /comment:""Usuario estándar para colaboradores"" /passwordchg:no /expires:never
                    
                    # Configurar que la contraseña nunca expira
                    wmic useraccount where ""name='MAB'"" set PasswordExpires=false
                    
                    # Agregar al grupo Users
                    net localgroup ""Usuarios"" MAB /add 2>$null
                    net localgroup ""Users"" MAB /add 2>$null
                ");
                ps.Invoke();

                if (ps.HadErrors)
                {
                    var errors = string.Join(", ", ps.Streams.Error.Select(e => e.ToString()));
                    _logService.LogWarning($"Advertencias al crear usuario MAB: {errors}");
                    // No fallar por errores menores (como agregar a grupo que ya pertenece)
                }

                // Verificar que el usuario se creó correctamente
                ps.Commands.Clear();
                ps.AddScript("Get-LocalUser -Name 'MAB' -ErrorAction SilentlyContinue");
                var verifyUser = ps.Invoke();

                if (verifyUser.Count == 0)
                {
                    return OperationResult.Fail("El usuario MAB no se creó correctamente");
                }

                _logService.LogSuccess("Usuario MAB creado exitosamente sin contraseña");
                return OperationResult.Ok("Usuario MAB creado sin contraseña");
            }
            catch (Exception ex)
            {
                _logService.LogError("Error al crear usuario MAB", ex);
                return OperationResult.Fail("Error al crear usuario MAB", ex.Message, ex);
            }
        }

        public OperationResult RemovePasswordRequirement(string username)
        {
            try
            {
                _logService.LogInfo($"Removiendo requerimiento de contraseña para {username}...");

                using var ps = PowerShell.Create();
                ps.AddScript($@"
                    Set-LocalUser -Name '{username}' -PasswordNeverExpires $true -UserMayNotChangePassword $true
                ");
                ps.Invoke();

                if (ps.HadErrors)
                {
                    var errors = string.Join(", ", ps.Streams.Error.Select(e => e.ToString()));
                    return OperationResult.Fail($"Error al configurar usuario {username}", errors);
                }

                _logService.LogSuccess($"Configuración aplicada para {username}");
                return OperationResult.Ok($"Configuración aplicada para {username}");
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error al configurar usuario {username}", ex);
                return OperationResult.Fail($"Error al configurar usuario {username}", ex.Message, ex);
            }
        }
    }
}
