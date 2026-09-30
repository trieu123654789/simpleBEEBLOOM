using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// EN: Bidirectional coordinate converter between GAMA CRS (integer, precision-scaled)
//     and Unity world space (float, meters). GAMA uses a top-down 2D grid where
//     Y increases downward; Unity uses a 3D left-hand system where Z is forward.
//     The converter applies scaling coefficients and offsets for each axis.
//     Note: GamaCRSCoefY is negated in the constructor to flip the Y-axis.
// VI: Bộ chuyển đổi tọa độ hai chiều giữa CRS GAMA (số nguyên, scale theo precision)
//     và không gian thế giới Unity (float, mét). GAMA dùng lưới 2D từ trên xuống
//     với Y tăng xuống dưới; Unity dùng hệ 3D thuận tay trái với Z hướng về trước.
//     Bộ chuyển đổi áp dụng hệ số co giãn và offset cho mỗi trục.
//     Lưu ý: GamaCRSCoefY bị đảo dấu trong constructor để lật trục Y.
public class CoordinateConverter 
{
    // EN: Scale coefficient for GAMA X → Unity X.
    // VI: Hệ số co giãn GAMA X → Unity X.
    public float GamaCRSCoefX = 1.0f;
    // EN: Scale coefficient for GAMA Y → Unity Z (negated in constructor).
    // VI: Hệ số co giãn GAMA Y → Unity Z (đảo dấu trong constructor).
    public float GamaCRSCoefY = 1.0f;
    // EN: Scale coefficient for GAMA Z → Unity Y (vertical axis).
    // VI: Hệ số co giãn GAMA Z → Unity Y (trục đứng).
    public float GamaCRSCoefZ = 1.0f;
    // EN: Offset added to Unity X after scaling.
    // VI: Offset cộng vào Unity X sau khi scale.
    public float GamaCRSOffsetX = 1.0f;
    // EN: Offset added to Unity Z after scaling.
    // VI: Offset cộng vào Unity Z sau khi scale.
    public float GamaCRSOffsetY = 1.0f;
    // EN: Offset added to Unity Y (vertical) after scaling.
    // VI: Offset cộng vào Unity Y (trục đứng) sau khi scale.
    public float GamaCRSOffsetZ = 1.0f;

    // EN: Precision multiplier — all GAMA integer coords = real_value * precision.
    // VI: Hệ số nhân precision — mọi tọa độ nguyên GAMA = giá_trị_thực * precision.
    public int precision;

    // EN: 2D constructor (no Z/vertical conversion).
    // VI: Constructor 2D (không chuyển đổi Z/trục đứng).
    public CoordinateConverter(int p, float x, float y, float ox, float oy)
    {
        precision = p;
        GamaCRSCoefX = x;
        GamaCRSCoefY = -1 * y;
        GamaCRSOffsetX = ox;
        GamaCRSOffsetY = oy;
    }

    // EN: 3D constructor (includes Z/vertical axis conversion).
    // VI: Constructor 3D (bao gồm chuyển đổi trục Z/đứng).
    public CoordinateConverter(int p, float x, float y, float z, float ox, float oy, float oz)
    {
        precision = p;
        GamaCRSCoefX = x;
        GamaCRSCoefY = -1 * y;
        GamaCRSCoefZ = z;
        GamaCRSOffsetX = ox;
        GamaCRSOffsetY = oy;
        GamaCRSOffsetZ = oz;
    }
    // EN: Convert GAMA 2D integer coordinates → Unity Vector2.
    //     Formula: Unity.x = (CoefX * gamaX) / precision + OffsetX
    //              Unity.y = (CoefY * gamaY) / precision + OffsetY
    // VI: Chuyển tọa độ 2D số nguyên GAMA → Unity Vector2.
    //     Công thức: Unity.x = (CoefX * gamaX) / precision + OffsetX
    //              Unity.y = (CoefY * gamaY) / precision + OffsetY
    public Vector2 fromGAMACRS2D(int x, int y )
    {
        return new Vector2((GamaCRSCoefX * x) / precision + GamaCRSOffsetX, (GamaCRSCoefY * y) / precision + GamaCRSOffsetY);
    }

    // EN: Convert GAMA 3D integer coordinates → Unity Vector3.
    //     GAMA(x,y,z) → Unity(x=gamaX, y=gamaZ, z=gamaY) with axis remapping.
    // VI: Chuyển tọa độ 3D số nguyên GAMA → Unity Vector3.
    //     GAMA(x,y,z) → Unity(x=gamaX, y=gamaZ, z=gamaY) với ánh xạ lại trục.
    public Vector3 fromGAMACRS(int x, int y, int z)
    {
        return new Vector3((GamaCRSCoefX * x) / precision + GamaCRSOffsetX, (GamaCRSCoefZ * z) / precision + GamaCRSOffsetZ, (GamaCRSCoefY * y) / precision + GamaCRSOffsetY);
    }

    // EN: Convert Unity Vector3 → GAMA 2D integer coordinates [gamaX, gamaY].
    //     Inverse of fromGAMACRS, discarding the vertical (Y) component.
    // VI: Chuyển Unity Vector3 → tọa độ 2D số nguyên GAMA [gamaX, gamaY].
    //     Nghịch đảo của fromGAMACRS, bỏ thành phần đứng (Y).
    public List<int> toGAMACRS(Vector3 pos)
    {
        List<int> position = new List<int>();
        position.Add((int)((pos.x - GamaCRSOffsetX)/ GamaCRSCoefX * precision));
        position.Add((int)((pos.z - GamaCRSOffsetY)/ GamaCRSCoefY * precision));

        return position;
    }

    // EN: Convert Unity Vector3 → GAMA 3D integer coordinates [gamaX, gamaY, gamaZ].
    //     Full inverse including vertical axis: Unity.Y → GAMA.Z.
    // VI: Chuyển Unity Vector3 → tọa độ 3D số nguyên GAMA [gamaX, gamaY, gamaZ].
    //     Nghịch đảo đầy đủ bao gồm trục đứng: Unity.Y → GAMA.Z.
    public List<int> toGAMACRS3D(Vector3 pos)
    {
        List<int> position = new List<int>();
         position.Add((int)((pos.x - GamaCRSOffsetX)/ GamaCRSCoefX * precision));
        position.Add((int)((pos.z - GamaCRSOffsetY) / GamaCRSCoefY * precision));
        position.Add((int)((pos.y - GamaCRSOffsetZ) / GamaCRSCoefZ * precision));

        return position; 
    }


}
