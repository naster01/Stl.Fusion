using Stl.Requirements;

namespace Stl;

public abstract record Requirement
{
    public abstract bool IsSatisfiedUntyped(object? value);
    public abstract object RequireUntyped(object? value);

    public static FuncRequirement<T> New<T>(ExceptionBuilder exceptionBuilder, Func<T?, bool> validator) 
        => new(exceptionBuilder, validator);
    public static FuncRequirement<T> New<T>(Func<T?, bool> validator) 
        => new(validator);
}

public abstract record Requirement<T> : Requirement
{
    private const string MustExistPropertyName = "MustExist";
    // ReSharper disable once StaticMemberInGenericType
    private static readonly object MustExistLock = new();
    private static volatile Requirement<T>? _mustExist;

    public static Requirement<T> MustExist {
        get {
            if (_mustExist != null)
                return _mustExist;
            lock (MustExistLock) {
                if (_mustExist != null)
                    return _mustExist;

                var type = typeof(T);
                var result = type
                    .GetProperty(MustExistPropertyName, BindingFlags.Public | BindingFlags.Static)
                    ?.GetValue(null) as Requirement<T>;
                result ??= type
                    .GetField(MustExistPropertyName, BindingFlags.Public | BindingFlags.Static)
                    ?.GetValue(null) as Requirement<T>;
                result ??= MustExistRequirement<T>.Default;
                return _mustExist = result;
            }
        }
    }

    public override bool IsSatisfiedUntyped(object? value)
        => IsSatisfied((T?) value);
    public override object RequireUntyped(object? value)
#pragma warning disable CS8603
        => Check((T?) value);
#pragma warning restore CS8603

    public abstract bool IsSatisfied(T? value);
    public abstract T Check(T? value);

    public Requirement<T> And(Requirement<T> secondary)
        => new JointRequirement<T>(this, secondary);

    public Requirement<T> With(ExceptionBuilder exceptionBuilder)
        => this is CustomizableRequirementBase<T> customizableRequirement
            ? customizableRequirement with { ExceptionBuilder = exceptionBuilder }
            : new CustomizableRequirement<T>(this, exceptionBuilder);
    public Requirement<T> With(string messageTemplate, Func<string, Exception>? exceptionFactory = null)
        => With(new ExceptionBuilder(messageTemplate, exceptionFactory));
    public Requirement<T> With(string messageTemplate, string targetName, Func<string, Exception>? exceptionFactory = null)
        => With(new ExceptionBuilder(messageTemplate, targetName, exceptionFactory));
    public Requirement<T> With(Func<Exception> exceptionFactory)
        => With(new ExceptionBuilder(exceptionFactory));

    public static implicit operator Requirement<T>((ExceptionBuilder ExceptionBuilder, Func<T?, bool> Validator) args)
        => New(args.ExceptionBuilder, args.Validator);
    public static implicit operator Requirement<T>(Func<T?, bool> validator)
        => New(validator);

    public static Requirement<T> operator &(Requirement<T> primary, Requirement<T> secondary)
        => primary.And(secondary);
    public static Requirement<T> operator +(Requirement<T> requirement, ExceptionBuilder exceptionBuilder)
        => requirement.With(exceptionBuilder);
    public static Requirement<T> operator +(Requirement<T> requirement, string messageTemplate)
        => requirement.With(messageTemplate);
    public static Requirement<T> operator +(Requirement<T> requirement, Func<Exception> exceptionBuilder)
        => requirement.With(exceptionBuilder);
}
