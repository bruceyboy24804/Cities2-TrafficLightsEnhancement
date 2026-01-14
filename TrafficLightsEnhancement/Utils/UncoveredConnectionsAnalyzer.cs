using C2VM.TrafficLightsEnhancement.Components;
using Colossal.UI.Binding;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace C2VM.TrafficLightsEnhancement.Utils;





public struct UncoveredConnectionsAnalyzer
{
	
	
	
	public struct UncoveredConnection : IJsonWritable
	{
		public Entity m_Edge;
		public float3 m_Position;
		public VehicleGroup m_VehicleGroup;
		public TurnType m_TurnType;
		public int m_LaneCount;

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(typeof(UncoveredConnection).FullName);
			writer.PropertyName("m_Edge");
			writer.Write(m_Edge);
			writer.PropertyName("m_Position");
			writer.Write(m_Position);
			writer.PropertyName("m_VehicleGroup");
			writer.Write((int)m_VehicleGroup);
			writer.PropertyName("m_TurnType");
			writer.Write((int)m_TurnType);
			writer.PropertyName("m_LaneCount");
			writer.Write(m_LaneCount);
			writer.TypeEnd();
		}
	}

	
	
	
	public struct AnalysisResult : IJsonWritable
	{
		public NativeList<UncoveredConnection> UncoveredConnections;
		public int TotalLaneConnections;
		public int UncoveredCount;
		public bool HasUncovered;

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(typeof(AnalysisResult).FullName);
			writer.PropertyName("totalLaneConnections");
			writer.Write(TotalLaneConnections);
			writer.PropertyName("uncoveredCount");
			writer.Write(UncoveredCount);
			writer.PropertyName("hasUncovered");
			writer.Write(HasUncovered);
			writer.PropertyName("uncoveredConnections");
			writer.ArrayBegin(UncoveredConnections.IsCreated ? UncoveredConnections.Length : 0);
			if (UncoveredConnections.IsCreated)
			{
				foreach (var connection in UncoveredConnections)
				{
					connection.Write(writer);
				}
			}
			writer.ArrayEnd();
			writer.TypeEnd();
		}

		public void Dispose()
		{
			if (UncoveredConnections.IsCreated)
			{
				UncoveredConnections.Dispose();
			}
		}
	}

	
	
	
	public static AnalysisResult FindUncoveredConnections(
		Allocator allocator,
		NativeList<NodeUtils.EdgeInfo> edgeInfoList)
	{
		NativeList<UncoveredConnection> uncovered = new(8, allocator);
		int totalConnections = 0;

		foreach (var edgeInfo in edgeInfoList)
		{
			
			CheckCarLanes(ref uncovered, ref totalConnections, edgeInfo, false);
			
			
			CheckPublicCarLanes(ref uncovered, ref totalConnections, edgeInfo);
			
			
			CheckTrackLanes(ref uncovered, ref totalConnections, edgeInfo);
			
			
			CheckPedestrianLanes(ref uncovered, ref totalConnections, edgeInfo);
			
			
			CheckBicycleLanes(ref uncovered, ref totalConnections, edgeInfo);
		}

		return new AnalysisResult
		{
			UncoveredConnections = uncovered,
			TotalLaneConnections = totalConnections,
			UncoveredCount = uncovered.Length,
			HasUncovered = uncovered.Length > 0
		};
	}

	private static void CheckCarLanes(
		ref NativeList<UncoveredConnection> uncovered,
		ref int totalConnections,
		NodeUtils.EdgeInfo edgeInfo,
		bool isPublicOnly)
	{
		var mask = edgeInfo.m_EdgeGroupMask;
		
		
		if (edgeInfo.m_CarLaneLeftCount > 0)
		{
			totalConnections += edgeInfo.m_CarLaneLeftCount;
			if (!mask.m_Car.m_Left.IsAnySet())
			{
				uncovered.Add(new UncoveredConnection
				{
					m_Edge = edgeInfo.m_Edge,
					m_Position = edgeInfo.m_Position,
					m_VehicleGroup = VehicleGroup.Car,
					m_TurnType = TurnType.Left,
					m_LaneCount = edgeInfo.m_CarLaneLeftCount
				});
			}
		}

		
		if (edgeInfo.m_CarLaneStraightCount > 0)
		{
			totalConnections += edgeInfo.m_CarLaneStraightCount;
			if (!mask.m_Car.m_Straight.IsAnySet())
			{
				uncovered.Add(new UncoveredConnection
				{
					m_Edge = edgeInfo.m_Edge,
					m_Position = edgeInfo.m_Position,
					m_VehicleGroup = VehicleGroup.Car,
					m_TurnType = TurnType.Straight,
					m_LaneCount = edgeInfo.m_CarLaneStraightCount
				});
			}
		}

		
		if (edgeInfo.m_CarLaneRightCount > 0)
		{
			totalConnections += edgeInfo.m_CarLaneRightCount;
			if (!mask.m_Car.m_Right.IsAnySet())
			{
				uncovered.Add(new UncoveredConnection
				{
					m_Edge = edgeInfo.m_Edge,
					m_Position = edgeInfo.m_Position,
					m_VehicleGroup = VehicleGroup.Car,
					m_TurnType = TurnType.Right,
					m_LaneCount = edgeInfo.m_CarLaneRightCount
				});
			}
		}

		
		if (edgeInfo.m_CarLaneUTurnCount > 0)
		{
			totalConnections += edgeInfo.m_CarLaneUTurnCount;
			if (!mask.m_Car.m_UTurn.IsAnySet())
			{
				uncovered.Add(new UncoveredConnection
				{
					m_Edge = edgeInfo.m_Edge,
					m_Position = edgeInfo.m_Position,
					m_VehicleGroup = VehicleGroup.Car,
					m_TurnType = TurnType.UTurn,
					m_LaneCount = edgeInfo.m_CarLaneUTurnCount
				});
			}
		}
	}

	private static void CheckPublicCarLanes(
		ref NativeList<UncoveredConnection> uncovered,
		ref int totalConnections,
		NodeUtils.EdgeInfo edgeInfo)
	{
		var mask = edgeInfo.m_EdgeGroupMask;

		
		if (edgeInfo.m_PublicCarLaneLeftCount > 0)
		{
			totalConnections += edgeInfo.m_PublicCarLaneLeftCount;
			if (!mask.m_PublicCar.m_Left.IsAnySet())
			{
				uncovered.Add(new UncoveredConnection
				{
					m_Edge = edgeInfo.m_Edge,
					m_Position = edgeInfo.m_Position,
					m_VehicleGroup = VehicleGroup.PublicCar,
					m_TurnType = TurnType.Left,
					m_LaneCount = edgeInfo.m_PublicCarLaneLeftCount
				});
			}
		}

		
		if (edgeInfo.m_PublicCarLaneStraightCount > 0)
		{
			totalConnections += edgeInfo.m_PublicCarLaneStraightCount;
			if (!mask.m_PublicCar.m_Straight.IsAnySet())
			{
				uncovered.Add(new UncoveredConnection
				{
					m_Edge = edgeInfo.m_Edge,
					m_Position = edgeInfo.m_Position,
					m_VehicleGroup = VehicleGroup.PublicCar,
					m_TurnType = TurnType.Straight,
					m_LaneCount = edgeInfo.m_PublicCarLaneStraightCount
				});
			}
		}

		
		if (edgeInfo.m_PublicCarLaneRightCount > 0)
		{
			totalConnections += edgeInfo.m_PublicCarLaneRightCount;
			if (!mask.m_PublicCar.m_Right.IsAnySet())
			{
				uncovered.Add(new UncoveredConnection
				{
					m_Edge = edgeInfo.m_Edge,
					m_Position = edgeInfo.m_Position,
					m_VehicleGroup = VehicleGroup.PublicCar,
					m_TurnType = TurnType.Right,
					m_LaneCount = edgeInfo.m_PublicCarLaneRightCount
				});
			}
		}

		
		if (edgeInfo.m_PublicCarLaneUTurnCount > 0)
		{
			totalConnections += edgeInfo.m_PublicCarLaneUTurnCount;
			if (!mask.m_PublicCar.m_UTurn.IsAnySet())
			{
				uncovered.Add(new UncoveredConnection
				{
					m_Edge = edgeInfo.m_Edge,
					m_Position = edgeInfo.m_Position,
					m_VehicleGroup = VehicleGroup.PublicCar,
					m_TurnType = TurnType.UTurn,
					m_LaneCount = edgeInfo.m_PublicCarLaneUTurnCount
				});
			}
		}
	}

	private static void CheckTrackLanes(
		ref NativeList<UncoveredConnection> uncovered,
		ref int totalConnections,
		NodeUtils.EdgeInfo edgeInfo)
	{
		var mask = edgeInfo.m_EdgeGroupMask;

		
		if (edgeInfo.m_TrackLaneLeftCount > 0)
		{
			totalConnections += edgeInfo.m_TrackLaneLeftCount;
			if (!mask.m_Track.m_Left.IsAnySet())
			{
				uncovered.Add(new UncoveredConnection
				{
					m_Edge = edgeInfo.m_Edge,
					m_Position = edgeInfo.m_Position,
					m_VehicleGroup = VehicleGroup.Tram | VehicleGroup.Train,
					m_TurnType = TurnType.Left,
					m_LaneCount = edgeInfo.m_TrackLaneLeftCount
				});
			}
		}

		
		if (edgeInfo.m_TrackLaneStraightCount > 0)
		{
			totalConnections += edgeInfo.m_TrackLaneStraightCount;
			if (!mask.m_Track.m_Straight.IsAnySet())
			{
				uncovered.Add(new UncoveredConnection
				{
					m_Edge = edgeInfo.m_Edge,
					m_Position = edgeInfo.m_Position,
					m_VehicleGroup = VehicleGroup.Tram | VehicleGroup.Train,
					m_TurnType = TurnType.Straight,
					m_LaneCount = edgeInfo.m_TrackLaneStraightCount
				});
			}
		}

		
		if (edgeInfo.m_TrackLaneRightCount > 0)
		{
			totalConnections += edgeInfo.m_TrackLaneRightCount;
			if (!mask.m_Track.m_Right.IsAnySet())
			{
				uncovered.Add(new UncoveredConnection
				{
					m_Edge = edgeInfo.m_Edge,
					m_Position = edgeInfo.m_Position,
					m_VehicleGroup = VehicleGroup.Tram | VehicleGroup.Train,
					m_TurnType = TurnType.Right,
					m_LaneCount = edgeInfo.m_TrackLaneRightCount
				});
			}
		}
	}

	private static void CheckPedestrianLanes(
		ref NativeList<UncoveredConnection> uncovered,
		ref int totalConnections,
		NodeUtils.EdgeInfo edgeInfo)
	{
		var mask = edgeInfo.m_EdgeGroupMask;

		
		if (edgeInfo.m_PedestrianLaneStopLineCount > 0)
		{
			totalConnections += edgeInfo.m_PedestrianLaneStopLineCount;
			if (!mask.m_Pedestrian.IsAnySet())
			{
				uncovered.Add(new UncoveredConnection
				{
					m_Edge = edgeInfo.m_Edge,
					m_Position = edgeInfo.m_Position,
					m_VehicleGroup = VehicleGroup.Pedestrian,
					m_TurnType = TurnType.Straight,
					m_LaneCount = edgeInfo.m_PedestrianLaneStopLineCount
				});
			}
		}
	}

	private static void CheckBicycleLanes(
		ref NativeList<UncoveredConnection> uncovered,
		ref int totalConnections,
		NodeUtils.EdgeInfo edgeInfo)
	{
		var mask = edgeInfo.m_EdgeGroupMask;

		
		if (edgeInfo.m_BicycleLaneCount > 0)
		{
			totalConnections += edgeInfo.m_BicycleLaneCount;
			if (!mask.m_Bicycle.IsAnySet())
			{
				uncovered.Add(new UncoveredConnection
				{
					m_Edge = edgeInfo.m_Edge,
					m_Position = edgeInfo.m_Position,
					m_VehicleGroup = VehicleGroup.Bike,
					m_TurnType = TurnType.Straight,
					m_LaneCount = edgeInfo.m_BicycleLaneCount
				});
			}
		}
	}
}
