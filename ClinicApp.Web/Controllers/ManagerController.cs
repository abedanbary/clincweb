using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicApp.Web.Controllers
{
    // لو حاب، نخليها لمستخدم Manager فقط. لو مش جاهز auth، احذف السطر هذا.
    [Authorize(Roles = "Manager")]
    public class ManagerController : Controller
    {
        // GET: /Manager/Index
        public IActionResult Index()
        {
            // لاحقاً نمرر موديل، الآن فقط View
            return View();
        }

        public IActionResult Materials()
         {
         return View();
         }

    }

}
