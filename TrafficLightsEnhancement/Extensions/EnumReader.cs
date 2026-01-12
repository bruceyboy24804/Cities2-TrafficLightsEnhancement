
namespace C2VM.TrafficLightsEnhancement.Extensions
{
    using Colossal.UI.Binding;

    public class EnumReader<T> : IReader<T>
    {
        public void Read(IJsonReader reader, out T value)
        {
            reader.Read(out int value2);
            value = (T)(object)value2;
        }
    }
}
