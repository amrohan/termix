using System.Text.Json.Serialization;
using termix.models;

namespace termix.Services;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(TermixConfig))]
internal partial class ConfigJsonContext : JsonSerializerContext
{
}