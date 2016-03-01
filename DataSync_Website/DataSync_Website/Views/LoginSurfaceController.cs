using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Web.Security;
using DataSync_Website.Models;
using Umbraco.Web.Mvc;

namespace DataSync_Website.Views
{
    public class LoginSurfaceController : SurfaceController
    {
        private readonly RepositoryDataSync _repository = new RepositoryDataSync();

        [HttpGet]
        [ActionName("UserLogin")]
        public ActionResult UserLoginGet()
        {
            return PartialView("UserLoginPartial", new UserLoginModel());
        }

        [HttpPost]
        [ActionName("UserLogin")]
        public ActionResult UserLoginPost(UserLoginModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _repository.UserLoginCheck(model.UserName, model.UserPassword);
                if (user != null)
                {
                    var serializer = new JavaScriptSerializer();
                    var userData = serializer.Serialize(user);
                    var authTicket = new FormsAuthenticationTicket(
                        1,
                        model.UserName,
                        DateTime.Now,
                        DateTime.Now.AddMonths(1),
                        model.RememberMe, // persistant
                        userData);
                    var encTicket = FormsAuthentication.Encrypt(authTicket);
                    var faCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encTicket);
                    if (model.RememberMe) faCookie.Expires = DateTime.Now.AddYears(10);
                    Response.Cookies.Add(faCookie);

                    return Redirect("/Welcome");
                }
            }
            TempData["Status"] = "Forkert brugernavn eller password";
            return RedirectToCurrentUmbracoPage();
        }

        [HttpGet]
        public ActionResult UserLogout()
        {
            Session.Clear();
            FormsAuthentication.SignOut();
            return Redirect("/");
        }
    }
}