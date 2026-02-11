namespace TestWebApi.Controllers;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("Test2")]
public class TestController2
{
    [Route("Test")]
    public string InvalidEndpointNoVerb()
    {
        return "Invalid";
    }

    public string InvalidEndpointNoRoute()
    {
        return "Invalid";
    }
}