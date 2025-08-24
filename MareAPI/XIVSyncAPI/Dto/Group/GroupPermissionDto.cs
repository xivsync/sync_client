using XIVSync.API.Data;
using XIVSync.API.Data.Enum;
using MessagePack;

namespace XIVSync.API.Dto.Group;

[MessagePackObject(keyAsPropertyName: true)]
public record GroupPermissionDto(GroupData Group, GroupPermissions Permissions) : GroupDto(Group);