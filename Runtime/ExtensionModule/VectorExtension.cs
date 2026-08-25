using UnityEngine;

namespace FlowIoC.ExtensionModule
{
    public static class VectorExtension
    {
        public static bool ContainsInBetween(this Vector2Int vector2Int, int value) => vector2Int.x <= value && value < vector2Int.y;
        public static bool ContainsInBetween(this Vector2 vector2, int value) => vector2.x < value && value < vector2.y;
        
        public static Vector3 MultipliedBy(this Vector3 vector, float value) => new(vector.x * value, vector.y * value, vector.z * value);
        public static Vector3 MultipliedBy(this Vector2 vector, float value) => new Vector2(vector.x * value, vector.y * value);

        public static Vector2 ToVector2ByXZ(this Vector3 vector3) => new(vector3.x, vector3.z);
        public static Vector2 ToVector2ByXY(this Vector3 vector3) => new (vector3.x, vector3.y);
        public static Vector2 ToVector2ByYX(this Vector3 vector3) => new(vector3.y, vector3.x);
        public static Vector2 ToVector2ByYZ(this Vector3 vector3) => new(vector3.y, vector3.z);
        public static Vector2 ToVector2ByZX(this Vector3 vector3) => new(vector3.z, vector3.x);
        public static Vector2 ToVector2ByZY(this Vector3 vector3) => new(vector3.z, vector3.y);
        
        public static Vector2Int ToVector2ByXZ(this Vector3Int vector3) => new(vector3.x, vector3.z);
        public static Vector2Int ToVector2ByXY(this Vector3Int vector3) => new (vector3.x, vector3.y);
        public static Vector2Int ToVector2ByYX(this Vector3Int vector3) => new(vector3.y, vector3.x);
        public static Vector2Int ToVector2ByYZ(this Vector3Int vector3) => new(vector3.y, vector3.z);
        public static Vector2Int ToVector2ByZX(this Vector3Int vector3) => new(vector3.z, vector3.x);
        public static Vector2Int ToVector2ByZY(this Vector3Int vector3) => new(vector3.z, vector3.y);

        public static Vector3 ToVector3ByXY(this Vector2 vector2, float z = 0) => new(vector2.x, vector2.y, z);
        public static Vector3 ToVector3ByYX(this Vector2 vector2, float z = 0) => new(vector2.y, vector2.x, z);
        public static Vector3 ToVector3ByXZ(this Vector2 vector2, float y = 0) => new(vector2.x, y, vector2.y);
        public static Vector3 ToVector3ByZX(this Vector2 vector2, float y = 0) => new(vector2.y, y, vector2.x);
        public static Vector3 ToVector3ByYZ(this Vector2 vector2, float x = 0) => new(x, vector2.x, vector2.y);
        public static Vector3 ToVector3ByZY(this Vector2 vector2, float x = 0) => new(x, vector2.y, vector2.x);
        
        public static Vector3 ToVector3ByXY(this Vector2Int vector2, float z = 0) => new(vector2.x, vector2.y, z);
        public static Vector3 ToVector3ByYX(this Vector2Int vector2, float z = 0) => new(vector2.y, vector2.x, z);
        public static Vector3 ToVector3ByXZ(this Vector2Int vector2, float y = 0) => new(vector2.x, y, vector2.y);
        public static Vector3 ToVector3ByZX(this Vector2Int vector2, float y = 0) => new(vector2.y, y, vector2.x);
        public static Vector3 ToVector3ByYZ(this Vector2Int vector2, float x = 0) => new(x, vector2.x, vector2.y);
        public static Vector3 ToVector3ByZY(this Vector2Int vector2, float x = 0) => new(x, vector2.y, vector2.x);
        
        public static Vector3Int ToVector3IntByXY(this Vector2Int vector2, int z = 0) => new(vector2.x, vector2.y, z);
        public static Vector3Int ToVector3IntByYX(this Vector2Int vector2, int z = 0) => new(vector2.y, vector2.x, z);
        public static Vector3Int ToVector3IntByXZ(this Vector2Int vector2, int y = 0) => new(vector2.x, y, vector2.y);
        public static Vector3Int ToVector3IntByZX(this Vector2Int vector2, int y = 0) => new(vector2.y, y, vector2.x);
        public static Vector3Int ToVector3IntByYZ(this Vector2Int vector2, int x = 0) => new(x, vector2.x, vector2.y);
        public static Vector3Int ToVector3IntByZY(this Vector2Int vector2, int x = 0) => new(x, vector2.y, vector2.x);

        public static Vector2 ScaleWith(this Vector2 vector, Vector3 scaler) => Vector2.Scale(vector, scaler);
        public static Vector3 ScaleWith(this Vector3 vector, Vector3 scaler) => Vector3.Scale(vector, scaler);

        public static Vector2 WithX(this Vector2 vector, float x) => new(x, vector.y);
        public static Vector2 WithY(this Vector2 vector, float y) => new(vector.x, y);
        public static Vector2Int WithX(this Vector2Int vector, int x) => new(x, vector.y);
        public static Vector2Int WithY(this Vector2Int vector, int y) => new(vector.x, y);
        public static Vector3 WithX(this Vector3 vector, float x) => new(x, vector.y, vector.z);
        public static Vector3 WithY(this Vector3 vector, float y) => new(vector.x, y, vector.z);
        public static Vector3 WithZ(this Vector3 vector, float z) => new(vector.x, vector.y, z);
        public static Vector3 With(this Vector3 vector, float? x = null, float? y = null, float? z = null)
        {
            return new Vector3(x ?? vector.x, y ?? vector.y, z ?? vector.z);
        }
        public static Vector3Int WithX(this Vector3Int vector, int x) => new(x, vector.y, vector.z);
        public static Vector3Int WithY(this Vector3Int vector, int y) => new(vector.x, y, vector.z);
        public static Vector3Int WithZ(this Vector3Int vector, int z) => new(vector.x, vector.y, z);
        
        public static Vector2 AddX(this Vector2 vector, float x) => new(vector.x + x, vector.y);
        public static Vector2 AddY(this Vector2 vector, float y) => new Vector3(vector.x, vector.y + y);
        public static Vector3 AddX(this Vector3 vector, float x) => new(vector.x + x, vector.y, vector.z);
        public static Vector3 AddY(this Vector3 vector, float y) => new(vector.x, vector.y + y, vector.z);
        public static Vector3 AddZ(this Vector3 vector, float z) => new(vector.x, vector.y, vector.z + z);
        public static Vector3Int AddX(this Vector3Int vector, int x) => new(vector.x + x, vector.y, vector.z);
        public static Vector3Int AddY(this Vector3Int vector, int y) => new(vector.x, vector.y + y, vector.z);
        public static Vector3Int AddZ(this Vector3Int vector, int z) => new(vector.x, vector.y, vector.z + z);
        public static Vector3 Add(this Vector3 vector, float? x = null, float? y = null, float? z = null)
        {
            return new Vector3(
                (float) (x == null ? vector.x : vector.x + x),
                (float) (y == null ? vector.y : vector.y + y),
                (float) (z == null ? vector.z : vector.z + z));
        }
        
        public static Vector3 AddAll(this Vector3 vector, float value)
        {
            return new Vector3(vector.x + value, vector.y + value, vector.z + value);

        }
    }
}