

// EN: Multiplayer stub extending SimulationManager.
//     Currently empty — intended for multiplayer-specific overrides
//     (e.g. player list sync, shared resource management).
// VI: Stub multiplayer kế thừa SimulationManager.
//     Hiện đang trống — dự kiến cho các override riêng multiplayer
//     (vd: đồng bộ danh sách người chơi, quản lý tài nguyên chung).
using UnityEngine;

public class SimulationManagerMulti : SimulationManager
{
    // EN: Runtime initialization for scenes that instantiate ManagersMulti dynamically
    //     (e.g. Level1 via GAMABridgeLevel1). Sets the protected fields that are normally
    //     assigned in the Inspector when the prefab is placed in the scene.
    // VI: Khởi tạo runtime cho các scene tạo ManagersMulti động
    //     (vd: Level1 qua GAMABridgeLevel1). Gán các field protected thường được
    //     gán trong Inspector khi prefab được đặt trong scene.
    public void InitReferences(GameObject playerObj, GameObject groundObj)
    {
        player = playerObj;
        Ground = groundObj;
        if (player != null)
        {
            XROrigin = player.transform;
            mh = player.GetComponentInChildren<MoveHorizontal>();
            mv = player.GetComponentInChildren<MoveVertical>();
        }
    }
}