using Microsoft.AspNetCore.Mvc;

namespace TestWebApi.Controllers;

[ApiController]
[Route("Test3")]
public class TestController3
{
    [HttpGet]
    public string ValidEndpointWithGetParameter(string input)
    {
        return input;
    }

    [HttpGet("test/{id}")]
    public string ValidEndpointWithRouteParameter(string id)
    {
        return id;
    }

    public record ResponseModel(string Value1, int Value2);
    public record RequestModel(string Value1, int Value2);

    [HttpGet("1")]
    public ResponseModel ValidEndpointWithComplexTypeOutput()
    {
        return new ResponseModel("1", 1);
    }
    
    [HttpGet("2")]
    public string ValidEndpointWithComplexTypeInput(RequestModel input)
    {
        return "test";
    }
    
    [HttpGet("3")]
    public ResponseModel ValidEndpointWithComplexTypeInputAndOutput(RequestModel input)
    {
        return new ResponseModel("1", 1);
    }

    [HttpGet("4")]
    public string ValidEndpointWithBodyParameter([FromBody] string value)
    {
        return value;
    }

    [HttpGet("5")]
    [ProducesResponseType<OkResult>(200)]
    public IActionResult ValidEndpointWithActionResult()
    {
        return new OkResult();
    }

    [HttpGet("6")]
    [ProducesResponseType(typeof(ResponseModel), 200)]
    public IActionResult ValidEndpointWithComplexTypeActionResult()
    {
        return new OkObjectResult(new ResponseModel("1", 1));
    }
}