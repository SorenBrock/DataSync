using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;

namespace DataSync_Website.Models
{
    public class RepositoryDataSync
    {
        private static readonly DataSyncDBEntities Db = new DataSyncDBEntities();

        internal User UserLoginCheck(string userName, string userPassword)
        {
            User user = null;
            CustomerSet customer = null;
            try
            {
                customer = Db.CustomerSet.FirstOrDefault(x => x.WebLogin == userName && x.WebPassword == userPassword);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }

            if (customer != null)
                user = new User() { Id = customer.Id, Name = customer.Name, UserName = customer.WebLogin };
            return user;
        }

        internal CustomerSet GetCustomerByUser(User user)
        {
            try
            {
                var entity = Db.CustomerSet.FirstOrDefault(x => x.Id == user.Id);
                ((IObjectContextAdapter)Db).ObjectContext.Refresh(RefreshMode.StoreWins, entity);
                return entity;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                return null;
            }
        }

        internal int GetCountOfUnreadMessageInInbox(User user)
        {
            try
            {
                var entity = Db.WebSiteInboxSet.Where(x => x.CustomerId == user.Id && !x.IsRead);
                ((IObjectContextAdapter)Db).ObjectContext.Refresh(RefreshMode.StoreWins, entity);
                return entity.Count();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                return 0;
            }
        }

        internal IEnumerable<WebSiteInboxSet> GetWebSiteInboxListByUser(User user)
        {
            try
            {
                var entity = Db.WebSiteInboxSet.Where(x => x.CustomerId == user.Id);
                ((IObjectContextAdapter)Db).ObjectContext.Refresh(RefreshMode.StoreWins, entity);
                return entity;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                return null;
            }
        }

        public void UpdateWebSiteInboxIsReadByList(List<int> messageIdList, bool b)
        {
            try
            {
                Db.WebSiteInboxSet.Where(x => messageIdList.Contains(x.Id)).ToList()
                       .ForEach(a => { a.IsRead = b; });
                Db.SaveChanges();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
        }

        public void DeleteWebSiteInboxByList(List<int> messageIdList, bool b)
        {
            try
            {
                Db.WebSiteInboxSet.RemoveRange(Db.WebSiteInboxSet.Where(x => messageIdList.Contains(x.Id)));
                Db.SaveChanges();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
        }
    }
}