using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zpravodajstvi.Application.DTOs;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Presentation.Models;

namespace Zpravodajstvi.Presentation.Controllers;

// Registrace, přihlášení, odhlášení, profil a změna hesla. S ASP.NET Core Identity pracuje jen přes IAccountService a IUserService.
public class AccountController : Controller
{
    private readonly IAccountService _accountService;
    private readonly IUserService _userService;
    private readonly IArticleService _articleService;
    private readonly ICommentService _commentService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IAccountService accountService,
        IUserService userService,
        IArticleService articleService,
        ICommentService commentService,
        ILogger<AccountController> logger)
    {
        _accountService = accountService;
        _userService = userService;
        _articleService = articleService;
        _commentService = commentService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Služba vytvoří účet s rolí Čtenář a uživatele rovnou přihlásí
        var result = await _accountService.RegisterAsync(new RegisterUserDto
        {
            FullName = model.FullName,
            Email = model.Email,
            Password = model.Password
        });

        if (result.Succeeded)
        {
            _logger.LogInformation("Zaregistroval se nový uživatel {Email}.", model.Email);
            return RedirectToLocal(returnUrl);
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await _accountService.LoginAsync(model.Email, model.Password, model.RememberMe))
        {
            _logger.LogInformation("Uživatel {Email} se přihlásil.", model.Email);
            return RedirectToLocal(returnUrl);
        }

        _logger.LogWarning("Neúspěšný pokus o přihlášení pro {Email}.", model.Email);
        ModelState.AddModelError(string.Empty, "Neplatné přihlašovací údaje. Zkontrolujte prosím e-mail a heslo.");
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _accountService.LogoutAsync();
        return RedirectToAction("Index", "Home");
    }

    // Profil přihlášeného uživatele: jeho údaje, jeho články a komentáře (každý vidí jen svůj profil)
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = userId != null ? await _userService.GetUserByIdAsync(userId) : null;
        if (user == null)
        {
            return Challenge();
        }

        var model = new ProfileViewModel
        {
            User = user,
            Articles = await _articleService.GetArticlesByAuthorAsync(user.Id),
            Comments = await _commentService.GetCommentsByUserIdAsync(user.Id)
        };

        return View(model);
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    // Změna vlastního hesla – Id uživatele se bere z přihlášení (claim NameIdentifier),
    // takže tímto formulářem nejde změnit heslo jinému účtu. Ověření a uložení hesla dělá IAccountService.
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Challenge();
        }

        var result = await _accountService.ChangePasswordAsync(userId, model.CurrentPassword, model.NewPassword);
        if (result.Succeeded)
        {
            _logger.LogInformation("Uživatel {UserId} si změnil heslo.", userId);
            TempData["SuccessMessage"] = "Heslo bylo úspěšně změněno.";
            return RedirectToAction(nameof(Profile));
        }

        _logger.LogWarning("Neúspěšný pokus o změnu hesla uživatele {UserId}.", userId);
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }
}
