using C2VM.TrafficLightsEnhancement.Systems.Serialization;
using Colossal.Serialization.Entities;
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

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(uint.MaxValue);
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
        reader.Read(out uint marker);
        if (marker != uint.MaxValue) return; 
        
        reader.Read(out int version);
        if (version <= TLEDataVersion.V2)
        {
           reader.Read(out m_IsCoordinated);
           reader.Read(out m_GreenWaveEnabled);
           reader.Read(out m_GreenWaveSpeed);
           reader.Read(out m_GreenWaveOffset);
           reader.Read(out m_MaxCoordinationDistance);
           reader.Read(out m_CreationTime);
           reader.Read(out m_CycleLength); 
        }
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
    }
}

public struct TrafficGroupName : IComponentData, ISerializable
{
    public ulong NamePart1; 
    public ulong NamePart2; 
    public ulong NamePart3; 
    public ulong NamePart4; 
    public ulong NamePart5; 
    public ulong NamePart6; 
    public ulong NamePart7; 
    public ulong NamePart8; 

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        string name = GetName();
        writer.Write(name);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        reader.Read(out string name);
        SetName(name);
    }

    public TrafficGroupName()
    {
        NamePart1 = 0;
        NamePart2 = 0;
        NamePart3 = 0;
        NamePart4 = 0;
        NamePart5 = 0;
        NamePart6 = 0;
        NamePart7 = 0;
        NamePart8 = 0;
    }

    public TrafficGroupName(string name)
    {
        NamePart1 = 0;
        NamePart2 = 0;
        NamePart3 = 0;
        NamePart4 = 0;
        NamePart5 = 0;
        NamePart6 = 0;
        NamePart7 = 0;
        NamePart8 = 0;
        SetName(name);
    }

    public string GetName()
    {
        var chars = new char[64];
        int index = 0;
        
        for (int part = 0; part < 8; part++)
        {
            ulong value = part switch
            {
                0 => NamePart1,
                1 => NamePart2,
                2 => NamePart3,
                3 => NamePart4,
                4 => NamePart5,
                5 => NamePart6,
                6 => NamePart7,
                7 => NamePart8,
                _ => 0
            };
            
            for (int i = 0; i < 8 && index < 64; i++)
            {
                char c = (char)((value >> (i * 8)) & 0xFF);
                if (c == '\0')
                    goto Done;
                chars[index++] = c;
            }
        }
        
        Done:
        return new string(chars, 0, index);
    }

    public void SetName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return;
            
        int length = math.min(name.Length, 64);
        int charIndex = 0;
        for (int part = 0; part < 8 && charIndex < length; part++)
        {
            ulong value = 0;
            for (int i = 0; i < 8 && charIndex < length; i++)
            {
                value |= ((ulong)name[charIndex++] << (i * 8));
            }
            
            switch (part)
            {
                case 0: NamePart1 = value; break;
                case 1: NamePart2 = value; break;
                case 2: NamePart3 = value; break;
                case 3: NamePart4 = value; break;
                case 4: NamePart5 = value; break;
                case 5: NamePart6 = value; break;
                case 6: NamePart7 = value; break;
                case 7: NamePart8 = value; break;
            }
        }
    }
}
