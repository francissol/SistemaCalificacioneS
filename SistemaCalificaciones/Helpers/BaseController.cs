using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SistemaCalificaciones.Controllers
{
    public abstract class BaseController : ControllerBase
    {
        protected int IdUsuario
        {
            get
            {
                var claim = User.FindFirst(ClaimTypes.NameIdentifier);

                return claim == null ? 0 : int.Parse(claim.Value);
            }
        }

        protected string Rol
        {
            get
            {
                return User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            }
        }

        protected int? IdCentro
        {
            get
            {
                var claim = User.FindFirst("IdCentro");

                if (claim == null)
                    return null;

                return int.Parse(claim.Value);
            }
        }

        protected bool EsAdministrador
        {
            get
            {
                return Rol == "Administrador";
            }
        }
    }
}       