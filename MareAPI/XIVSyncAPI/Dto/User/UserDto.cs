using XIVSync.API.Data;
using MessagePack;

namespace XIVSync.API.Dto.User;

[MessagePackObject(keyAsPropertyName: true)]
public record UserDto(UserData User);