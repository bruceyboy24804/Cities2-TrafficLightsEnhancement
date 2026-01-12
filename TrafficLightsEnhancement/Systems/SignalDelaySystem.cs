using C2VM.TrafficLightsEnhancement.Components;
using Colossal.Entities;
using Unity.Collections;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Systems;

public partial class SignalDelaySystem : SystemBase
{
    protected override void OnUpdate()
    {
        
    }

    public static void SetSignalDelay(EntityManager entityManager, Entity junctionEntity, Entity edgeEntity, int openDelay, int closeDelay, bool isEnabled = true)
    {
        if (!entityManager.Exists(junctionEntity) || !entityManager.Exists(edgeEntity))
            return;

        DynamicBuffer<SignalDelayData> signalDelayBuffer;
        if (!entityManager.TryGetBuffer(junctionEntity, false, out signalDelayBuffer))
        {
            signalDelayBuffer = entityManager.AddBuffer<SignalDelayData>(junctionEntity);
        }

        bool found = false;
        for (int i = 0; i < signalDelayBuffer.Length; i++)
        {
            if (signalDelayBuffer[i].m_Edge.Equals(edgeEntity))
            {
                var updatedDelay = signalDelayBuffer[i];
                updatedDelay.m_OpenDelay = openDelay;
                updatedDelay.m_CloseDelay = closeDelay;
                updatedDelay.m_IsEnabled = isEnabled;
                signalDelayBuffer[i] = updatedDelay;
                found = true;
                break;
            }
        }

        if (!found)
        {
            var newDelay = new SignalDelayData(edgeEntity, openDelay, closeDelay, isEnabled);
            signalDelayBuffer.Add(newDelay);
        }
    }

    public static void RemoveSignalDelay(EntityManager entityManager, Entity junctionEntity, Entity edgeEntity)
    {
        if (!entityManager.Exists(junctionEntity) || !entityManager.Exists(edgeEntity))
            return;

        if (entityManager.TryGetBuffer(junctionEntity, false, out DynamicBuffer<SignalDelayData> signalDelayBuffer))
        {
            for (int i = 0; i < signalDelayBuffer.Length; i++)
            {
                if (signalDelayBuffer[i].m_Edge.Equals(edgeEntity))
                {
                    signalDelayBuffer.RemoveAt(i);
                    break;
                }
            }
        }
    }

    public static SignalDelayData? GetSignalDelayForEdge(EntityManager entityManager, Entity junctionEntity, Entity edgeEntity)
    {
        if (!entityManager.Exists(junctionEntity) || !entityManager.Exists(edgeEntity))
            return null;

        if (entityManager.TryGetBuffer(junctionEntity, false, out DynamicBuffer<SignalDelayData> signalDelayBuffer))
        {
            for (int i = 0; i < signalDelayBuffer.Length; i++)
            {
                if (signalDelayBuffer[i].m_Edge.Equals(edgeEntity))
                {
                    return signalDelayBuffer[i];
                }
            }
        }

        return null;
    }

    public static NativeArray<SignalDelayData> GetAllSignalDelays(EntityManager entityManager, Entity junctionEntity, Allocator allocator)
    {
        if (!entityManager.Exists(junctionEntity))
            return new NativeArray<SignalDelayData>(0, allocator);

        if (entityManager.TryGetBuffer(junctionEntity, false, out DynamicBuffer<SignalDelayData> signalDelayBuffer))
        {
            var result = new NativeArray<SignalDelayData>(signalDelayBuffer.Length, allocator);
            for (int i = 0; i < signalDelayBuffer.Length; i++)
            {
                result[i] = signalDelayBuffer[i];
            }
            return result;
        }

        return new NativeArray<SignalDelayData>(0, allocator);
    }
}
