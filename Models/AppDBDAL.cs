using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;

namespace MDB.Models
{
    public static class AppDBDAL
    {
        #region Transaction management
        private static DbContextTransaction Transaction
        {
            get
            {
                if (HttpContext.Current != null)
                {
                    return (DbContextTransaction)HttpContext.Current.Session["Transaction"];
                }
                return null;
            }
            set
            {
                if (HttpContext.Current != null)
                {
                    HttpContext.Current.Session["Transaction"] = value;
                }
            }
        }
        private static void BeginTransaction(AppDBEntities DB)
        {
            if (Transaction != null)
            {
                System.Diagnostics.Debug.WriteLine("BeginTransaction failed");
                throw new Exception("BeginTransaction failed");
            }
            Transaction = DB.Database.BeginTransaction();
        }
        private static void Commit()
        {
            if (Transaction != null)
            {
                Transaction.Commit();
                Transaction.Dispose();
                Transaction = null;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Commit failed : No transaction in use");
                throw new Exception("Commit failed : No transaction in use");
            }
        }
        private static void Rollback()
        {
            if (Transaction != null)
            {
                Transaction.Rollback();
                Transaction.Dispose();
                Transaction = null;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Rollback failed : No transaction in use");
                throw new Exception("Rollback failed : No transaction in use");
            }
        }
        #endregion
        #region Data HasChanged accessors
        public static bool ActorsHasChanged
        {
            get
            {
                if (HttpContext.Current.Session["ActorsHasChanged"] == null)
                    HttpContext.Current.Session["ActorsHasChanged"] = true;
                return (bool)HttpContext.Current.Session["ActorsHasChanged"];
            }
            set { HttpContext.Current.Session["ActorsHasChanged"] = value; }
        }
        public static bool MoviesHasChanged
        {
            get
            {
                if (HttpContext.Current.Session["MoviesHasChanged"] == null)
                    HttpContext.Current.Session["MoviesHasChanged"] = true;
                return (bool)HttpContext.Current.Session["MoviesHasChanged"];
            }
            set { HttpContext.Current.Session["MoviesHasChanged"] = value; }
        }
        public static bool DistributorsHasChanged
        {
            get
            {
                if (HttpContext.Current.Session["DistributorsHasChanged"] == null)
                    HttpContext.Current.Session["DistributorsHasChanged"] = true;
                return (bool)HttpContext.Current.Session["DistributorsHasChanged"];
            }
            set { HttpContext.Current.Session["DistributorsHasChanged"] = value; }
        }
        #endregion
        #region Accounts CRUD
        public static bool EmailAvailable(this AppDBEntities DB, string email, int excludedId = 0)
        {
            User user = DB.Users.Where(u => u.Email.ToLower() == email.ToLower()).FirstOrDefault();
            if (user == null)
                return true;
            else
                if (user.Id != excludedId)
                return user.Email.ToLower() != email.ToLower();
            return true;
        }
        public static bool EmailExist(this AppDBEntities DB, string email)
        {
            return DB.Users.Where(u => u.Email.ToLower() == email.ToLower()).FirstOrDefault() != null;
        }
        public static bool EmailBlocked(this AppDBEntities DB, string email)
        {
            User user = DB.Users.Where(u => u.Email.ToLower() == email.ToLower()).FirstOrDefault();
            if (user != null)
                return user.Blocked;
            return true;
        }
        public static bool EmailVerified(this AppDBEntities DB, string email)
        {
            User user = DB.Users.Where(u => u.Email.ToLower() == email.ToLower()).FirstOrDefault();
            if (user != null)
                return user.Verified;
            return false;
        }
        
