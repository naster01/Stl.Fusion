namespace Stl.Serialization;

public static class NewtonsoftJsonSerialized
{
    public static NewtonsoftJsonSerialized<TValue> New<TValue>() => new();
    public static NewtonsoftJsonSerialized<TValue> New<TValue>(TValue value) => new() { Value = value };
    public static NewtonsoftJsonSerialized<TValue> New<TValue>(string data) => new(data);
}

[DataContract]
[Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptOut)]
public class NewtonsoftJsonSerialized<T> : TextSerialized<T>
{
    [ThreadStatic] private static ITextSerializer<T>? _serializer;

    public NewtonsoftJsonSerialized() { }
    public NewtonsoftJsonSerialized(string data) : base(data) { }

    protected override ITextSerializer<T> GetSerializer()
        => _serializer ??= new NewtonsoftJsonSerializer().ToTyped<T>();
}
