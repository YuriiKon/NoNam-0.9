using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Security.Claims;
using Microsoft.Owin.Security;
using System.Threading.Tasks;
using NoNameApp.WEB.Models;
using NoNameApp.LogicContracts;
using NoNameApp.Entities;
using AutoMapper;
using NoNameApp.BLL.Infrastructure;
using System.Collections.Generic;

namespace NoNameApp.WEB.Controllers
{
    public class AccountController : Controller
    {
        IUserService userService;
        public AccountController(IUserService serv)
        {
            userService = serv;
        }

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult> Login(LoginModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    ClaimsIdentity claim = await userService.Authenticate(model.Email, model.Password);
                    if (claim != null)
                    {
                        AuthenticationManager.SignOut();
                        AuthenticationManager.SignIn(new AuthenticationProperties
                        {
                            IsPersistent = true
                        }, claim);
                        return RedirectToAction("Index", "Home");
                    }
                }
            }
            catch (ValidationException ex)
            {
                ModelState.AddModelError(ex.Property,ex.Message);
            }
            return View(model);
        }

        [Authorize]
        public ActionResult Logout()
        {
            AuthenticationManager.SignOut();
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    User user = new User { Email = model.Email, Password = model.Password };
                    OperationDetails operationDetails = await userService.Create(user);
                    ModelState.AddModelError(operationDetails.Property, operationDetails.Message);
                }
            }
            catch (ValidationException ex)
            {
                ModelState.AddModelError(ex.Property, ex.Message);
            }
            return View(model);
        }

        [Authorize]
        public ActionResult Edit()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public ActionResult Edit(EditUserViewModel model)
        {
            try
            {
                var user = GetUser();
                if (ModelState.IsValid)
                {
                    OperationDetails operationDetails = userService.EditUser(user, model.OldPassword, model.NewPassword);
                    ModelState.AddModelError(operationDetails.Property, operationDetails.Message);
                }
            }
            catch (ValidationException ex)
            {
                ModelState.AddModelError(ex.Property, ex.Message);
            }
            return View(model);
        }

            private User GetUser()
            {
                string email = User.Identity.Name;
                var user = userService.GetUser(email);
                return user;
            }
        }
}