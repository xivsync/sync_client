﻿using System;
using System.Numerics;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Microsoft.Extensions.Logging;
using XIVSync.API.Dto.CharaData;
using XIVSync.Services;

namespace XIVSync.Interop.Ipc;

public sealed class IpcCallerBrio : IIpcCaller, IDisposable
{
    private readonly ILogger<IpcCallerBrio> _logger;
    private readonly DalamudUtilService _dalamudUtilService;

    private readonly ICallGateSubscriber<(int, int)> _brioApiVersion;

    private readonly ICallGateSubscriber<bool, bool, bool, Task<IGameObject?>> _brioSpawnExAsync;
    private readonly ICallGateSubscriber<Task<IGameObject?>> _brioSpawnAsync;
    private readonly ICallGateSubscriber<IGameObject?> _brioSpawn;

    private readonly ICallGateSubscriber<IGameObject, bool> _brioDespawnActor;
    private readonly ICallGateSubscriber<IGameObject, Vector3?, Quaternion?, Vector3?, bool, bool> _brioSetModelTransform;
    private readonly ICallGateSubscriber<IGameObject, (Vector3?, Quaternion?, Vector3?)> _brioGetModelTransform;
    private readonly ICallGateSubscriber<IGameObject, string?> _brioGetPoseAsJson;
    private readonly ICallGateSubscriber<IGameObject, string, bool, bool> _brioSetPoseFromJson;
    private readonly ICallGateSubscriber<IGameObject, bool> _brioFreezeActor;
    private readonly ICallGateSubscriber<bool> _brioFreezePhysics;

    public bool APIAvailable { get; private set; }

    public IpcCallerBrio(ILogger<IpcCallerBrio> logger, IDalamudPluginInterface pi, DalamudUtilService dalamudUtilService)
    {
        _logger = logger;
        _dalamudUtilService = dalamudUtilService;

        _brioApiVersion = pi.GetIpcSubscriber<(int, int)>("Brio.ApiVersion");
        _brioSpawnExAsync = pi.GetIpcSubscriber<bool, bool, bool, Task<IGameObject?>>("Brio.Actor.SpawnExAsync");
        _brioSpawnAsync = pi.GetIpcSubscriber<Task<IGameObject?>>("Brio.Actor.SpawnAsync");
        _brioSpawn = pi.GetIpcSubscriber<IGameObject?>("Brio.Actor.Spawn");
        _brioDespawnActor = pi.GetIpcSubscriber<IGameObject, bool>("Brio.Actor.Despawn");
        _brioSetModelTransform = pi.GetIpcSubscriber<IGameObject, Vector3?, Quaternion?, Vector3?, bool, bool>("Brio.Actor.SetModelTransform");
        _brioGetModelTransform = pi.GetIpcSubscriber<IGameObject, (Vector3?, Quaternion?, Vector3?)>("Brio.Actor.GetModelTransform");
        _brioGetPoseAsJson = pi.GetIpcSubscriber<IGameObject, string?>("Brio.Actor.Pose.GetPoseAsJson");
        _brioSetPoseFromJson = pi.GetIpcSubscriber<IGameObject, string, bool, bool>("Brio.Actor.Pose.LoadFromJson");
        _brioFreezeActor = pi.GetIpcSubscriber<IGameObject, bool>("Brio.Actor.Freeze");
        _brioFreezePhysics = pi.GetIpcSubscriber<bool>("Brio.FreezePhysics");

        CheckAPI();
    }

    public void CheckAPI()
    {
        try
        {
            var (major, minor) = _brioApiVersion.InvokeFunc();
            APIAvailable = major >= 2;
            _logger.LogDebug("Brio.ApiVersion = {Major}.{Minor}, APIAvailable={Avail}", major, minor, APIAvailable);
        }
        catch
        {
            APIAvailable = false;
        }
    }

    public async Task<IGameObject?> SpawnActorAsync()
    {
        if (!APIAvailable) return null;
        try
        {
            var ex = await _brioSpawnExAsync.InvokeFunc(false, false, true).ConfigureAwait(false);
            if (ex != null) return ex;
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "Brio.Actor.SpawnExAsync failed");
        }

