using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shelfora.Data;
using Shelfora.Models;

namespace Shelfora.Controllers;

public sealed class MembersController(LibraryDbContext db, ILogger<MembersController> logger) : Controller
{
    public async Task<IActionResult> Index(string? q, string? status, string? sort, int page = 1, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        const int size = 15;
        var query = db.Members.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.FullName.Contains(q) || x.MemberCode.Contains(q) || (x.Email != null && x.Email.Contains(q)));
        if (status == "active") query = query.Where(x => x.IsActive);
        if (status == "inactive") query = query.Where(x => !x.IsActive);
        query = sort switch
        {
            "date" => query.OrderByDescending(x => x.MembershipDate),
            "code" => query.OrderBy(x => x.MemberCode),
            _ => query.OrderBy(x => x.FullName)
        };
        ViewBag.Page = page; ViewBag.TotalPages = (int)Math.Ceiling(await query.CountAsync(ct) / (double)size); ViewBag.Query = q; ViewBag.Status = status; ViewBag.Sort = sort;
        return View(await query.Skip((Math.Max(page, 1) - 1) * size).Take(size).ToListAsync(ct));
    }
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var member = await db.Members.AsNoTracking().Include(x => x.Transactions).ThenInclude(x => x.Book)
            .Include(x => x.Transactions).ThenInclude(x => x.Returns)
            .SingleOrDefaultAsync(x => x.Id == id, ct);
        return member is null ? NotFound() : View(member);
    }
    public IActionResult Create() => View(new Member { IsActive = true, MembershipDate = DateTime.Today });
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Member member, CancellationToken ct)
    {
        if (await db.Members.AnyAsync(x => x.MemberCode == member.MemberCode, ct)) ModelState.AddModelError(nameof(member.MemberCode), "Member identifier is already in use.");
        if (!ModelState.IsValid) return View(member);
        member.Id = 0;
        member.Transactions.Clear();
        member.CreatedAtUtc = member.UpdatedAtUtc = DateTime.UtcNow;
        db.Members.Add(member);
        try { await db.SaveChangesAsync(ct); TempData["Success"] = "Member registered."; return RedirectToAction(nameof(Index)); }
        catch (DbUpdateException ex) { logger.LogError(ex, "Could not create member"); ModelState.AddModelError("", "Could not save the member. Check for a duplicate identifier."); return View(member); }
    }
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var member = await db.Members.FindAsync([id], ct);
        return member is null ? NotFound() : View(member);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Member input, CancellationToken ct)
    {
        if (id != input.Id) return BadRequest();
        var member = await db.Members.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (member is null) return NotFound();
        if (await db.Members.AnyAsync(x => x.MemberCode == input.MemberCode && x.Id != id, ct)) ModelState.AddModelError(nameof(input.MemberCode), "Member identifier is already in use.");
        if (member.IsActive && !input.IsActive && await db.BorrowTransactions.AnyAsync(x => x.MemberId == id && x.QuantityReturned < x.Quantity, ct))
            ModelState.AddModelError(nameof(input.IsActive), "Membership cannot be deactivated while this member has unreturned books.");
        if (!ModelState.IsValid) return View(input);
        member.MemberCode = input.MemberCode; member.FullName = input.FullName; member.Email = input.Email;
        member.ContactNumber = input.ContactNumber; member.Address = input.Address; member.MembershipDate = input.MembershipDate;
        member.IsActive = input.IsActive; member.Notes = input.Notes; member.UpdatedAtUtc = DateTime.UtcNow;
        try { await db.SaveChangesAsync(ct); TempData["Success"] = "Member updated."; return RedirectToAction(nameof(Details), new { id }); }
        catch (DbUpdateException ex) { logger.LogError(ex, "Could not update member {MemberId}", id); ModelState.AddModelError("", "Could not save member changes."); return View(input); }
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var member = await db.Members.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (member is null) return NotFound();
        if (await db.BorrowTransactions.AnyAsync(x => x.MemberId == id, ct)) TempData["Error"] = "Member has borrowing history; preserve the history and deactivate the membership instead.";
        else { db.Members.Remove(member); await db.SaveChangesAsync(ct); TempData["Success"] = "Member deleted."; }
        return RedirectToAction(nameof(Index));
    }
}
