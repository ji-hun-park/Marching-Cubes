using System.Collections.Generic;
using UnityEngine;

public class MarchingCubes : MonoBehaviour {
    public int gridSize = 20; // Voxel Grid 크기
    public int gridSizeX = 10;
    public int gridSizeY = 10;
    public int gridSizeZ = 10;
    public float threshold = 0.5f; // 밀도 기준값
    private VoxelGrid voxelGrid;
    private MeshFilter meshFilter;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();

    void Start() {
        meshFilter = gameObject.AddComponent<MeshFilter>();
        gameObject.AddComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));

        //voxelGrid = new VoxelGrid(gridSize, gridSize, gridSize);
        //GenerateDensityValues();
        GenerateMesh();
    }
    
    // 지형을 생성할 밀도 함수(SDF, Scalar Field) 생성, y값과 노이즈 값을 비교하여 지형의 형태를 결정
    float GetDensity(float x, float y, float z) {
        return y - Mathf.PerlinNoise(x, z); // 예제: 퍼린 노이즈 기반 지형
    } // GetDensity(x, y, z) > 0이면 공기, <= 0이면 지형이 됨
    
    // 3D 격자를 순회하면서 각 격자(Cell, Cube)의 8개 꼭짓점 밀도값을 샘플링하여 cubeIndex를 생성
    int ComputeCubeIndex(Vector3[] corners) {
        int cubeIndex = 0; // 삼각형을 찾는 Key 역할
        for (int i = 0; i < 8; i++) { // 8개의 꼭짓점을 검사하여 00000000 ~ 11111111 (0~255)의 값을 가짐
            if (GetDensity(corners[i].x, corners[i].y, corners[i].z) < 0) {
                cubeIndex |= (1 << i); // 해당 꼭짓점이 내부라면 cubeIndex 비트 설정
            }
        }
        return cubeIndex;
    }

    void GenerateMesh()
    {
        float cellSize = 1f; // 셀 크기 (예: 1x1x1)

        for (int x = 0; x < gridSizeX; x++) {
            for (int y = 0; y < gridSizeY; y++) {
                for (int z = 0; z < gridSizeZ; z++) {
                    Vector3 cellPosition = new Vector3(x * cellSize, y * cellSize, z * cellSize);
                    GenerateUnityMesh(cellPosition);
                }
            }
        }
    }
    
    void GenerateDensityValues() {
        // 노이즈를 사용해 밀도 값 설정
        for (int x = 0; x < gridSize; x++) {
            for (int y = 0; y < gridSize; y++) {
                for (int z = 0; z < gridSize; z++) {
                    float noise = Mathf.PerlinNoise(x * 0.1f, z * 0.1f);
                    voxelGrid.density[x, y, z] = y - noise * 10f; 
                }
            }
        }
    }

    // cubeIndex를 이용해 triTable에서 삼각형을 가져와서 메쉬를 생성
    void GenerateUnityMesh(Vector3 cellPosition) {
        /*
        for (int x = 0; x < gridSize - 1; x++) {
            for (int y = 0; y < gridSize - 1; y++) {
                for (int z = 0; z < gridSize - 1; z++) {
                    int cubeIndex = 0;

                    // 8개의 꼭짓점 체크하여 cubeIndex 계산
                    for (int i = 0; i < 8; i++) {
                        int dx = i & 1;
                        int dy = (i >> 1) & 1;
                        int dz = (i >> 2) & 1;

                        if (voxelGrid.GetDensity(x + dx, y + dy, z + dz) < threshold) {
                            cubeIndex |= 1 << i;
                        }
                    }

                    // 삼각형 패턴을 가져와서 메시 생성
                    int[] triangulation = MarchingCubesTable.triTable[cubeIndex];
                    for (int i = 0; triangulation[i] != -1; i += 3) {
                        vertices.Add(GetVertexPosition(x, y, z, triangulation[i]));
                        vertices.Add(GetVertexPosition(x, y, z, triangulation[i + 1]));
                        vertices.Add(GetVertexPosition(x, y, z, triangulation[i + 2]));

                        int baseIndex = vertices.Count - 3;
                        triangles.Add(baseIndex);
                        triangles.Add(baseIndex + 1);
                        triangles.Add(baseIndex + 2);
                    }
                }
            }
        }
        */
        Vector3[] corners = GetCubeCorners(cellPosition);
        int cubeIndex = ComputeCubeIndex(corners);

        int[] triangulation = MarchingCubesTable.triTable[cubeIndex];

        for (int i = 0; triangulation[i] != -1; i += 3) {
            Vector3 v0 = Interpolate(corners[triangulation[i]], corners[triangulation[i + 1]]);
            Vector3 v1 = Interpolate(corners[triangulation[i + 1]], corners[triangulation[i + 2]]);
            Vector3 v2 = Interpolate(corners[triangulation[i + 2]], corners[triangulation[i]]);

            int index = vertices.Count;
            vertices.Add(v0);
            vertices.Add(v1);
            vertices.Add(v2);

            triangles.Add(index);
            triangles.Add(index + 1);
            triangles.Add(index + 2);
        }
        
        // Unity Mesh 생성
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
    }
    
    // 현재 cell(격자)의 8개 꼭짓점 위치를 반환하는 함수
    /* 다음 순서를 가짐
     y
     ↑       (6)───────(7)
     |       / |       / |
     |     (2)───────(3) |
     |     |  |      |  | 
     |     | (4)───────(5) → x
     |     | /       | /
     |     (0)───────(1)
     |
     └───────→ z
    */
    Vector3[] GetCubeCorners(Vector3 cellPosition, float cellSize = 1f) {
        return new Vector3[]
        {
            cellPosition + new Vector3(0, 0, 0),   // (0) 
            cellPosition + new Vector3(cellSize, 0, 0),   // (1)
            cellPosition + new Vector3(0, 0, cellSize),   // (2)
            cellPosition + new Vector3(cellSize, 0, cellSize),   // (3)
            cellPosition + new Vector3(0, cellSize, 0),   // (4)
            cellPosition + new Vector3(cellSize, cellSize, 0),   // (5)
            cellPosition + new Vector3(0, cellSize, cellSize),   // (6)
            cellPosition + new Vector3(cellSize, cellSize, cellSize)    // (7)
        };
    }

    // 보간을 적용하여 부드러운 메시 생성
    Vector3 GetVertexPosition(int x, int y, int z, int edge) {
        Vector3 p1 = new Vector3(x, y, z);
        Vector3 p2 = p1 + new Vector3(1, 0, 0);

        return Vector3.Lerp(p1, p2, 0.5f);
    }
    
    // 두 점 사이에서 정확한 간격으로 보간하여 정점(Vertex) 위치를 계산함
    Vector3 Interpolate(Vector3 p1, Vector3 p2) {
        float v1 = GetDensity(p1.x, p1.y, p1.z);
        float v2 = GetDensity(p2.x, p2.y, p2.z);

        float t = v1 / (v1 - v2);
        return p1 + t * (p2 - p1);
    }
}
