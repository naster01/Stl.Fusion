using Castle.DynamicProxy;
using Stl.Interception.Interceptors;

namespace Stl.Interception;

public interface ITypeViewFactory
{
    object CreateView(object implementation, Type implementationType, Type viewType);
    TypeViewFactory<TView> For<TView>()
        where TView : class;
}

public class TypeViewFactory : ITypeViewFactory
{
    public static ITypeViewFactory Default { get; } =
        new TypeViewFactory(
            TypeViewProxyGenerator.Default,
            new TypeViewInterceptor(DependencyInjection.ServiceProviderExt.Empty));

    protected TypeViewProxyGenerator ProxyGenerator { get; }
    protected IInterceptor[] Interceptors { get; }

    public TypeViewFactory(
        TypeViewProxyGenerator proxyGenerator,
        TypeViewInterceptor interceptor)
    {
        ProxyGenerator = proxyGenerator;
        Interceptors = new IInterceptor[] { interceptor };
    }

    public object CreateView(object implementation, Type implementationType, Type viewType)
    {
        if (!(implementationType.IsClass || implementationType.IsInterface))
            throw new ArgumentOutOfRangeException(nameof(implementationType));
        if (!viewType.IsInterface)
            throw new ArgumentOutOfRangeException(nameof(viewType));
        var proxyType = ProxyGenerator.GetProxyType(implementationType, viewType);
        var view = (TypeView) proxyType.CreateInstance(Interceptors, (object?) null);
        view.ViewTarget = implementation;
        return view;
    }

    public TypeViewFactory<TView> For<TView>()
        where TView : class
        => new(this);
}

public readonly struct TypeViewFactory<TView>
    where TView : class
{
    public ITypeViewFactory Factory { get; }

    public TypeViewFactory(ITypeViewFactory factory) => Factory = factory;

    public TView CreateView(Type implementationType, object implementation)
        => (TView) Factory.CreateView(implementation, implementationType, typeof(TView));

    public TView CreateView<TImplementation>(TImplementation implementation)
        where TImplementation : class
        => (TView) Factory.CreateView(implementation, typeof(TImplementation), typeof(TView));
}
