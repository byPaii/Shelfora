using Microsoft.AspNetCore.Mvc;
using Shelfora.Services;

namespace Shelfora.Controllers;

public sealed class HomeController(ILibraryService library) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await library.DashboardAsync(cancellationToken));

    public IActionResult Error() => View();
}
