﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DGN.Data;
using DGN.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace DGN.Controllers
{
    public class ArticlesController : Controller
    {
        private readonly DGNContext _context;

        public ArticlesController(DGNContext context)
        {
            _context = context;
        }

        // GET: Articles
        public async Task<IActionResult> Index()
        {
            var dGNContext = _context.Article.Include(a => a.Category).Include(a => a.User).OrderByDescending(a => a.CreationTimestamp);
            return View(await dGNContext.Take(5).ToListAsync());
        }

        [Authorize]
        // GET: Articles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var article = await _context.Article
                .Include(a => a.Category)
                .Include(a => a.User)
                .Include(a => a.Comments)
                .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (article == null)
            {
                return NotFound();
            }

            return View(article);
        }

        // GET: Articles/Create
        [Authorize(Roles = "Author,Admin")]
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Category, "Id", "CategoryName");
            return View();
        }

        // POST: Articles/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Author,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Body,ImageLocation,CategoryId")] Article article)
        {
            if (ModelState.IsValid && !ArticleExists(article.Title))
            {
                article.CreationTimestamp = DateTime.Now;
                article.UserId = int.Parse(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
                _context.Add(article);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Category, "Id", "CategoryName", article.CategoryId);
            return View(article);
        }

        // GET: Articles/Edit/5
        [Authorize(Roles = "Author,Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var article = await _context.Article.FindAsync(id);
            if (article == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Category, "Id", "CategoryName", article.CategoryId);
            return View(article);
        }

        // POST: Articles/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Author,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Body,ImageLocation,CategoryId")] Article newArticle)
        {
            var currArticle = await _context.Article.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
            if ((currArticle == null) || (id != newArticle.Id))
            {
                return NotFound();
            }

            bool NotDuplicatedTitle = true;
            if (currArticle.Title != newArticle.Title)
            {
                NotDuplicatedTitle = !ArticleExists(newArticle.Title);
            }

            if ((ModelState.IsValid) && (NotDuplicatedTitle))
            {
                try
                {
                    _context.Update(newArticle);
                    newArticle.CreationTimestamp = currArticle.CreationTimestamp;
                    newArticle.UserId = currArticle.UserId;
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ArticleExists(newArticle.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Details), new { id = newArticle.Id });
            }
            ViewData["CategoryId"] = new SelectList(_context.Category, "Id", "CategoryName", newArticle.CategoryId);
            return View(newArticle);
        }

        // GET: Articles/Delete/5
        [Authorize(Roles = "Author,Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var article = await _context.Article
                .Include(a => a.Category)
                .Include(a => a.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (article == null)
            {
                return NotFound();
            }

            return View(article);
        }

        // POST: Articles/Delete/5
        [Authorize(Roles = "Author,Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var article = await _context.Article.FindAsync(id);
            _context.Article.Remove(article);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ArticleExists(int id)
        {
            return _context.Article.Any(e => e.Id == id);
        }

        private bool ArticleExists(string title)
        {
            return _context.Article.Any(e => e.Title == title);
        }

        public async Task<IActionResult> Search(string queryTitle)
        {
            return Json(await _context.Article.Where(a => (a.Title.Contains(queryTitle))).ToListAsync());
        }

        public async Task<IActionResult> GetMostCommentedArticles(int count)
        {
            var query = from comment in _context.Comment
                        join article in _context.Article on comment.RelatedArticleId equals article.Id
                        group comment by new { article.Id, article.Title, article.ImageLocation } into ArticleCommentsGroup
                        orderby ArticleCommentsGroup.Count() descending
                        select ArticleCommentsGroup.Key;

            return Json(await query.Take(count).ToListAsync());
        }

        public async Task<IActionResult> GetMostLikedArticles(int count)
        {
            return Json(await _context.Article.Include(a => a.UserLikes).OrderByDescending(a => a.UserLikes.Count()).Take(count).ToListAsync());
        }
    }
}