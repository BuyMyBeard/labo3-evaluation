using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;

namespace MDB.Models
{
    public class GeoLocation
    {
        [JsonIgnore]
        public const string ServiceUrl = "http://ip-api.com/json/";

        public string query;
        public string status;
        public string continent;
        public string continentCode;
        public string country;
        public string countryCode;
        public string region;
        public string regionName;
        public string city;
        public string district;
        public string zip;
        public double lat;
        public double lon;
        public string timezone;
        public int offset;
        public string currency;
        public string isp;
        public string org;
        public string asname;
        public bool mobile;
        public bool proxy;
        public bool hosting;

        public static GeoLocation Call(string IP_Address)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(ServiceUrl + IP_Address);
            request.Method = "GET";
            try
            {
                WebResponse webResponse = request.GetResponse();
                using (Stream webStream = webResponse.GetResponseStream() ?? Stream.Null)
                using (StreamReader responseReader = new StreamReader(webStream))
                {
                    string response = responseReader.ReadToEnd();
                    return JsonConvert.DeserializeObject<GeoLocation>(response);
                }
            }
            catch (Exception) { /* todo */ }
            return null;
        }
    }

    public static class OnlineUsers
    {
        #region private members
        private class UserLastAccess
        {
            public int UserId { get; set; }
            public DateTime LastAccess { get; set; }
            public UserLastAccess(int userId)
            {
                UserId = userId;
                LastAccess = DateTime.Now;
            }
        }
        private static readonly int TimeOut = Int32.Parse(WebConfigurationManager.AppSettings["SessionTimeout"]); // minutes
        private static List<UserLastAccess> LastUsersAccess
        {
            get
            {
                if (HttpRuntime.Cache["LastUsersAccess"] == null)
                    HttpRuntime.Cache["LastUsersAccess"] = new List<UserLastAccess>();
                return (List<UserLastAccess>)HttpRuntime.Cache["LastUsersAccess"];
            }
        }
        private static DateTime LastUserAccess(int userId)
        {
            foreach (UserLastAccess userAccess in LastUsersAccess)
            {
                if (userAccess.UserId == userId)
                {
                    return userAccess.LastAccess;
                }
            }
            return new DateTime(0);
        }
        private static bool SetUserAccessTime(int userId)
        {
            if (IsOnLine(userId))
            {
                foreach (UserLastAccess userAccess in LastUsersAccess)
                {
                    if (userAccess.UserId == userId)
                    {
                        userAccess.LastAccess = DateTime.Now;
                        return true;
                    }
                }
                LastUsersAccess.Add(new UserLastAccess(userId));
                return true;
            }
            return false;
        }
        private static void RemoveLastAccess(int userId)
        {
            UserLastAccess lastUserAccess = LastUsersAccess.Where(l => l.UserId == userId).FirstOrDefault();
            if (lastUserAccess != null)
                LastUsersAccess.Remove(lastUserAccess);
        }
        private static int CurrentUserId
        {
            get
            {
                try
                {
                    if (HttpContext.Current.Session["UserId"] != null)
                        return (int)HttpContext.Current.Session["UserId"];
                    return 0;
                }
                catch { return 0; }
            }
            set
            {
                if (value != 0)
                {
                    HttpContext.Current.Session.Timeout = 60;
                    HttpContext.Current.Session["UserId"] = value;
                }
                else
                {
                    if (HttpContext.Current != null)
                        HttpContext.Current.Session.Abandon();
                }
            }
        }
        private static string SerialNumber
        {
            get
            {
                if (HttpRuntime.Cache["OnLineUsersSerialNumber"] == null)
                    SetHasChanged();
                return (string)HttpRuntime.Cache["OnLineUsersSerialNumber"];
            }
            set
            {
                HttpRuntime.Cache["OnLineUsersSerialNumber"] = value;
            }
        }
        private static bool SessionExpired(int userId, bool refresh = true)
        {
            if (IsOnLine(userId))
            {
                DateTime lastAccess = LastUserAccess(userId);
                if ((DateTime.Now - lastAccess).TotalMinutes > TimeOut)
                    return true;
                if (refresh)
                    SetUserAccessTime(userId);
            }
            return false;
        }
        #endregion

        #region public members
        public static List<int> ConnectedUsersId
        {
            get
            {
                if (HttpRuntime.Cache["OnLineUsers"] == null)
                    HttpRuntime.Cache["OnLineUsers"] = new List<int>();
                return (List<int>)HttpRuntime.Cache["OnLineUsers"];
            }
        }
        public static bool HasChanged()
        {
            if (HttpContext.Current.Session["SerialNumber"] == null)
            {
                HttpContext.Current.Session["SerialNumber"] = SerialNumber;
                return true;
            }
            string sessionSerialNumber = (string)HttpContext.Current.Session["SerialNumber"];
            HttpContext.Current.Session["SerialNumber"] = SerialNumber;
            return SerialNumber != sessionSerialNumber;
        }
        public static void SetHasChanged()
        {
            SerialNumber = Guid.NewGuid().ToString();
        }
        public static void AddSessionUser(int userId)
        {
            if (userId != 0)
            {
                if (!ConnectedUsersId.Contains(userId))
                {
                    ConnectedUsersId.Add(userId);
                    CurrentUserId = userId;
                    SetUserAccessTime(userId);
                    SetHasChanged();
                }
            }
        }
        public static void RemoveSessionUser()
        {
            RemoveUser(CurrentUserId);
            CurrentUserId = 0;
        }
        public static void RemoveUser(int userId)
        {
            if (userId != 0)
            {
                RemoveLastAccess(userId);
                ConnectedUsersId.Remove(userId);
                SetHasChanged();
            }
        }
        public static User GetSessionUser()
        {
            if (CurrentUserId != 0)
            {
                AppDBEntities DB = new AppDBEntities();
                User currentUser = DB.FindUser(CurrentUserId);
                DB.Dispose();
                return currentUser;
            }
            return null;
        }
        public static bool IsOnLine(int userId)
        {
            return ConnectedUsersId.Contains(userId);
        }
        #endregion

        #region AuthorizeAttribute
        public class UserAccess : AuthorizeAttribute
        {
            private bool ServerSideResponseHandling { get; set; }
            public UserAccess(bool serverSideResponseHandling = true)
            {
                ServerSideResponseHandling = serverSideResponseHandling;
            }
            protected override bool AuthorizeCore(HttpContextBase httpContext)
            {
                HttpContext.Current.Session["CRUD_Access"] = false;
                User sessionUser = OnlineUsers.GetSessionUser();
                if (sessionUser != null)
                {
                    HttpContext.Current.Session["CRUD_Access"] = sessionUser.CRUD_Access;
                    if (OnlineUsers.SessionExpired(sessionUser.Id, ServerSideResponseHandling))
                    {
                        if (ServerSideResponseHandling)
                        {
                            OnlineUsers.RemoveSessionUser();
                            httpContext.Response.Redirect("~/Accounts/Login?message=Session expirée!");
                            return false;
                        }
                        else
                        {
                            httpContext.Response.StatusCode = 408; // Timeout status
                        }
                    }
                    else
                    {
                        if (sessionUser.Blocked)
                        {
                            if (ServerSideResponseHandling)
                            {
                                OnlineUsers.RemoveSessionUser();
                                httpContext.Response.Redirect("~/Accounts/Login?message=Compte bloqué!");
                                return false;
                            }
                            else
                            {
                                httpContext.Response.StatusCode = 403; // Forbiden status
                            }
                        }
                    }
                }
                else
                {
                    OnlineUsers.RemoveSessionUser();
                    httpContext.Response.Redirect("~/Accounts/Login?message=Accès non autorisé!");
                    return false;
                }
                return true;
            }
        }
        public class PowerUserAccess : AuthorizeAttribute
        {
            private bool ServerSideResponseHandling { get; set; }
            public PowerUserAccess(bool serverSideResponseHandling = true)
            {
                ServerSideResponseHandling = serverSideResponseHandling;
            }
            protected override bool AuthorizeCore(HttpContextBase httpContext)
            {
                User sessionUser = OnlineUsers.GetSessionUser();
                if (sessionUser != null && sessionUser.IsPowerUser)
                {
                    HttpContext.Current.Session["CRUD_Access"] = sessionUser.CRUD_Access;
                    if (OnlineUsers.SessionExpired(sessionUser.Id, ServerSideResponseHandling))
                    {
                        if (ServerSideResponseHandling)
                        {
                            OnlineUsers.RemoveSessionUser();
                            httpContext.Response.Redirect("~/Accounts/Login?message=Session expirée!");
                            return false;
                        }
                        else
                        {
                            httpContext.Response.StatusCode = 408; // Timeout status
                        }
                    }
                }
                else
                {
                    OnlineUsers.RemoveSessionUser();
                    httpContext.Response.Redirect("~/Accounts/Login?message=Accès non autorisé!");
                    return false;
                }
                return true;
            }
        }
        public class AdminAccess : AuthorizeAttribute
        {
            private bool ServerSideResponseHandling { get; set; }
            public AdminAccess(bool serverSideResponseHandling = true)
            {
                ServerSideResponseHandling = serverSideResponseHandling;
            }
            protected override bool AuthorizeCore(HttpContextBase httpContext)
            {
                User sessionUser = OnlineUsers.GetSessionUser();
                if (sessionUser != null && sessionUser.IsAdmin)
                {
                    HttpContext.Current.Session["CRUD_Access"] = sessionUser.CRUD_Access;
                    if (OnlineUsers.SessionExpired(sessionUser.Id, ServerSideResponseHandling))
                    {
                        if (ServerSideResponseHandling)
                        {
                            OnlineUsers.RemoveSessionUser();
                            httpContext.Response.Redirect("~/Accounts/Login?message=Session expirée!");
                            return false;
                        }
                        else
                        {
                            httpContext.Response.StatusCode = 408; // Timeout status
                        }
                    }
                }
                else
                {
                    OnlineUsers.RemoveSessionUser();
                    httpContext.Response.Redirect("~/Accounts/Login?message=Accès non autorisé!");
                    return false;
                }
                return true;
            }
        }
        #endregion
    }
}
