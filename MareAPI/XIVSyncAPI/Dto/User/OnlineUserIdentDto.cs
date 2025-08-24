using XIVSync.API.Data;
using MessagePack;

namespace XIVSync.API.Dto.User;

[MessagePackObject(keyAsPropertyName: true)]
public record OnlineUserIdentDto(UserData User, string Ident) : UserDto(User);