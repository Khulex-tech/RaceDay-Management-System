using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    public class AccountController : RaceDayController
    {
        public AccountController(IRaceDayApi api) : base(api)
        {
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (CurrentUserId != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new RegisterForm());
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterForm form)
        {
            if (form.Role != "Participant" && form.Role != "Organiser")
            {
                ModelState.AddModelError(nameof(form.Role), "Choose Participant or Organiser.");
            }

            if (form.Role == "Organiser" && string.IsNullOrWhiteSpace(form.InviteCode))
            {
                ModelState.AddModelError(nameof(form.InviteCode), "Organiser registration needs the invite code.");
            }

            if (!ModelState.IsValid)
            {
                return View(form);
            }

            var result = form.Role == "Organiser"
                ? await Api.RegisterOrganiserAsync(form, form.InviteCode!.Trim())
                : await Api.RegisterParticipantAsync(form);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Registration failed.");
                return View(form);
            }

            NoteSuccess("Account created. Log in with the email and password you just chose.");
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl)
        {
            if (CurrentUserId != null)
            {
                return GoAfterLogin(CurrentRole, returnUrl);
            }

            return View(new LoginForm { ReturnUrl = returnUrl });
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginForm form)
        {
            if (!ModelState.IsValid)
            {
                return View(form);
            }

            var result = await Api.LoginAsync(form.Email, form.Password);
            if (!result.Success || result.Data == null)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Invalid email or password.");
                return View(form);
            }

            RememberUser(result.Data.UserId, result.Data.Role, result.Data.FullName);
            return GoAfterLogin(result.Data.Role, form.ReturnUrl);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await Api.LogoutAsync();
            HttpContext.Session.Clear();
            NoteSuccess("You have logged out.");
            return RedirectToAction(nameof(Login));
        }

        private IActionResult GoAfterLogin(string? role, string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            if (role == "Organiser")
            {
                return Redirect("/organiser");
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
