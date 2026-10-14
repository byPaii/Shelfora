using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shelfora.Data;

namespace Shelfora.Components;

public sealed class LibraryIdentityViewComponent(LibraryDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var name = await db.LibrarySettings.AsNoTracking().Select(x => x.LibraryName)
            .FirstOrDefaultAsync(HttpContext.RequestAborted);
        return Content(name ?? "Shelfora");
    }
}
