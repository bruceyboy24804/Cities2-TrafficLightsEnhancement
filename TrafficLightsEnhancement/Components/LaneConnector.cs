using System;
using Colossal.UI.Binding;
using Unity.Entities;
using Unity.Mathematics;

namespace C2VM.TrafficLightsEnhancement.Components;





[Flags]
public enum VehicleGroup : ushort
{
	None = 0,
	Car = 1 << 0,
	PublicCar = 1 << 1,
	Train = 1 << 2,
	Tram = 1 << 3,
	Subway = 1 << 4,
	Bike = 1 << 5,
	Pedestrian = 1 << 6,
	Highway = 1 << 7,

	
	TrackGroup = Train | Tram | Subway,
	AllCar = Car | PublicCar,
	AllVehicle = Car | PublicCar | TrackGroup | Bike,
}




public enum TurnType : byte
{
	Unknown = 0,
	Straight = 1,
	Left = 2,
	Right = 3,
	UTurn = 4,
	GentleLeft = 5,
	GentleRight = 6,
}




public enum ConnectorType : byte
{
	Source = 0,
	Target = 1,
	TwoWay = 2,
}





public struct LaneConnector : IBufferElementData, IJsonWritable
{
	public Entity m_Edge;
	public Entity m_SubLane;
	public Entity m_NodeSubLane;
	public int m_LaneIndex;
	public int2 m_CarriagewayAndGroupIndex;
	public float3 m_Position;
	public float3 m_Direction;
	public VehicleGroup m_VehicleGroup;
	public ConnectorType m_ConnectorType;
	public TurnType m_TurnType;
	public bool m_IsPublicOnly;
	public bool m_IsUnsafe;

	public LaneConnector(
		Entity edge,
		Entity subLane,
		Entity nodeSubLane,
		int laneIndex,
		int2 carriagewayAndGroupIndex,
		float3 position,
		float3 direction,
		VehicleGroup vehicleGroup,
		ConnectorType connectorType,
		TurnType turnType,
		bool isPublicOnly,
		bool isUnsafe)
	{
		m_Edge = edge;
		m_SubLane = subLane;
		m_NodeSubLane = nodeSubLane;
		m_LaneIndex = laneIndex;
		m_CarriagewayAndGroupIndex = carriagewayAndGroupIndex;
		m_Position = position;
		m_Direction = direction;
		m_VehicleGroup = vehicleGroup;
		m_ConnectorType = connectorType;
		m_TurnType = turnType;
		m_IsPublicOnly = isPublicOnly;
		m_IsUnsafe = isUnsafe;
	}

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin(typeof(LaneConnector).FullName);
		writer.PropertyName("m_Edge");
		writer.Write(m_Edge);
		writer.PropertyName("m_SubLane");
		writer.Write(m_SubLane);
		writer.PropertyName("m_NodeSubLane");
		writer.Write(m_NodeSubLane);
		writer.PropertyName("m_LaneIndex");
		writer.Write(m_LaneIndex);
		writer.PropertyName("m_CarriagewayIndex");
		writer.Write(m_CarriagewayAndGroupIndex.x);
		writer.PropertyName("m_GroupIndex");
		writer.Write(m_CarriagewayAndGroupIndex.y);
		writer.PropertyName("m_VehicleGroup");
		writer.Write((int)m_VehicleGroup);
		writer.PropertyName("m_ConnectorType");
		writer.Write((int)m_ConnectorType);
		writer.PropertyName("m_TurnType");
		writer.Write((int)m_TurnType);
		writer.PropertyName("m_IsPublicOnly");
		writer.Write(m_IsPublicOnly);
		writer.PropertyName("m_IsUnsafe");
		writer.Write(m_IsUnsafe);
		writer.TypeEnd();
	}
}





public struct ComputedLaneConnection : IJsonWritable
{
	public Entity m_SourceEdge;
	public Entity m_TargetEdge;
	public Entity m_SourceSubLane;
	public Entity m_TargetSubLane;
	public Entity m_NodeSubLane;
	public int m_SourceLaneIndex;
	public int m_TargetLaneIndex;
	public int4 m_CarriagewayAndGroupIndexMap; 
	public float3x2 m_LanePositionMap; 
	public float3 m_SourceDirection;
	public float3 m_TargetDirection;
	public TurnType m_TurnType;
	public VehicleGroup m_VehicleGroup;
	public bool m_IsPublicOnly;
	public bool m_IsUnsafe;
	public bool m_IsTwoWay;

	
	
	
	
	public float GetTurnAngle()
	{
		if (math.lengthsq(m_SourceDirection) < 0.001f || math.lengthsq(m_TargetDirection) < 0.001f)
		{
			return 0f;
		}
		float dot = math.dot(math.normalize(m_SourceDirection.xz), math.normalize(m_TargetDirection.xz));
		return math.degrees(math.acos(math.clamp(dot, -1f, 1f)));
	}

	
	
	
	public bool ConflictsWith(ComputedLaneConnection other)
	{
		
		if (m_SourceEdge == other.m_SourceEdge)
		{
			return false;
		}

		
		if (m_TurnType == TurnType.Straight && other.m_TurnType == TurnType.Straight)
		{
			
			if (m_TargetEdge != other.m_TargetEdge)
			{
				return true;
			}
		}

		
		if (m_TurnType == TurnType.Left && other.m_TurnType == TurnType.Straight)
		{
			if (m_TargetEdge == other.m_SourceEdge || other.m_TargetEdge == m_SourceEdge)
			{
				return true;
			}
		}

		return false;
	}

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin(typeof(ComputedLaneConnection).FullName);
		writer.PropertyName("m_SourceEdge");
		writer.Write(m_SourceEdge);
		writer.PropertyName("m_TargetEdge");
		writer.Write(m_TargetEdge);
		writer.PropertyName("m_SourceSubLane");
		writer.Write(m_SourceSubLane);
		writer.PropertyName("m_TargetSubLane");
		writer.Write(m_TargetSubLane);
		writer.PropertyName("m_NodeSubLane");
		writer.Write(m_NodeSubLane);
		writer.PropertyName("m_SourceLaneIndex");
		writer.Write(m_SourceLaneIndex);
		writer.PropertyName("m_TargetLaneIndex");
		writer.Write(m_TargetLaneIndex);
		writer.PropertyName("m_TurnType");
		writer.Write((int)m_TurnType);
		writer.PropertyName("m_VehicleGroup");
		writer.Write((int)m_VehicleGroup);
		writer.PropertyName("m_IsPublicOnly");
		writer.Write(m_IsPublicOnly);
		writer.PropertyName("m_IsUnsafe");
		writer.Write(m_IsUnsafe);
		writer.PropertyName("m_IsTwoWay");
		writer.Write(m_IsTwoWay);
		writer.TypeEnd();
	}
}
