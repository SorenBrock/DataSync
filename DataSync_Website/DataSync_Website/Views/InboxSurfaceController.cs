using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Web.Security;
using DataSync_Website.Models;
using Umbraco.Web.Mvc;

namespace DataSync_Website.Views
{
    public class InboxSurfaceController : SurfaceController
    {
        private readonly RepositoryDataSync _repository = new RepositoryDataSync();

        [HttpGet]
        [ActionName("WebsiteInboxMessageCount")]
        public ActionResult WebsiteInboxMessageCount()
        {
            var modelResult = 0;
            if (User.Identity.IsAuthenticated)
            {
                modelResult = 0;
                var authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
                if (authCookie != null)
                {
                    var ticket = FormsAuthentication.Decrypt(authCookie.Value);
                    if (ticket != null)
                    {
                        var serializer = new JavaScriptSerializer();
                        var user = serializer.Deserialize<User>(ticket.UserData);
                        modelResult = _repository.GetCountOfUnreadMessageInInbox(user);
                    }
                }
            }
            return PartialView("WebsiteInboxCountPartial", modelResult);
        }

        [HttpGet]
        [ActionName("WebsiteInbox")]
        public ActionResult WebsiteInbox()
        {
            if (!User.Identity.IsAuthenticated)
                return Redirect("/Login");

            var authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie == null) return Redirect("/");
            var ticket = FormsAuthentication.Decrypt(authCookie.Value);
            if (ticket == null) return Redirect("/");
            var serializer = new JavaScriptSerializer();
            var user = serializer.Deserialize<User>(ticket.UserData);

            var userViewModel = new UserViewModel()
            {
                WebSiteInboxList = _repository.GetWebSiteInboxListByUser(user)
            };

            return PartialView("WebsiteInboxPartial", userViewModel);
        }

        [HttpPost]
        [ActionName("WebsiteInbox")]
        public ActionResult WebsiteInbox(FormCollection formData)
        {
            var messageIdList = new List<int>();
            var messageIds = formData["messageId"].Split(',');
            var selectedIndices = formData["messageCheckBox"].Replace("true,false", "true")
                        .Split(',')
                        .Select((item, index) => new { item = item, index = index })
                        .Where(row => row.item == "true")
                        .Select(row => row.index).ToArray();

            foreach (var index in selectedIndices)
            {
                int messageId;
                if (int.TryParse(messageIds[index], out messageId))
                    messageIdList.Add(messageId);
            }
            if (messageIdList.Count != 0)
            {
                var submitButton = formData["SubmitParameter"];
                switch (submitButton)
                {
                    case "markasread":
                        _repository.UpdateWebSiteInboxIsReadByList(messageIdList, true);
                        break;
                    case "markasunread":
                        _repository.UpdateWebSiteInboxIsReadByList(messageIdList, false);
                        break;
                    case "delmessage":
                        _repository.DeleteWebSiteInboxByList(messageIdList, false);
                        break;
                    default:
                        break;
                }
            }
            return RedirectToCurrentUmbracoPage();
        }
    }
}