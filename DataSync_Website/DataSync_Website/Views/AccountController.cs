using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Web.Security;
using DataSync_Website.Models;
using Umbraco.Web.Mvc;

namespace DataSync_Website.Views
{
    public class AccountController : RenderMvcController
    {
        private readonly RepositoryDataSync _repository = new RepositoryDataSync();

        public ActionResult Index()
        {
                if (!User.Identity.IsAuthenticated) 
                    return Redirect("/Login");

                var authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
                if (authCookie == null) return Redirect("/");
                var ticket = FormsAuthentication.Decrypt(authCookie.Value);
                if (ticket == null) return Redirect("/");
                var serializer = new JavaScriptSerializer();
                var user = serializer.Deserialize<User>(ticket.UserData);

                return user != null
                    ? CurrentTemplate(new UserViewModel() { CustomerSet = _repository.GetCustomerByUser(user) })
                    : Redirect("/Login");
        }
    }
}