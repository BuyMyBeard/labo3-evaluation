using MailSender.Models;
using MDB.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Web.Mvc;
using System.Xml.Linq;

namespace MDB.Controllers
{
    public class AccountsController : Controller
    {
        private readonly AppDBEntities DB = new AppDBEntities();

        [HttpPost] 
        public JsonResult EmailAvailable(string email, int id)
        {
            return Json(DB.EmailAvailable(email, id));
        }

        [HttpPost]
        public JsonResult EmailExist(string email)
        {
            return Json(DB.EmailExist(email));
        }

        #region Login and Logout
        public ActionResult Login(string message)
        {
            ViewBag.Message = message;
            return View(new LoginCredential());
        }
        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult Login(LoginCredential loginCredential)
        {
            if (ModelState.IsValid)
            {
                if (DB.EmailBlocked(loginCredential.Email))
                {
                    ModelState.AddModelError("Email", "Ce compte est bloqué.");
                    return View(loginCredential);
                }
                if (!DB.EmailVerified(loginCredential.Email))
                {
                    ModelState.AddModelError("Email", "Ce courriel n'est pas vérifié.");
                    return View(loginCredential);
                }
                User user = DB.GetUser(loginCredential);
                if (user == null)
                {
                    ModelState.AddModelError("Password", "Mot de passe incorrecte.");
                    return View(loginCredential);
                }
                if (OnlineUsers.IsOnLine(user.Id))
                {
                    ModelState.AddModelError("Email", "Cet usager est déjà connecté.");
                    return View(loginCredential);
                }
                OnlineUsers.AddSessionUser(user.Id);
                Session["currentLoginId"] = DB.AddLogin(user.Id).Id;
                return RedirectToAction("Index", "Movies");
            }
            return View(loginCredential);
        }
        public ActionResult Logout()
        {
            if (Session["currentLoginId"] != null)
                DB.UpdateLogout((int)Session["currentLoginId"]);
            OnlineUsers.RemoveSessionUser();
            return RedirectToAction("Login");
        }
        #endregion

        public ActionResult Signup()
        {
            ViewBag.Genders = SelectListUtilities<Gender>.Convert(DB.Genders.ToList());
            ViewBag.SelectedGender = 0;
            return View(new User());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Signup(User user)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Genders = SelectListUtilities<Gender>.Convert(DB.Genders.ToList());
                ViewBag.SelectedGender = 0;
                return View(user);
            }


            DB.AddUser(user);

            var unverifiedEmail = DB.Add_UnverifiedEmail(user.Id, user.Email);
            if (unverifiedEmail == null)
            {
                ViewBag.Genders = SelectListUtilities<Gender>.Convert(DB.Genders.ToList());
                ViewBag.SelectedGender = 0;
                return View(user);
            }


            string toName = user.FirstName + " " + user.LastName;
            string toEmail = user.Email;
            const string Subject = "MDB - Vérification d'inscription...";
            string body =
                $"Bonjour {user.GetFullName(true)}, <br>" +
                "<br>" +
                "Merci de vous être inscrit au site MDB." +
                "Pour utiliser votre compte vous devez confirmer votre inscription en cliquant sur le lien suivant :<br>" +
                "<br>" +
                $"<a href=https://localhost:44339/Accounts/VerifyUser?userId={user.Id}&code={unverifiedEmail.VerificationCode}>Confirmez votre inscription...</a><br>" +
                "<br>" +
                "<b>Ce courriel a été généré automatiquement, veuillez ne pas y répondre.</b>";

            SMTP.SendEmail(toName, toEmail, Subject, body);
            return RedirectToAction("SignupDone", new {name = user.GetFullName(true), type = "inscription"});
        }

        public ActionResult SignupDone(string name, string type)
        {
            ViewBag.Name = name;
            ViewBag.Type = type;
            return View();
        }

        public ActionResult VerifyUser(int userId, int code)
        {
            if (DB.VerifyUser(userId, code))
            {
                User user = DB.Users.Find(userId);
                return RedirectToAction("VerifyDone", new {name = user.GetFullName(true)});
            }
            return RedirectToAction("VerifyError");
        }

        public ActionResult VerifyDone(string name)
        {
            ViewBag.Name = name;
            return View();
        }

