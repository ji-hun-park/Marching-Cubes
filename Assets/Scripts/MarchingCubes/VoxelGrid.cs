using UnityEngine;

public class VoxelGrid
{
    public int sizeX, sizeY, sizeZ;
    public float[,,] density; // 밀도 값 저장

    public VoxelGrid(int x, int y, int z) {
        sizeX = x;
        sizeY = y;
        sizeZ = z;
        density = new float[x, y, z];
    }

    // 특정 위치의 밀도 값을 반환
    public float GetDensity(int x, int y, int z) {
        return density[x, y, z];
    }
}
