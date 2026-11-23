using System;
using System.Linq;
using System.Reflection;
using MultiplayerSFS.Common;
using SFS;
using SFS.UI;
using SFS.World;
using SFS.WorldBase;
using UnityEngine;

namespace MultiplayerSFS.Mod
{
    public static class TimeWarpVoting
    {
        public static bool IsTimeWarping { get; private set; } = false;

        public static float CurrentTimeScale { get; private set; } = 1f;

        public static bool CurrentPhysicsWarp { get; private set; } = false;

        /// <summary>
        /// 单机倍速逻辑执行完后调用：把 WorldTime 当前的真实状态同步到服务器。
        /// 这样联机的倍速序列、物理倍速（1/2/3/5）、慢动作（&lt;1）和最大倍速都和单机一致。
        /// </summary>
        public static void OnLocalTimewarpChanged()
        {
            if (!ClientManager.multiplayerEnabled.Value || WorldTime.main == null)
            {
                return;
            }

            float timeScale = (float)WorldTime.main.timewarpSpeed;
            bool physicsWarp = WorldTime.main.realtimePhysics.Value;

            if (ClientManager.IsHost)
            {
                IsTimeWarping = Mathf.Abs(timeScale - 1f) > 0.001f;
                CurrentTimeScale = timeScale;
                CurrentPhysicsWarp = physicsWarp;
            }

            ClientManager.SendPacket(new Packet_TimeWarpRequest
            {
                TimeScale = timeScale,
                RequesterName = (LocalManager.Player?.username ?? "Unknown"),
                PhysicsWarp = physicsWarp
            });

            // 非房主只能“请求”，不能本地生效：请求发出后立即恢复为服务器当前倍速。
            if (!ClientManager.IsHost)
            {
                ApplyTimeScale(CurrentTimeScale, CurrentPhysicsWarp);
            }
        }

        public static void OnVoteReceived(Packet_TimeWarpVote vote)
        {
            if (!ClientManager.multiplayerEnabled.Value)
            {
                return;
            }

            // 简化的自动同意：收到投票请求后自动投赞成票。
            // 如需真实投票 UI，可在此扩展为弹窗。
            ClientManager.SendPacket(new Packet_TimeWarpVoteResponse
            {
                VoteId = vote.VoteId,
                Agreed = true
            });
        }

        public static void OnVoteResultReceived(Packet_TimeWarpResult packet)
        {
            if (packet.Approved)
            {
                IsTimeWarping = Mathf.Abs(packet.TimeScale - 1f) > 0.001f;
                CurrentTimeScale = packet.TimeScale;
                CurrentPhysicsWarp = packet.PhysicsWarp;
                ApplyTimeScale(packet.TimeScale, packet.PhysicsWarp);

                if (packet.TimeScale == 1f && packet.VoteId == -1)
                {
                    if (!string.IsNullOrEmpty(packet.StopperName))
                    {
                        MsgDrawer.main.Log(packet.StopperName + MultiplayerLoc.L(" 结束了倍速", " ended time warp"));
                    }
                    else
                    {
                        MsgDrawer.main.Log(MultiplayerLoc.L("倍速已结束", "Time warp ended"));
                    }
                }
                else
                {
                    MsgDrawer.main.Log(MultiplayerLoc.L("倍速已设置为 ", "Time warp set to ") + packet.TimeScale + "x");
                }
            }
            else
            {
                IsTimeWarping = false;
                CurrentTimeScale = 1f;
                CurrentPhysicsWarp = false;
                ApplyTimeScale(1f, physicsWarp: true);
                MsgDrawer.main.Log(MultiplayerLoc.L("倍速请求被拒绝", "Time warp request rejected"));
            }
        }

        private static void ApplyTimeScale(float timeScale, bool physicsWarp)
        {
            if (WorldTime.main != null)
            {
                // 1x 和慢动作都是 realtimePhysics=true；物理倍速也是 true。
                bool realtimePhysics = timeScale <= 1f || physicsWarp;
                WorldTime.main.SetState(timeScale, realtimePhysics, showMsg: false);
                SyncLocalTimewarpIndex(timeScale);
            }
        }

        private static void SyncLocalTimewarpIndex(float timeScale)
        {
            if (WorldTime.main == null)
            {
                return;
            }

            if (timeScale < 1f)
            {
                float[] decelSpeeds = { 0.75f, 0.5f, 0.25f, 0.1f, 0.05f, 0.01f, 0.0f };
                int decelIndex = 0;
                for (int i = 0; i < decelSpeeds.Length; i++)
                {
                    if (Mathf.Abs(decelSpeeds[i] - timeScale) < 0.0001f)
                    {
                        decelIndex = i + 1;
                        break;
                    }
                }
                WorldTime.main.timewarpIndex = 0;
                // IL2CPP 兼容：原用反射 SetValue 私有字段，改为直接调用
                WorldTime.main.SetTimeDecelIndex(decelIndex);
                return;
            }

            // IL2CPP 兼容：原用反射 SetValue 私有字段，改为直接调用
            WorldTime.main.SetTimeDecelIndex(0);

            int maxPhysicsIndex = (Base.worldBase != null && Base.worldBase.settings != null)
                ? Base.worldBase.settings.difficulty.MaxPhysicsTimewarpIndex
                : 2;
            int maxIndex = maxPhysicsIndex + (int)Math.Log(
                (Base.planetLoader != null && Base.planetLoader.planets != null)
                    ? Base.planetLoader.planets.Values.Max((Planet a) => a.orbit?.period ?? 0.0) / 24.0
                    : 0.0, 4.72);

            int bestIndex = 0;
            double bestDelta = double.PositiveInfinity;
            for (int i = 0; i <= Math.Max(0, maxIndex); i++)
            {
                double speed = GetSinglePlayerSpeedForIndex(i, maxPhysicsIndex);
                double delta = Math.Abs(speed - timeScale);
                if (delta < bestDelta)
                {
                    bestDelta = delta;
                    bestIndex = i;
                }
            }
            WorldTime.main.timewarpIndex = bestIndex;
        }

        private static double GetSinglePlayerSpeedForIndex(int index, int maxPhysicsIndex)
        {
            if (index <= maxPhysicsIndex)
            {
                int[] physicsSpeeds = { 1, 2, 3, 5 };
                return physicsSpeeds[Math.Min(index, physicsSpeeds.Length - 1)];
            }

            int railIndex = index - maxPhysicsIndex;
            int[] railBase = { 1, 5, 25 };
            return railBase[railIndex % railBase.Length] * Math.Pow(100.0, railIndex / railBase.Length);
        }

        public static void Reset()
        {
            IsTimeWarping = false;
            CurrentTimeScale = 1f;
            CurrentPhysicsWarp = false;
            if (WorldTime.main != null)
            {
                WorldTime.main.SetState(1.0, realtimePhysics: true, showMsg: false);
            }
        }

        public static void SyncCurrentTimeWarp()
        {
            if (IsTimeWarping && WorldTime.main != null)
            {
                ApplyTimeScale(CurrentTimeScale, CurrentPhysicsWarp);
            }
        }
    }
}
