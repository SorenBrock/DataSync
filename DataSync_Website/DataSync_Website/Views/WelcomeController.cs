using System.Web.Mvc;
using DataSync_Website.Models;
using Umbraco.Web.Mvc;

namespace DataSync_Website.Views
{
    public class WelcomeController : RenderMvcController
    {
        private readonly RepositoryDataSync _repository = new RepositoryDataSync();

        public ActionResult Index()
        {
            if (!User.Identity.IsAuthenticated)
                return Redirect("/Login");

            return CurrentTemplate(new UserViewModel());
        }
    }
}