using Microsoft.AspNetCore.Mvc;

namespace TestWebApi.Controllers;

[ApiController]
[Microsoft.AspNetCore.Components.Route("[controller]")]
public class TestController1
{
    [HttpGet]
    [Route("get")]
    public string ValidEndpointGet()
    {
        return "TEST!";
    }
    
    [HttpPut("put")]
    [HttpPost("post")]
    public string ValidEndpointPutAndPost()
    {
        return "TEST!";
    }
    
    [HttpPatch]
    [Route("patch")]
    public string ValidEndpointPatchWithRoute()
    {
        return "TEST!";
    }

    [HttpDelete("delete")]
    public string ValidEndpointDeleteWithRoute()
    {
        return "Test!";
    }
}