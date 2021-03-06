﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PersonalWebsite.Models;
using PersonalWebsite.Services;

namespace PersonalWebsite.Controllers
{
    [ResponseCache(VaryByHeader = "User-Agent", Duration = 60 * 60 * 8)]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [Route("/Awards")]
        public IActionResult Awards()
        {
            return View();
        }

        [Route("/Projects")]
        public IActionResult Projects()
        {
            return View();
        }

        [Route("/privacy-policy")]
        public IActionResult PrivacyPolicy()
        {
            return View();
        }

        [Route("/jcli-d-cli-framework")]
        public IActionResult ProjectJcli()
        {
            return View("/Views/Projects/Jcli.cshtml");
        }

        [Route("/farmmaster")]
        public IActionResult ProjectFarmMaster()
        {
            return View("/Views/Projects/FarmMaster.cshtml");
        }

        [Route("/sitemap.xml")]
        public IActionResult Sitemap([FromServices] ISitemapGenerator sitemap)
        {
            return new ContentResult
            {
                Content     = sitemap.GenerateSitemap().ToString(),
                ContentType = "text/xml",
                StatusCode  = 200
            };
        }

        [Route("/Blog/")]
        [Route("/Blog/{seriesRef}")]
        public IActionResult Blog(string seriesRef, string tag, [FromServices] IBlogProvider blogs)
        {
            return View(new BlogIndexViewModel
            {
                Series = blogs.GetBlogSeries()
                              .Where(s => seriesRef == null || s.Series.Reference == seriesRef)
                              .Where(s => tag == null       || s.Series.Tags.Contains(tag)),
                TagFilter = tag
            });
        }

        [Route("/BlogPost/{seriesRef}/{postIndex}")]
        public IActionResult BlogPost(string seriesRef, string postIndex, [FromServices] IBlogProvider blogs)
        {
            var series = blogs.GetBlogSeries().FirstOrDefault(s => s.Series.Reference == seriesRef);
            if(series == null)
                return Redirect("/Blog");

            BlogPost lastBlog    = null;
            BlogPost currentBlog = null;
            BlogPost nextBlog    = null;

            var postIndexNumString = postIndex.Split('-').FirstOrDefault(); // Allows SEO keywords after the post index.
            var isNumber = Int32.TryParse(postIndexNumString, out int postIndexValue);

            if(!isNumber)
                return Redirect($"/Blog/{seriesRef}");

            if(postIndexValue > 0 && series.Posts.Count > 1)
                lastBlog = series.Posts[postIndexValue - 1];

            if(postIndexValue < series.Posts.Count)
                currentBlog = series.Posts[postIndexValue];
            else
                return Redirect($"/Blog/{seriesRef}");

            if(postIndexValue < series.Posts.Count - 1)
                nextBlog = series.Posts[postIndexValue + 1];

            return View(new BlogPostViewModel
            {
                Series      = series.Series,
                LastPost    = lastBlog,
                CurrentPost = currentBlog,
                NextPost    = nextBlog
            });
        }
    }
}