        public ActionResult VerifyError()
        {
            return View();
        }

        public ActionResult Profil()
        {
            User user = OnlineUsers.GetSessionUser();
            ViewBag.Genders = SelectListUtilities<Gender>.Convert(DB.Genders.ToList());
            ViewBag.SelectedGender = user.GenderId;
            Session["password"] = user.Password;
            Session["GUID"] = Guid.NewGuid().ToString().Substring(0,12);
            Session["email"] = user.Email;
            return View(user);
        }
        [HttpPost]
        public ActionResult Profil(User user)
        {
            UnverifiedEmail unverifiedEmail;
            if (!ModelState.IsValid)
            {
                ViewBag.Genders = SelectListUtilities<Gender>.Convert(DB.Genders.ToList());
                ViewBag.SelectedGender = user.GenderId;
                return View(user);
            }
            if (user.Password == Session["GUID"].ToString())
            {
                user.Password = Session["password"].ToString();
                user.ConfirmPassword = Session["password"].ToString();
            }
            DB.UpdateUser(user);
            if (user.Email != Session["email"].ToString())
            {
                unverifiedEmail = DB.Add_UnverifiedEmail(user.Id, user.Email);
                if (unverifiedEmail == null)
                {
                    ViewBag.Genders = SelectListUtilities<Gender>.Convert(DB.Genders.ToList());
                    ViewBag.SelectedGender = user.GenderId;
                    return View(user);
                }
                string toName = user.FirstName + " " + user.LastName;
                string toEmail = user.Email;
                const string Subject = "MDB - Changement de courriel...";
                string body =
                $"Bonjour {user.GetFullName(true)}, <br>" +
                "<br>" +
                "Vous avez demandé de modifier votre adresse courriel.<br>" +
                "Confirmez celle-ci en cliquant sur le lien suivant :<br>" +
                "<br>" +
                $"<a href=https://localhost:44339/Accounts/VerifyUser?userId={user.Id}&code={unverifiedEmail.VerificationCode }>Confirmer votre adresse courriel...</a><br>" +
                "<br>" +
                "<b>Ce courriel a été généré automatiquement, veuillez ne pas y répondre.</b>";

                SMTP.SendEmail(toName, toEmail, Subject, body);
                return RedirectToAction("SignupDone", new { name = user.GetFullName(true), type = "changement de courriel" });
            }
            return RedirectToAction("Index", "Movies");
        }
        public ActionResult ResetPasswordCommand()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ResetPasswordCommand(string email)
        {
            ResetPasswordCommand reset = DB.AddResetPasswordCommand(email);
            if (reset == null)
                return View();
            User user = DB.FindUser(reset.UserId);
            string toName = user.FirstName + " " + user.LastName;
            string toEmail = user.Email;
            const string Subject = "MDB - Réinitialisation...";
            string body =
                $"Bonjour {user.GetFullName(true)}, <br>" +
                "<br>" +
                "Vous avez demandé de réinitaliser votre mot de passe.<br>" +
                "Procédez en cliquant sur le lien suivant :<br>" +
                "<br>" +
                $"<a href=https://localhost:44339/Accounts/ResetPassword?userId={user.Id}&code={reset.VerificationCode}>Réinitialisation de mot de passe...</a><br>" +
                "<br>" +
                "<b>Ce courriel a été généré automatiquement, veuillez ne pas y répondre.</b>";

            SMTP.SendEmail(toName, toEmail, Subject, body);
            return RedirectToAction("ResetPasswordCommandDone");
        }

        public ActionResult ResetPasswordCommandDone()
        {
            return View();
        }

        public ActionResult ResetPassword(int userId, int code)
        {
            ResetPasswordCommand reset = DB.FindResetPasswordCommand(userId, code);
            if (reset == null)
                return RedirectToAction("Login");
            PasswordView model = new PasswordView();
            model.UserId = userId;
            return View(model);
        }

        [HttpPost]
        public ActionResult ResetPassword(PasswordView model)
        {
            if (DB.ResetPassword(model.UserId, model.Password))
                return RedirectToAction("ResetPasswordDone");
            return View(model);
        }
        public ActionResult ResetPasswordDone()
        {
            return View();
        }
    }
}