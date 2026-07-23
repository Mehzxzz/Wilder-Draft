using AmongUs.GameOptions;
using Reactor.Networking.Attributes;

namespace WilderDraft;

public static class Networking
{
    [MethodRpc((uint)RpcCalls.SetRole)]
    public static void RpcCustomSetRole(this PlayerControl p, uint role)
    {
        RoleManager.Instance.SetRole(p, (RoleTypes) role);
    }
}

public enum RpcCalls
{
    SetRole
}