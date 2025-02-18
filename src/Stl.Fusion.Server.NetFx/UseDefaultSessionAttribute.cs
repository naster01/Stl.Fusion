using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Stl.Fusion.Authentication;

namespace Stl.Fusion.Server;

public class UseDefaultSessionAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(HttpActionContext actionContext)
    {
        if (actionContext.Request.Method != HttpMethod.Post)
            return;
        foreach (var (_, argument) in actionContext.ActionArguments) {
            if (argument is ISessionCommand command && command.Session.IsDefault()) {
                var services = actionContext.GetAppServices();
                var sessionProvider = services.GetRequiredService<ISessionProvider>();
                command.UseDefaultSession(sessionProvider);
            }
        }
    }
}
