using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ContestantBE.Attribute;

/// <summary>
/// Attribute to ensure user has selected a contest (contestId > 0 in JWT)
/// </summary>
public class RequireContestAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var contestIdClaim = context.HttpContext.User.FindFirst("contestId");
        
        if (contestIdClaim == null || !int.TryParse(contestIdClaim.Value, out var contestId) || contestId <= 0)
        {
            context.Result = new BadRequestObjectResult(new
            {
                success = false,
                error = "No contest selected. Please select a contest first.",
                requireContestSelection = true
            });
            return;
        }

        base.OnActionExecuting(context);
    }
}
