using XIVSync.API.Data;
using XIVSync.API.Data.Enum;
using MessagePack;

namespace XIVSync.API.Dto.User;

[MessagePackObject(keyAsPropertyName: true)]
public record UserFullPairDto(UserData User, IndividualPairStatus IndividualPairStatus, List<string> Groups, UserPermissions OwnPermissions, UserPermissions OtherPermissions) : UserDto(User)
{
    public UserPermissions OwnPermissions { get; set; } = OwnPermissions;
    public UserPermissions OtherPermissions { get; set; } = OtherPermissions;
    public IndividualPairStatus IndividualPairStatus { get; set; } = IndividualPairStatus;
}

[MessagePackObject(keyAsPropertyName: true)]
public record UserPairDto(UserData User, IndividualPairStatus IndividualPairStatus, UserPermissions OwnPermissions, UserPermissions OtherPermissions) : UserDto(User)
{
    public UserPermissions OwnPermissions { get; set; } = OwnPermissions;
    public UserPermissions OtherPermissions { get; set; } = OtherPermissions;
    public IndividualPairStatus IndividualPairStatus { get; set; } = IndividualPairStatus;
}