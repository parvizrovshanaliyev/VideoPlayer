using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VideoPlayer.WebAppMvc.Models;

namespace VideoPlayer.WebAppMvc.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }
    
    // Action to serve the video player
    public IActionResult VideoPlayer(string videoFileName)
    {
        if (string.IsNullOrEmpty(videoFileName))
        {
            videoFileName = "sample.mp4"; // Default video file
        }

        ViewData["VideoFileName"] = videoFileName;
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
