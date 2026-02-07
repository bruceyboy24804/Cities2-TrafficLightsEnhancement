using System.Collections.Generic;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Systems.Serialization;

public static class MigrationIssuesService
{
	private static readonly List<Entity> s_AffectedEntities = new();

	public static List<Entity> AffectedEntities => s_AffectedEntities;
	public static int Count => s_AffectedEntities.Count;
	public static bool HasIssues => s_AffectedEntities.Count > 0;

	public static void AddEntity(Entity entity)
	{
		if (!s_AffectedEntities.Contains(entity))
		{
			s_AffectedEntities.Add(entity);
		}
	}

	public static void RemoveAt(int index)
	{
		if (index >= 0 && index < s_AffectedEntities.Count)
		{
			s_AffectedEntities.RemoveAt(index);
		}
	}

	public static void Clear()
	{
		s_AffectedEntities.Clear();
	}

	public static Entity GetEntity(int index)
	{
		if (index >= 0 && index < s_AffectedEntities.Count)
		{
			return s_AffectedEntities[index];
		}
		return Entity.Null;
	}
}
