using System.Collections.Generic;
using UnityEngine;

public class MarchingCubes : MonoBehaviour {
    public int gridSize = 20; // Voxel Grid 크기
    public float threshold = 0.5f; // 밀도 기준값
    private VoxelGrid voxelGrid;
    private MeshFilter meshFilter;

    void Start() {
        meshFilter = gameObject.AddComponent<MeshFilter>();
        gameObject.AddComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));

        voxelGrid = new VoxelGrid(gridSize, gridSize, gridSize);
        GenerateDensityValues();
        GenerateMesh();
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

    void GenerateMesh() {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

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

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
    }

    // 보간을 적용하여 부드러운 메시 생성
    Vector3 GetVertexPosition(int x, int y, int z, int edge) {
        Vector3 p1 = new Vector3(x, y, z);
        Vector3 p2 = p1 + new Vector3(1, 0, 0);

        return Vector3.Lerp(p1, p2, 0.5f);
    }
}
