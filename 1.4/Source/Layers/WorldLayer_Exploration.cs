using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Verse;
using RimWorld.Planet;


namespace RimworldExploration.Layer
{
	[StaticConstructorOnStartup]

	public class WorldLayer_Exploration : WorldLayer
	{

		private List<List<int>> triangleIndexToTileID = new List<List<int>>();

		private List<Vector3> elevationValues = new List<Vector3>();

		private Material TileMaterial_Explore;
		
		private List<Vector3> verts = new List<Vector3>();
		private List<int> tileIDToVerts_offsets = new List<int>();
		private List<int> tileIDToNeighbors_offsets = new List<int>();
		private List<int> tileIDToNeighbors_values = new List<int>();


		public void Initialize()
		{
			
		}

		public override IEnumerable Regenerate()
		{
			WorldGrid grid = Find.World.grid;
			if (!TileMaterial_Explore)
			{
				Shader shader = ShaderDatabase.DefaultShader;
				TileMaterial_Explore = new Material(shader);
				TileMaterial_Explore.color = Color.black;
				Vector3 viewCenter = grid.viewCenter;
				float viewAngle = grid.viewAngle;
				PlanetShapeGenerator.Generate(10, out verts, out tileIDToVerts_offsets, out tileIDToNeighbors_offsets, out tileIDToNeighbors_values, 100.3f, viewCenter, viewAngle);
				Initialize();
				foreach (object item2 in CalculateInterpolatedVerticesParams())
				{
					yield return item2;
				}
			}
			
			triangleIndexToTileID.Clear();
			
			foreach (object item in base.Regenerate())
			{
				yield return item;
			}

			int num = 0;
			for (int i = 0; i < grid.TilesCount; i++)
			{
				if (VisibilityManager.TileExplored(i)) continue;
				int subMeshIndex;
				LayerSubMesh subMesh = GetSubMesh(TileMaterial_Explore, out subMeshIndex);
				while (subMeshIndex >= triangleIndexToTileID.Count)
				{
					triangleIndexToTileID.Add(new List<int>());
				}

				int count = subMesh.verts.Count;
				int num2 = 0;
				int num3 = ((i + 1 < tileIDToVerts_offsets.Count) ? tileIDToVerts_offsets[i + 1] : verts.Count);
				for (int j = tileIDToVerts_offsets[i]; j < num3; j++)
				{
					subMesh.verts.Add(verts[j]);
					subMesh.uvs.Add(elevationValues[num]);
					num++;
					if (j < num3 - 2)
					{
						subMesh.tris.Add(count + num2 + 2);
						subMesh.tris.Add(count + num2 + 1);
						subMesh.tris.Add(count);
						triangleIndexToTileID[subMeshIndex].Add(i);
					}
					num2++;
				}
			}
			
			FinalizeMesh(MeshParts.All);

			// elevationValues.Clear();
			// elevationValues.TrimExcess();
			
		}

		private IEnumerable CalculateInterpolatedVerticesParams()
		{
			elevationValues.Clear();
			WorldGrid grid = Find.World.grid;
			
			int tilesCount = grid.TilesCount;
			
			List<Tile> tiles = grid.tiles;
			
			for (int i = 0; i < tilesCount; i++)
			{
				Tile tile = tiles[i];
				float elevation = tile.elevation;
				int num = ((i + 1 < tileIDToNeighbors_offsets.Count)
					? tileIDToNeighbors_offsets[i + 1]
					: tileIDToNeighbors_values.Count);
				int num2 = ((i + 1 < tilesCount) ? tileIDToVerts_offsets[i + 1] : verts.Count);
				for (int j = tileIDToVerts_offsets[i]; j < num2; j++)
				{
					Vector3 item = default(Vector3);
					item.x = elevation + 1;
					bool flag = false;
					for (int k = tileIDToNeighbors_offsets[i]; k < num; k++)
					{
						int num3 = ((tileIDToNeighbors_values[k] + 1 < tileIDToVerts_offsets.Count)
							? tileIDToVerts_offsets[tileIDToNeighbors_values[k] + 1]
							: verts.Count);
						for (int l = tileIDToVerts_offsets[tileIDToNeighbors_values[k]]; l < num3; l++)
						{
							if (!(verts[l] == verts[j]))
							{
								continue;
							}

							Tile tile2 = tiles[tileIDToNeighbors_values[k]];
							if (!flag)
							{
								if ((tile2.elevation >= 0f && elevation <= 0f) ||
								    (tile2.elevation <= 0f && elevation >= 0f))
								{
									flag = true;
								}
								else if (tile2.elevation > item.x)
								{
									item.x = tile2.elevation;
								}
							}

							break;
						}
					}
					elevationValues.Add(item);
				}

				if (i % 1000 == 0)
				{
					yield return null;
				}
			}
		}
	}
}