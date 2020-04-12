﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class TerrainConstructor : MonoBehaviour {

  [SerializeField]
  private Texture2D heightmap;
  [SerializeField]
  private float areaScale = 0.1f;
  [SerializeField]
  private float heightScale = 1.0f;

  private int height;
  private int width;
  private int[] triangles;
  private Mesh mesh;
  private Vector3[] vertices;
  private Vector3 origin;

  void Start() {
    // Initialize and set mesh.
    mesh = new Mesh();
    GetComponent<MeshFilter>().mesh = mesh;

    // Initialize origin.
    origin = transform.position;

    // Initialize height and width.
    height = heightmap.height;
    width = heightmap.width;

    // Construct vertices.
    vertices = new Vector3[width*height];
    Color32[] pixelData = heightmap.GetPixels32();
    for (int i = 0; i < height; ++i) { // For each row.
      for (int j = 0; j < width; ++j) { // For each column.
        // Extract height data from the red channel. Red channel should be equal to
        // the others, and between 0 and 255, so divide to get value between 0 and 1.
        float color = pixelData[i*width+j].r / 255.0f;
        // Set the vertex position as the offset from the origin, with y-value offset
        // according to the color in the heightmap and the desired scale.
        vertices[i*width+j] = new Vector3(origin.x + (j*areaScale), origin.y + (color*heightScale), origin.z + (i*areaScale));
      }
    }

    // Construct triangles.
    // Size of triangles array:
    // number of gridcells * 2 triangles per grid cell * 3 vertex indices per triangle
    triangles = new int[6*(height-1)*(width-1)];
    int triangleOffset = 0;
    for (int i = 0; i < (height-1); ++i) { // For each row of grid cells.
      for (int j = 0; j < (width-1); ++j) { // For each column of grid cells.
        int cellOffset = i*width + j;
        // Lower left triangle of grid cell (clockwise assigned).
        triangles[triangleOffset++] = cellOffset;
        triangles[triangleOffset++] = cellOffset + width;
        triangles[triangleOffset++] = cellOffset + 1;

        // Upper right triangle of grid cell (clockwise assigned).
        triangles[triangleOffset++] = cellOffset + 1;
        triangles[triangleOffset++] = cellOffset + width;
        triangles[triangleOffset++] = cellOffset + width + 1;
      }
    }

    // Update mesh.
    mesh.Clear();
    mesh.vertices = vertices;
    mesh.triangles = triangles;
    mesh.RecalculateNormals();
  }
}