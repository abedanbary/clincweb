using System.Collections.Generic;
using ClinicApp.Web.Models;

namespace ClinicApp.Web.ViewModels
{
    public class MaterialsPageViewModel
    {
        public List<Material> Materials { get; set; } = new List<Material>();

        public Material NewMaterial { get; set; } = new Material();
    }
}
