using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    [RequireLogin]
    public class ProfileController : RaceDayController
    {
        public ProfileController(IRaceDayApi api) : base(api)
        {
        }

        public async Task<IActionResult> Index()
        {
            var profile = await Api.GetProfileAsync();
            if (!profile.Success || profile.Data == null)
            {
                NoteError(profile.Error);
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Profile = profile.Data;
            return View(new ProfilePage
            {
                Details = new ProfileForm
                {
                    FirstName = profile.Data.FirstName,
                    LastName = profile.Data.LastName,
                    PhoneNumber = profile.Data.PhoneNumber
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> Update([Bind(Prefix = "Details")] ProfileForm details)
        {
            if (!ModelState.IsValid)
            {
                NoteError("Check the profile details and try again.");
                return RedirectToAction(nameof(Index));
            }

            var result = await Api.UpdateProfileAsync(details);
            if (Succeeded(result) && result.Data != null)
            {
                RememberUser(result.Data.UserId, result.Data.Role, result.Data.FirstName + " " + result.Data.LastName);
                NoteSuccess("Profile updated.");
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Password([Bind(Prefix = "Password")] PasswordForm password)
        {
            if (!ModelState.IsValid)
            {
                NoteError("Check the password fields and try again.");
                return RedirectToAction(nameof(Index));
            }

            var result = await Api.ChangePasswordAsync(password);
            if (Succeeded(result))
            {
                NoteSuccess("Password updated.");
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [ActionName("Picture")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> PictureFile()
        {
            var file = await Api.GetProfilePictureAsync();
            if (!file.Success || file.Data == null || file.Data.Bytes.Length == 0)
            {
                return NotFound();
            }

            return File(file.Data.Bytes, file.Data.ContentType);
        }

        [HttpPost]
        public async Task<IActionResult> Picture(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                NoteError("Choose a JPG or PNG picture to upload.");
                return RedirectToAction(nameof(Index));
            }

            var result = await Api.UploadProfilePictureAsync(file);
            if (Succeeded(result))
            {
                NoteSuccess("Profile picture updated.");
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
