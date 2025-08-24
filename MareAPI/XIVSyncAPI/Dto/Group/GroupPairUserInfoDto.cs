using XIVSync.API.Data;
using XIVSync.API.Data.Enum;
using MessagePack;

namespace XIVSync.API.Dto.Group;

[MessagePackObject(keyAsPropertyName: true)]
public record GroupPairUserInfoDto(GroupData Group, UserData User, GroupPairUserInfo GroupUserInfo) : GroupPairDto(Group, User);