using System.Collections.Generic;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace DataSync_Website.Models
{
    public class UserViewModel : RenderModel
    {
        public UserViewModel() : base(UmbracoContext.Current.PublishedContentRequest.PublishedContent) { }

        public CustomerSet CustomerSet { get; set; }
        public IEnumerable<WebSiteInboxSet> WebSiteInboxList { get; set; }
    }
}