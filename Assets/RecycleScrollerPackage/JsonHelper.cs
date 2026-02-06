public static partial class JsonHelper
{
    public static bool TryDeserialize<T>(string json, out T result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(json)) return false;

        try
        {
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}