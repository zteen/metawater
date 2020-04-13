using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

struct GridPoint {
  public GridPoint(Vector3 position, float value) {
    this.position = position;
    this.value = value;
  }

  public Vector3 position;
  public float value;
}

struct GridCell {
  public GridCell(Vector3[] points, float[] values) {
    this.points = points;
    this.values = values;
  }

  public Vector3[] points;
  public float[] values;
}

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MetaballRenderer : MonoBehaviour {

  [SerializeField]
  private Bounds boundingBox;
  [SerializeField]
  [Range(1,30)]
  private float resolution = 10;
  [SerializeField]
  [Range(0, 10)]
  private float threshold = 2;
  [SerializeField]
  private bool showDebugBalls = false; // DEBUG

  private GridPoint[,,] grid;
  private GameObject[] metaballObjects; // DEBUG
  private MeshFilter meshFilter;
  private Metaball[] metaballs;

  void Start() {
    //ConstructGrid();
    meshFilter = GetComponent<MeshFilter>();
    // DEBUG START
    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
    Mesh sphereMesh = sphere.GetComponent<MeshFilter>().mesh;
    Material sphereMaterial = sphere.GetComponent<MeshRenderer>().sharedMaterial;
    Destroy(sphere);
    int numBalls = 10;
    metaballObjects = new GameObject[numBalls];
    metaballs = new Metaball[numBalls];
    for (int i = 0; i < numBalls; ++i) {
      float radius = 2.0f;
      float x = Random.Range(boundingBox.min.x + radius, boundingBox.max.x - radius);
      float y = Random.Range(boundingBox.min.y + radius, boundingBox.max.y - radius);
      float z = Random.Range(boundingBox.min.z + radius, boundingBox.max.z - radius);

      metaballObjects[i] = new GameObject("Metaball"+(i+1));
      metaballs[i] = metaballObjects[i].AddComponent<Metaball>();
      metaballs[i].radius = radius;
      metaballs[i].transform.position = new Vector3(x, y, z);
      // metaballObjects[i].transform.position = boundingBox.center;

      if (showDebugBalls) {
        metaballObjects[i].AddComponent<MeshFilter>().mesh = sphereMesh;
        metaballObjects[i].AddComponent<MeshRenderer>().sharedMaterial = sphereMaterial;
      }
    }
    // DEBUG END
  }

  void Update() {
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();

    MarchingCubes(vertices, triangles);

    Mesh mesh = new Mesh();
    meshFilter.mesh = mesh;
    mesh.Clear();
    mesh.vertices = vertices.ToArray(); // OPTIMIZATION 1 POSSIBLE
    mesh.triangles = triangles.ToArray(); // OPTIMIZATION 1 POSSIBLE
    mesh.RecalculateNormals();
    mesh.RecalculateBounds();
  }

  // Constructs the uniform grid and assigns the 3D positions for each grid point.
  private void ConstructGrid() {
    Vector3 increments = new Vector3(
    (boundingBox.max.x - boundingBox.min.x) / resolution,
    (boundingBox.max.y - boundingBox.min.y) / resolution,
    (boundingBox.max.z - boundingBox.min.z) / resolution);

    int pointsX = (int) ((boundingBox.max.x - boundingBox.min.x) / increments.x);
    int pointsY = (int) ((boundingBox.max.y - boundingBox.min.y) / increments.y);
    int pointsZ = (int) ((boundingBox.max.z - boundingBox.min.z) / increments.z);

    grid = new GridPoint[pointsX, pointsY, pointsZ];

    for (int indexX = 0; indexX < pointsX; ++indexX) {
      for (int indexY = 0; indexY < pointsY; ++indexY) {
        for (int indexZ = 0; indexZ < pointsZ; ++indexZ) {
          Vector3 position = boundingBox.min +
                             new Vector3(indexX*increments.x, indexY*increments.y, indexZ*increments.z);
          grid[indexX, indexY, indexZ] = new GridPoint(position, 0.0f);
        }
      }
    }
  }

  // Updates the value in each grid point according to metaball positions.
  private void UpdateGridValues() {
    if (grid == null) return;

    for (int i = 0; i < grid.GetLength(0); ++i) {
      for (int j = 0; j < grid.GetLength(1); ++j) {
        for (int k = 0; k < grid.GetLength(2); ++k) {
          Vector3 position = grid[i,j,k].position;
          float pointValue = 0;
          foreach (Metaball ball in metaballs) {
            pointValue += ball.Falloff(position);
          }
          grid[i,j,k].value = pointValue;
        }
      }
    }
  }

  // Begins the marching cubes algorithm.
  private void MarchingCubes(List<Vector3> vertices, List<int> triangles) {
    Vector3 increments = new Vector3(
    (boundingBox.max.x - boundingBox.min.x) / resolution,
    (boundingBox.max.y - boundingBox.min.y) / resolution,
    (boundingBox.max.z - boundingBox.min.z) / resolution);

    for (float x = boundingBox.min.x; x < boundingBox.max.x - increments.x; x += increments.x) {
      for (float y = boundingBox.min.y; y < boundingBox.max.y - increments.y; y += increments.y) {
        for (float z = boundingBox.min.z; z < boundingBox.max.z - increments.z; z += increments.z) {
          Polygonize(ConstructGridCell(new Vector3(x, y, z), increments), vertices, triangles);
        }
      }
    }
  }

  private void Polygonize(GridCell cell, List<Vector3> vertices, List<int> triangles) {
    // Determine index into edge table.
    int cubeIndex = 0;
    for (int i = 0; i < cell.values.Length; i++) {
      if (cell.values[i] > threshold) cubeIndex |= (1 << i);
    }

    Vector3[] vertexList = new Vector3[12];

    // Find vertices where the metaball surface intersects the cube.
    if ((MarchingCubesTables.edgeTable[cubeIndex] & 1) != 0)
    vertexList[0] =
    InterpolateVertex(cell.points[0], cell.points[1], cell.values[0], cell.values[1]);
    if ((MarchingCubesTables.edgeTable[cubeIndex] & 2) != 0)
    vertexList[1] =
    InterpolateVertex(cell.points[1], cell.points[2], cell.values[1], cell.values[2]);
    if ((MarchingCubesTables.edgeTable[cubeIndex] & 4) != 0)
    vertexList[2] =
    InterpolateVertex(cell.points[2], cell.points[3], cell.values[2], cell.values[3]);
    if ((MarchingCubesTables.edgeTable[cubeIndex] & 8) != 0)
    vertexList[3] =
    InterpolateVertex(cell.points[3], cell.points[0], cell.values[3], cell.values[0]);
    if ((MarchingCubesTables.edgeTable[cubeIndex] & 16) != 0)
    vertexList[4] =
    InterpolateVertex(cell.points[4], cell.points[5], cell.values[4], cell.values[5]);
    if ((MarchingCubesTables.edgeTable[cubeIndex] & 32) != 0)
    vertexList[5] =
    InterpolateVertex(cell.points[5], cell.points[6], cell.values[5], cell.values[6]);
    if ((MarchingCubesTables.edgeTable[cubeIndex] & 64) != 0)
    vertexList[6] =
    InterpolateVertex(cell.points[6], cell.points[7], cell.values[6], cell.values[7]);
    if ((MarchingCubesTables.edgeTable[cubeIndex] & 128) != 0)
    vertexList[7] =
    InterpolateVertex(cell.points[7], cell.points[4], cell.values[7], cell.values[4]);
    if ((MarchingCubesTables.edgeTable[cubeIndex] & 256) != 0)
    vertexList[8] =
    InterpolateVertex(cell.points[0], cell.points[4], cell.values[0], cell.values[4]);
    if ((MarchingCubesTables.edgeTable[cubeIndex] & 512) != 0)
    vertexList[9] =
    InterpolateVertex(cell.points[1], cell.points[5], cell.values[1], cell.values[5]);
    if ((MarchingCubesTables.edgeTable[cubeIndex] & 1024) != 0)
    vertexList[10] =
    InterpolateVertex(cell.points[2], cell.points[6], cell.values[2], cell.values[6]);
    if ((MarchingCubesTables.edgeTable[cubeIndex] & 2048) != 0)
    vertexList[11] =
    InterpolateVertex(cell.points[3], cell.points[7], cell.values[3], cell.values[7]);

    // Add vertices and triangles. OPTIMIZATION 1 POSSIBLE
    int numVertices = vertices.Count; // The next vertex index.
    for (int i = 0; MarchingCubesTables.triTable[cubeIndex, i] != -1; i += 3) {
      // Create vertices and add to mesh vertex list.
      vertices.Add(vertexList[MarchingCubesTables.triTable[cubeIndex, i]]);
      vertices.Add(vertexList[MarchingCubesTables.triTable[cubeIndex, i+1]]);
      vertices.Add(vertexList[MarchingCubesTables.triTable[cubeIndex, i+2]]);
      // Add the indices for the newly created vertices to mesh triangles.
      // The winding order in the algorithm originally is counter-clockwise, but
      // Unity requires clockwise.
      triangles.Add(numVertices);
      numVertices += 2;
      triangles.Add(numVertices);
      numVertices -= 1;
      triangles.Add(numVertices);
      numVertices += 2;
    }
  }

  private GridCell ConstructGridCell(Vector3 minPosition, Vector3 increments) {
    // Create the point positions, indexed as in the paper.
    Vector3[] positions = {
      minPosition,
      minPosition + new Vector3(0, 0, increments.z),
      minPosition + new Vector3(increments.x, 0, increments.z),
      minPosition + new Vector3(increments.x, 0, 0),
      minPosition + new Vector3(0, increments.y, 0),
      minPosition + new Vector3(0, increments.y, increments.z),
      minPosition + new Vector3(increments.x, increments.y, increments.z),
      minPosition + new Vector3(increments.x, increments.y, 0),
    };

    float[] values = new float[8];

    for (int i = 0; i < positions.Length; ++i) { // OPTIMIZATION 2 POSSIBLE
      float pointValue = 0;
      foreach (Metaball ball in metaballs) {
        pointValue += ball.Falloff(positions[i]);
      }
      values[i] = pointValue;
    }

    return new GridCell(positions, values);
  }

  private Vector3 InterpolateVertex(Vector3 point1, Vector3 point2, float value1, float value2) {
    Vector3 point;
    if(Mathf.Abs(value1 - value2) > 0.00001) {
      point = point1 + (point2 - point1) * ((threshold - value1) / (value2 - value1));
    } else {
      point = point1;
    }
    return point;
  }
}