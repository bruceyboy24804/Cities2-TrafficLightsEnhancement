using Colossal.Mathematics;
using Game.Net;
using Game.Rendering;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace C2VM.TrafficLightsEnhancement.Systems.Overlay;

public static class OverlayRenderingHelpers
{
	public static void DrawNodeOutline(
		Entity node,
		ref BufferLookup<ConnectedEdge> connectedEdgeData,
		ref ComponentLookup<StartNodeGeometry> startNodeGeometryData,
		ref ComponentLookup<EndNodeGeometry> endNodeGeometryData,
		ref ComponentLookup<Edge> edgeData,
		ref ComponentLookup<EdgeGeometry> edgeGeometryData,
		ref OverlayRenderSystem.Buffer overlayBuffer,
		Color color,
		float lineWidth,
		float offsetLength = 0f)
	{
		if (!connectedEdgeData.HasBuffer(node))
		{
			return;
		}

		DynamicBuffer<ConnectedEdge> connectedEdges = connectedEdgeData[node];

		for (int i = 0; i < connectedEdges.Length; i++)
		{
			ConnectedEdge edge = connectedEdges[i];
			
			if (!edgeData.HasComponent(edge.m_Edge) || !edgeGeometryData.HasComponent(edge.m_Edge))
			{
				continue;
			}

			bool isNearEnd = node == edgeData[edge.m_Edge].m_End;
			EdgeGeometry edgeGeometry = edgeGeometryData[edge.m_Edge];
			Segment edgeSegment = !isNearEnd ? edgeGeometry.m_Start : edgeGeometry.m_End;

			if (offsetLength > 0f && math.all(edgeSegment.m_Length > 0f))
			{
				float2 offsetFrac = math.clamp(offsetLength / edgeSegment.m_Length, float2.zero, new float2(1f));
				float4 cut = new float4(
					math.select(new float2(0f, offsetFrac.x), new float2(1f - offsetFrac.x, 1f), isNearEnd),
					math.select(new float2(0f, offsetFrac.y), new float2(1f - offsetFrac.y, 1f), isNearEnd)
				);
				Bezier4x3 leftToCorner = MathUtils.Cut(edgeSegment.m_Left, cut.xy);
				Bezier4x3 rightToCorner = MathUtils.Cut(edgeSegment.m_Right, cut.zw);
				float3 leftCorner = math.select(leftToCorner.a, leftToCorner.d, !isNearEnd);
				float3 rightCorner = math.select(rightToCorner.a, rightToCorner.d, !isNearEnd);
				overlayBuffer.DrawLine(color, color, 0.0f, 0, new Line3.Segment(leftCorner, rightCorner), lineWidth,  1);
				overlayBuffer.DrawCurve(color, color, 0.0f, 0, leftToCorner, lineWidth, 1);
				overlayBuffer.DrawCurve(color, color, 0.0f, 0, rightToCorner, lineWidth, 1);
			}
			else
			{
				overlayBuffer.DrawLine(color, color, 0.0f, 0, 
					new Line3.Segment(
						math.select(edgeSegment.m_Left.a, edgeSegment.m_Left.d, isNearEnd), 
						math.select(edgeSegment.m_Right.a, edgeSegment.m_Right.d, isNearEnd)), 
					lineWidth, 1);
			}

			if (startNodeGeometryData.HasComponent(edge.m_Edge) && endNodeGeometryData.HasComponent(edge.m_Edge))
			{
				EdgeNodeGeometry edgeNodeGeometry = !isNearEnd 
					? startNodeGeometryData[edge.m_Edge].m_Geometry 
					: endNodeGeometryData[edge.m_Edge].m_Geometry;

				overlayBuffer.DrawCurve(color, color, 0.0f, 0, edgeNodeGeometry.m_Left.m_Left, lineWidth, 1);
				overlayBuffer.DrawCurve(color, color, 0.0f, 0, edgeNodeGeometry.m_Right.m_Right, lineWidth, 1);

				if (edgeNodeGeometry.m_MiddleRadius > 0f)
				{
					overlayBuffer.DrawCurve(color, color, 0.0f, 0, edgeNodeGeometry.m_Left.m_Right, lineWidth, 1);
					overlayBuffer.DrawCurve(color, color, 0.0f, 0, edgeNodeGeometry.m_Right.m_Left, lineWidth,1);
				}
			}
		}
	}

	public static void DrawEdgeHalfOutline(
		Segment edgeSegment,
		ref OverlayRenderSystem.Buffer overlayBuffer,
		Color color,
		float lineWidth)
	{
		overlayBuffer.DrawCurve(color, edgeSegment.m_Left, lineWidth);
		overlayBuffer.DrawCurve(color, edgeSegment.m_Right, lineWidth);
		overlayBuffer.DrawLine(color, new Line3.Segment(edgeSegment.m_Left.d, edgeSegment.m_Right.d), lineWidth);
	}

	public static void DrawEdgeHighlight(
		Entity node,
		Entity targetEdge,
		ref BufferLookup<ConnectedEdge> connectedEdgeData,
		ref ComponentLookup<StartNodeGeometry> startNodeGeometryData,
		ref ComponentLookup<EndNodeGeometry> endNodeGeometryData,
		ref ComponentLookup<Edge> edgeData,
		ref OverlayRenderSystem.Buffer overlayBuffer,
		Color color,
		float lineWidth)
	{
		if (!connectedEdgeData.HasBuffer(node))
		{
			return;
		}

		DynamicBuffer<ConnectedEdge> connectedEdges = connectedEdgeData[node];

		for (int i = 0; i < connectedEdges.Length; i++)
		{
			ConnectedEdge edge = connectedEdges[i];
			
			if (edge.m_Edge != targetEdge)
			{
				continue;
			}
			
			if (!edgeData.HasComponent(edge.m_Edge))
			{
				continue;
			}

			bool isNearEnd = node == edgeData[edge.m_Edge].m_End;

			if (startNodeGeometryData.HasComponent(edge.m_Edge) && endNodeGeometryData.HasComponent(edge.m_Edge))
			{
				EdgeNodeGeometry edgeNodeGeometry = !isNearEnd 
					? startNodeGeometryData[edge.m_Edge].m_Geometry 
					: endNodeGeometryData[edge.m_Edge].m_Geometry;

				overlayBuffer.DrawLine(color, color, 0.0f,  0, 
					new Line3.Segment(edgeNodeGeometry.m_Left.m_Left.a, edgeNodeGeometry.m_Right.m_Right.a), 
					lineWidth, 1);
			}
			break;
		}
	}
}
