using C2VM.TrafficLightsEnhancement.Systems.Serialization;
using Colossal.Serialization.Entities;
using Game.Net;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace C2VM.TrafficLightsEnhancement.Components;

public struct TrafficGroup : IComponentData, ISerializable
{
    public bool m_IsCoordinated;
    public bool m_GreenWaveEnabled;
    public float m_GreenWaveSpeed;
    public float m_GreenWaveOffset;
    public float m_MaxCoordinationDistance;
    
    public float m_CycleLength;       
    
    public float m_LastSyncTime;      
    public float m_CycleTimer;        
    
    public uint m_CreationTime;

    // Runtime-only master clock fields (not serialized)
    // Populated each frame by TrafficGroupSystem from the leader
    public byte m_MasterPhase;
    public byte m_MasterNextPhase;
    public TrafficLightState m_MasterState;
    public byte m_MasterTimer;
    public uint m_MasterCustomTimer;
    public byte m_MasterSignalGroupCount;

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        
        writer.Write(TLEDataVersion.V1);
        writer.Write(m_IsCoordinated);
        writer.Write(m_GreenWaveEnabled);
        writer.Write(m_GreenWaveSpeed);
        writer.Write(m_GreenWaveOffset);
        writer.Write(m_MaxCoordinationDistance);
        writer.Write(m_CreationTime);
        writer.Write(m_CycleLength);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        
        m_IsCoordinated = false;
        m_GreenWaveEnabled = false;
        m_GreenWaveSpeed = 50f;
        m_GreenWaveOffset = 0f;
        m_MaxCoordinationDistance = 500f;
        m_CreationTime = 0;
        m_CycleLength = 16f;
        m_LastSyncTime = 0f;
        m_CycleTimer = 0f;
        m_MasterPhase = 0;
        m_MasterNextPhase = 0;
        m_MasterState = TrafficLightState.None;
        m_MasterTimer = 0;
        m_MasterCustomTimer = 0;
        m_MasterSignalGroupCount = 0;
        
        reader.Read(out int version);
        reader.Read(out m_IsCoordinated);
        reader.Read(out m_GreenWaveEnabled);
        reader.Read(out m_GreenWaveSpeed);
        reader.Read(out m_GreenWaveOffset);
        reader.Read(out m_MaxCoordinationDistance);
        reader.Read(out m_CreationTime);
        reader.Read(out m_CycleLength); 
        
    }

    public TrafficGroup()
    {
        m_IsCoordinated = false;
        m_GreenWaveEnabled = false;
        m_GreenWaveSpeed = 50f;
        m_GreenWaveOffset = 0f;
        m_MaxCoordinationDistance = 500f;
        m_CreationTime = 0;
        m_CycleLength = 16f;
        m_LastSyncTime = 0f;
        m_CycleTimer = 0f;
        m_MasterPhase = 0;
        m_MasterNextPhase = 0;
        m_MasterState = TrafficLightState.None;
        m_MasterTimer = 0;
        m_MasterCustomTimer = 0;
        m_MasterSignalGroupCount = 0;
    }

    public TrafficGroup(bool isCoordinated = false, bool greenWaveEnabled = false, float greenWaveSpeed = 50f, float greenWaveOffset = 0f, float maxCoordinationDistance = 500f, float cycleLength = 16f)
    {
        m_IsCoordinated = isCoordinated;
        m_GreenWaveEnabled = greenWaveEnabled;
        m_GreenWaveSpeed = greenWaveSpeed;
        m_GreenWaveOffset = greenWaveOffset;
        m_MaxCoordinationDistance = maxCoordinationDistance;
        m_CreationTime = (uint)System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        m_CycleLength = cycleLength;
        m_LastSyncTime = 0f;
        m_CycleTimer = 0f;
        m_MasterPhase = 0;
        m_MasterNextPhase = 0;
        m_MasterState = TrafficLightState.None;
        m_MasterTimer = 0;
        m_MasterCustomTimer = 0;
        m_MasterSignalGroupCount = 0;
    }
}

