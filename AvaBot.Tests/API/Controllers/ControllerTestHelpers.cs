using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AvaBot.Tests.API.Controllers;

public static class ControllerTestHelpers
{
    /// <summary>Simula o usuario autenticado do token (claim sub mapeada para NameIdentifier).</summary>
    public static T WithUser<T>(this T controller, long userId) where T : ControllerBase
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            "TestAuth");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        return controller;
    }

    /// <summary>Requisicao sem claim de usuario (token antigo, anterior a feature 016).</summary>
    public static T WithoutUser<T>(this T controller) where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
        };

        return controller;
    }
}
