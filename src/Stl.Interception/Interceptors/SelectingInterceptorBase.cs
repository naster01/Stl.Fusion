using Castle.DynamicProxy;
using Stl.Internal;

namespace Stl.Interception.Interceptors;

public abstract class SelectingInterceptorBase : InterceptorBase
{
    public new record Options : InterceptorBase.Options
    {
        public Type[] InterceptorTypes { get; init; } = Array.Empty<Type>();
    }

    protected IOptionalInterceptor[] Interceptors { get; }

    protected SelectingInterceptorBase(Options options, IServiceProvider services)
        : base(options, services)
    {
        Interceptors = new IOptionalInterceptor[options.InterceptorTypes.Length];
        for (var i = 0; i < options.InterceptorTypes.Length; i++)
            Interceptors[i] = (IOptionalInterceptor) services.GetRequiredService(options.InterceptorTypes[i]);
    }

    protected override Action<IInvocation>? CreateHandlerUntyped(MethodInfo methodInfo, IInvocation initialInvocation)
    {
        foreach (var interceptor in Interceptors) {
            var handler = interceptor.GetHandler(initialInvocation);
            if (handler != null)
                return handler;
        }
        return null;
    }

    protected override void ValidateTypeInternal(Type type)
    {
        foreach (var interceptor in Interceptors)
            interceptor.ValidateType(type);
    }

    protected override Action<IInvocation> CreateHandler<T>(IInvocation initialInvocation, MethodDef methodDef)
        => throw Errors.InternalError("This method shouldn't be called.");
    protected override MethodDef? CreateMethodDef(MethodInfo methodInfo, IInvocation initialInvocation)
        => throw Errors.InternalError("This method shouldn't be called.");
}