        public static User GetUser(this AppDBEntities DB, LoginCredential loginCredential)
        {
            User user = DB.Users.Where(u => (u.Email.ToLower() == loginCredential.Email.ToLower()) &&
                                            (u.Password == loginCredential.Password))
                                .FirstOrDefault();
            return user;
        }
        public static User AddUser(this AppDBEntities DB, User user)
        {
            try
            {
                user.SaveAvatar();
                user.CreationDate = DateTime.Now;
                user.Verified = false;
                user.Blocked = false;
                user.UserTypeId = 3;
                user = DB.Users.Add(user);
                DB.SaveChanges();
                DB.Entry(user).Reference(u => u.Gender).Load();
                DB.Entry(user).Reference(u => u.UserType).Load();
                OnlineUsers.SetHasChanged();
                return user;
            }
            catch (Exception ex)
            {
                user.RemoveAvatar();
                System.Diagnostics.Debug.WriteLine($"Add user failed : Message - {ex.Message}");
            }
            return null;
        }
        public static User UpdateUser(this AppDBEntities DB, User user)
        {
            try
            {
                user.SaveAvatar();
                DB.Entry(user).State = EntityState.Modified;
                DB.SaveChanges();
                DB.Entry(user).Reference(u => u.Gender).Load();
                DB.Entry(user).Reference(u => u.UserType).Load();
                OnlineUsers.SetHasChanged();
                return user;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Update user failed : Message - {ex.Message}");
                Rollback();
            }
            return null;
        }
        public static bool RemoveUser(this AppDBEntities DB, int userId)
        {
            try
            {
                BeginTransaction(DB);
                User userToDelete = DB.Users.Find(userId);
                if (userToDelete != null)
                {
                    DB.Logins.RemoveRange(DB.Logins.Where(l => l.UserId == userId));
                    DB.SaveChanges();
                    DB.UnverifiedEmails.RemoveRange(DB.UnverifiedEmails.Where(u => u.UserId == userId));
                    DB.SaveChanges();
                    DB.ResetPasswordCommands.RemoveRange(DB.ResetPasswordCommands.Where(r => r.UserId == userId));
                    DB.SaveChanges();
                    userToDelete.RemoveAvatar();
                    DB.Users.Remove(userToDelete);
                    DB.SaveChanges();
                    Commit();
                    OnlineUsers.RemoveUser(userToDelete.Id);
                    OnlineUsers.SetHasChanged();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Rollback();
                System.Diagnostics.Debug.WriteLine($"Remove user failed : Message - {ex.Message}");
                return false;
            }
        }
        public static User FindUser(this AppDBEntities DB, int id)
        {
            try
            {
                User user = DB.Users.Find(id);
                if (user != null)
                {
                    user.ConfirmEmail = user.Email;
                    user.ConfirmPassword = user.Password;
                    DB.Entry(user).Reference(u => u.Gender).Load();
                    DB.Entry(user).Reference(u => u.UserType).Load();
                }
                return user;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Find user failed : Message - {ex.Message}");
                return null;
            }
        }
        public static IEnumerable<User> SortedUsers(this AppDBEntities DB)
        {
            return DB.Users.OrderBy(u => u.FirstName).ThenBy(u => u.LastName);
        }
       
        public static bool VerifyUser(this AppDBEntities DB, int userId, int code)
        {
            User user = DB.FindUser(userId);
            if (user != null)
            {
                // take the last email verification request
                UnverifiedEmail unverifiedEmail = DB.UnverifiedEmails.Where(u => u.UserId == userId).FirstOrDefault();
                if (unverifiedEmail != null)
                {
                    if (unverifiedEmail.VerificationCode == code)
                    {
                        try
                        {
                            BeginTransaction(DB);
                            user.Email = user.ConfirmEmail = unverifiedEmail.Email;
                            user.Verified = true;
                            DB.Entry(user).State = EntityState.Modified;
                            DB.UnverifiedEmails.Remove(unverifiedEmail);
                            DB.SaveChanges();
                            Commit();
                            OnlineUsers.SetHasChanged();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Verify_User failed : Message - {ex.Message}");
                            Rollback();
                        }
                    }
                }
            }
            return false;
        }
        public static UnverifiedEmail Add_UnverifiedEmail(this AppDBEntities DB, int userId, string email)
        {
            try
            {
                UnverifiedEmail unverifiedEmail = new UnverifiedEmail() { UserId = userId, Email = email, VerificationCode = DateTime.Now.Millisecond };
                unverifiedEmail = DB.UnverifiedEmails.Add(unverifiedEmail);
                DB.SaveChanges();
                return unverifiedEmail;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Add_UnverifiedEmail failed : Message - {ex.Message}");
                return null;
            }
        }
        public static bool HaveUnverifiedEmail(this AppDBEntities DB, int userId, int code)
        {
            return DB.UnverifiedEmails.Where(u => (u.UserId == userId && u.VerificationCode == code)).FirstOrDefault() != null;
        }
        
        public static ResetPasswordCommand AddResetPasswordCommand(this AppDBEntities DB, string email)
        {
            try
            {
                User user = DB.Users.Where(u => u.Email == email).FirstOrDefault();
                if (user != null)
                {
                    ResetPasswordCommand resetPasswordCommand =
                        new ResetPasswordCommand() { UserId = user.Id, VerificationCode = DateTime.Now.Millisecond };

                    resetPasswordCommand = DB.ResetPasswordCommands.Add(resetPasswordCommand);
                    DB.SaveChanges();
                    return resetPasswordCommand;
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Add_ResetPasswordCommand failed : Message - {ex.Message}");
                return null;
            }
        }
        public static ResetPasswordCommand FindResetPasswordCommand(this AppDBEntities DB, int userid, int verificationCode)
        {
            return DB.ResetPasswordCommands.Where(r => (r.UserId == userid && r.VerificationCode == verificationCode)).FirstOrDefault();
        }
        public static bool ResetPassword(this AppDBEntities DB, int userId, string password)
        {
            User user = DB.FindUser(userId);
            if (user != null)
            {
                user.Password = user.ConfirmPassword = password;
                ResetPasswordCommand resetPasswordCommand = DB.ResetPasswordCommands.Where(r => r.UserId == userId).FirstOrDefault();
                if (resetPasswordCommand != null)
                {
                    try
                    {
                        BeginTransaction(DB);
                        DB.Entry(user).State = EntityState.Modified;
                        DB.ResetPasswordCommands.Remove(resetPasswordCommand);
                        DB.SaveChanges();
                        Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"ResetPassword failed : Message - {ex.Message}");
                        Rollback();
                    }
                }
            }
            return false;
        }

        public static String GetClientIPAddress()
        {
            String ip = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (string.IsNullOrEmpty(ip))
                ip = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
            else
                ip = ip.Split(',')[0];

            return ip;
        }
        public static Login AddLogin(this AppDBEntities DB, int userId)
        {
            try
            {
                Login login = new Login();
                login.LoginDate = login.LogoutDate = DateTime.Now;
                login.UserId = userId;
                login.IpAddress = GetClientIPAddress();
                login = DB.Logins.Add(login);
                DB.SaveChanges();
                return login;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AddLogin failed : Message - {ex.Message}");
                return null;
            }
        }
        public static bool UpdateLogout(this AppDBEntities DB, int loginId)
        {
            try
            {
                Login login = DB.Logins.Find(loginId);
                if (login != null)
                {
                    login.LogoutDate = DateTime.Now;
                    DB.Entry(login).State = EntityState.Modified;
                    DB.SaveChanges();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateLogout failed : Message - {ex.Message}");
                return false;
            }
        }
        public static bool UpdateLogoutByUserId(this AppDBEntities DB, int userId)
        {
            try
            {
                Login login = DB.Logins.Where(l => l.UserId == userId).OrderByDescending(l => l.LoginDate).FirstOrDefault();
                if (login != null)
                {
                    login.LogoutDate = DateTime.Now;
                    DB.Entry(login).State = EntityState.Modified;
                    DB.SaveChanges();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateLogoutByUserId failed : Message - {ex.Message}");
                return false;
            }
        }
        public static bool DeleteLoginsJournalDay(this AppDBEntities DB, DateTime day)
        {
            try
            {
                DateTime dayAfter = day.AddDays(1);
                DB.Logins.RemoveRange(DB.Logins.Where(l => l.LoginDate >= day && l.LoginDate < dayAfter));
                DB.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteLoginsJournalDay failed : Message - {ex.Message}");
                return false;
            }
        }
        #endregion
        #region Movie CRUD
        public static Movie AddMovie(this AppDBEntities DB, Movie movie, List<int> actorsIdList, List<int> SelectedDistributorsId)
        {
            try
            {
                BeginTransaction(DB);
                movie.SavePoster();
                movie = DB.Movies.Add(movie);
                DB.SaveChanges();
                SetMovieCastings(DB, movie.Id, actorsIdList);
                SetMovieDistribution(DB, movie.Id, SelectedDistributorsId);
                Commit();
                MoviesHasChanged = true;
                return movie;
            }
            catch (System.Exception ex)
            {
                movie.RemovePoster();
                Rollback();
                System.Diagnostics.Debug.Write(ex.ToString());
            }
            return null;
        }
        public static bool UpdateMovie(this AppDBEntities DB, Movie movie, List<int> actorsIdList, List<int> SelectedDistributorsId)
        {
            try
            {
                BeginTransaction(DB);
                movie.SavePoster();
                DB.Entry(movie).State = EntityState.Modified;
                DB.SaveChanges();
                SetMovieCastings(DB, movie.Id, actorsIdList);
                SetMovieDistribution(DB, movie.Id, SelectedDistributorsId);
                Commit();
                MoviesHasChanged = true;
                return true;
            }
            catch (System.Exception ex)
            {
                Rollback();
                System.Diagnostics.Debug.Write(ex.ToString());
            }
            return false;
        }
        public static bool RemoveMovie(this AppDBEntities DB, int id)
        {
            Movie movie = DB.Movies.Find(id);
            if (movie != null)
            {
                try
                {
                    BeginTransaction(DB);
                    movie.RemovePoster();
                    SetMovieCastings(DB, movie.Id, null);
                    SetMovieDistribution(DB, movie.Id, null);
                    DB.Movies.Remove(movie);
                    DB.SaveChanges();
                    Commit();
                    MoviesHasChanged = true;
                    return true;
                }
                catch (Exception ex)
                {
                    Rollback();
                    System.Diagnostics.Debug.Write(ex.ToString());
                }
            }
            return false;
        }
        private static bool SetMovieCastings(AppDBEntities DB, int movieId, List<int> actorsIdList)
        {
            DB.Castings.RemoveRange(DB.Castings.Where(c => c.MovieId == movieId));
            if (actorsIdList != null)
            {
                foreach (int actor_Id in actorsIdList)
                {
                    Casting casting = new Casting() { ActorId = actor_Id, MovieId = movieId };
                    DB.Castings.Add(casting);
                }
            }
            DB.SaveChanges();
            return true;
        }
        #endregion
        #region Actor CRUD
        public static Actor AddActor(this AppDBEntities DB, Actor actor, List<int> movieIdList)
        {
            try
            {
                BeginTransaction(DB);
                actor.SaveAvatar();
                actor = DB.Actors.Add(actor);
                DB.SaveChanges();
                SetActorCastings(DB, actor.Id, movieIdList);
                Commit();
                ActorsHasChanged = true;
                return actor;
            }
            catch (Exception ex)
            {
                actor.RemoveAvatar();
                Rollback();
                System.Diagnostics.Debug.Write(ex.ToString());
            }
            return null;
        }
        public static bool UpdateActor(this AppDBEntities DB, Actor actor, List<int> movieIdList)
        {
            try
            {
                BeginTransaction(DB);
                actor.SaveAvatar();
                DB.Entry(actor).State = EntityState.Modified;
                DB.SaveChanges();
                SetActorCastings(DB, actor.Id, movieIdList);
                Commit();
                ActorsHasChanged = true;
                return true;
            }
            catch (Exception ex)
            {
                Rollback();
                System.Diagnostics.Debug.Write(ex.ToString());
            }
            return true;
        }
        public static bool RemoveActor(this AppDBEntities DB, int id)
        {
            Actor actor = DB.Actors.Find(id);
            if (actor != null)
            {
                try
                {
                    BeginTransaction(DB);
                    actor.RemoveAvatar();
                    SetActorCastings(DB, actor.Id, null);
                    DB.Actors.Remove(actor);
                    DB.SaveChanges();
                    Commit();
                    ActorsHasChanged = true;
                    return true;
                }
                catch (Exception ex)
                {
                    Rollback();
                    System.Diagnostics.Debug.Write(ex.ToString());
                }
            }
            return false;
        }
        private static bool SetActorCastings(AppDBEntities DB, int actorId, List<int> MoviesIdList)
        {
            DB.Castings.RemoveRange(DB.Castings.Where(c => c.ActorId == actorId));
            if (MoviesIdList != null)
            {
                foreach (int Movie_Id in MoviesIdList)
                {
                    Casting casting = new Casting() { ActorId = actorId, MovieId = Movie_Id };
                    DB.Castings.Add(casting);
                }
            }
            DB.SaveChanges();
            return true;
        }
        private static bool SetMovieDistribution(AppDBEntities DB, int movieId, List<int> DistributorIdList)
        {
            DB.Distributions.RemoveRange(DB.Distributions.Where(d => d.MovieId == movieId));
            if (DistributorIdList != null)
            {
                foreach (int distributor_Id in DistributorIdList)
                {
                    Distribution distribution = new Distribution() { DistributorId = distributor_Id, MovieId = movieId };
                    DB.Distributions.Add(distribution);
                }
            }
            DB.SaveChanges();
            return true;
        }
        #endregion
        #region Distributor CRUD
        public static Distributor AddDistributor(this AppDBEntities DB, Distributor distributor, List<int> movieIdList)
        {
            BeginTransaction(DB);
            distributor.SaveLogo();
            distributor = DB.Distributors.Add(distributor);
            DB.SaveChanges();
            SetDistribution(DB, distributor.Id, movieIdList);
            Commit();
            DistributorsHasChanged = true;
            return distributor;
        }
        public static bool UpdateDistributor(this AppDBEntities DB, Distributor distibutor, List<int> movieIdList)
        {
            BeginTransaction(DB);
            distibutor.SaveLogo();
            DB.Entry(distibutor).State = EntityState.Modified;
            DB.SaveChanges();
            SetDistribution(DB, distibutor.Id, movieIdList);
            Commit();
            DistributorsHasChanged = true;
            return true;
        }
        public static bool RemoveDistributor(this AppDBEntities DB, int id)
        {
            Distributor distributor = DB.Distributors.Find(id);
            if (distributor != null)
            {
                BeginTransaction(DB);
                distributor.RemoveLogo();
                SetDistribution(DB, distributor.Id, null);
                DB.Distributors.Remove(distributor);
                DB.SaveChanges();
                Commit();
                DistributorsHasChanged = true;
                return true;
            }
            return false;
        }
        private static bool SetDistribution(AppDBEntities DB, int distributorId, List<int> MoviesIdList)
        {
            DB.Distributions.RemoveRange(DB.Distributions.Where(d => d.DistributorId == distributorId));
            if (MoviesIdList != null)
            {
                foreach (int Movie_Id in MoviesIdList)
                {
                    Distribution distribution = new Distribution() { DistributorId = distributorId, MovieId = Movie_Id };
                    DB.Distributions.Add(distribution);
                }
            }
            DB.SaveChanges();
            return true;
        }
        #endregion
    }
}