﻿using MessagePack;

namespace XIVSync.API.Data;

[MessagePackObject(keyAsPropertyName: true)]
public record GroupData(string GID, string? Alias = null)
{
    [IgnoreMember]
    public string AliasOrGID => string.IsNullOrWhiteSpace(Alias) ? GID : Alias;
}