        try
        {
            var a = await _brioSpawnAsync.InvokeFunc().ConfigureAwait(false);
            if (a != null) return a;
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "Brio.Actor.SpawnAsync failed");
        }

        try
        {
            return _brioSpawn.InvokeFunc();
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Brio.Actor.Spawn failed");
            return null;
        }
    }

    public async Task<bool> DespawnActorAsync(nint address)
    {
        if (!APIAvailable) return false;
        var gameObject = await _dalamudUtilService.CreateGameObjectAsync(address).ConfigureAwait(false);
        if (gameObject == null) return false;
        try
        {
            return await _dalamudUtilService.RunOnFrameworkThread(() => _brioDespawnActor.InvokeFunc(gameObject)).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Brio.Actor.Despawn failed");
            return false;
        }
    }

    public async Task<bool> ApplyTransformAsync(nint address, WorldData data)
    {
        if (!APIAvailable) return false;
        var gameObject = await _dalamudUtilService.CreateGameObjectAsync(address).ConfigureAwait(false);
        if (gameObject == null) return false;
        try
        {
            return await _dalamudUtilService.RunOnFrameworkThread(() => _brioSetModelTransform.InvokeFunc(
                gameObject,
                new Vector3(data.PositionX, data.PositionY, data.PositionZ),
                new Quaternion(data.RotationX, data.RotationY, data.RotationZ, data.RotationW),
                new Vector3(data.ScaleX, data.ScaleY, data.ScaleZ),
                false
            )).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Brio.Actor.SetModelTransform failed");
            return false;
        }
    }

    public async Task<WorldData> GetTransformAsync(nint address)
    {
        if (!APIAvailable) return default;
        var gameObject = await _dalamudUtilService.CreateGameObjectAsync(address).ConfigureAwait(false);
        if (gameObject == null) return default;
        try
        {
            var data = await _dalamudUtilService.RunOnFrameworkThread(() => _brioGetModelTransform.InvokeFunc(gameObject)).ConfigureAwait(false);
            if (!data.Item1.HasValue || !data.Item2.HasValue || !data.Item3.HasValue) return default;
            return new WorldData
            {
                PositionX = data.Item1.Value.X,
                PositionY = data.Item1.Value.Y,
                PositionZ = data.Item1.Value.Z,
                RotationX = data.Item2.Value.X,
                RotationY = data.Item2.Value.Y,
                RotationZ = data.Item2.Value.Z,
                RotationW = data.Item2.Value.W,
                ScaleX = data.Item3.Value.X,
                ScaleY = data.Item3.Value.Y,
                ScaleZ = data.Item3.Value.Z
            };
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Brio.Actor.GetModelTransform failed");
            return default;
        }
    }

    public async Task<string?> GetPoseAsync(nint address)
    {
        if (!APIAvailable) return null;
        var gameObject = await _dalamudUtilService.CreateGameObjectAsync(address).ConfigureAwait(false);
        if (gameObject == null) return null;
        try
        {
            return await _dalamudUtilService.RunOnFrameworkThread(() => _brioGetPoseAsJson.InvokeFunc(gameObject)).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Brio.Actor.Pose.GetPoseAsJson failed");
            return null;
        }
    }

    public async Task<bool> SetPoseAsync(nint address, string pose)
    {
        if (!APIAvailable) return false;
        var gameObject = await _dalamudUtilService.CreateGameObjectAsync(address).ConfigureAwait(false);
        if (gameObject == null) return false;

        try
        {
            var applicablePose = JsonNode.Parse(pose)!;
            var currentPose = await _dalamudUtilService.RunOnFrameworkThread(() => _brioGetPoseAsJson.InvokeFunc(gameObject)).ConfigureAwait(false);
            if (currentPose is null) return false;

            var current = JsonNode.Parse(currentPose)!;
            if (current["ModelDifference"] is not null)
                applicablePose["ModelDifference"] = JsonNode.Parse(current["ModelDifference"]!.ToJsonString());

            await _dalamudUtilService.RunOnFrameworkThread(() =>
            {
                _brioFreezeActor.InvokeFunc(gameObject);
                _brioFreezePhysics.InvokeFunc();
            }).ConfigureAwait(false);

            return await _dalamudUtilService.RunOnFrameworkThread(() =>
                _brioSetPoseFromJson.InvokeFunc(gameObject, applicablePose.ToJsonString(), false)
            ).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Brio.Actor.Pose.LoadFromJson failed");
            return false;
        }
    }

    public void Dispose()
    {
    }
}
