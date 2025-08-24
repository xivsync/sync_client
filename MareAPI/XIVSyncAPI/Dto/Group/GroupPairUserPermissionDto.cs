using XIVSync.API.Data;
using XIVSync.API.Data.Enum;
using MessagePack;

namespace XIVSync.API.Dto.Group;

[MessagePackObject(keyAsPropertyName: true)]
public record GroupPairUserPermissionDto(GroupData Group, UserData User, GroupUserPreferredPermissions GroupPairPermissions) : GroupPairDto(Group, User);