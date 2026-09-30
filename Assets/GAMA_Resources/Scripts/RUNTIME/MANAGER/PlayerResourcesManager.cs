using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// EN: Manages player resources (e.g., wood from trees) with automatic refilling based on nearby refill sources.
// VI: Quản lý tài nguyên của người chơi (ví dụ: gỗ từ cây cối) với việc tự động nạp lại dựa trên các nguồn nạp gần đó.
public class PlayerResourcesManager : MonoBehaviour, ISupply
{
    // EN: Stats section - configurable parameters in Unity Inspector.
    // VI: Phần Stats - các tham số có thể cấu hình trong Unity Inspector.
    [Header("Stats")]

    // EN: Initial amount of resources at game start.
    // VI: Số lượng tài nguyên ban đầu khi bắt đầu game.
    [SerializeField] private int initialAmount = 0;

    // EN: Radius within which the manager detects refill sources (trees).
    // VI: Bán kính mà trong đó manager phát hiện các nguồn nạp (cây cối).
    [SerializeField] private float workRadius = 10f;

    // EN: Time interval (seconds) between each automatic refill.
    // VI: Khoảng thời gian (giây) giữa mỗi lần nạp tự động.
    [SerializeField] private float refillInterval = 2f;

    // EN: Amount of resources added per refill per source.
    // VI: Số lượng tài nguyên được thêm mỗi lần nạp cho mỗi nguồn.
    [SerializeField] private int refillAmount = 1;

    // EN: Miscellaneous section - other settings.
    // VI: Phần Miscellaneous - các cài đặt khác.
    [Header("Miscellaneous")]

    // EN: LayerMask to filter which objects are considered refill sources.
    // VI: LayerMask để lọc những object nào được coi là nguồn nạp.
    [SerializeField] private LayerMask targetLayerMask;

    // EN: Runtime private variables - not editable in Inspector, managed internally.
    // VI: Các biến private runtime - không chỉnh sửa được trong Inspector, được quản lý nội bộ.
    // EN: Current amount of resources the player has.
    // VI: Số lượng tài nguyên hiện tại mà người chơi có.
    private int currentAmount;

    // EN: Timer tracking time until next refill.
    // VI: Bộ đếm thời gian theo dõi thời gian đến lần nạp tiếp theo.
    private float currentInterval;

    // EN: Number of active refill sources (trees) currently in range.
    // VI: Số lượng nguồn nạp hoạt động (cây cối) hiện tại trong phạm vi.
    private int currentRefillSources;

    // EN: Total number of trees detected at initialization.
    // VI: Tổng số cây được phát hiện khi khởi tạo.
    private int totalTree;

    // EN: Public properties (Getters) - read-only access to private fields.
    // VI: Các thuộc tính public (Getters) - truy cập chỉ đọc đến các trường private.

    // EN: Gets the current resource amount.
    // VI: Lấy số lượng tài nguyên hiện tại.
    public int CurrentAmount {
        get { return currentAmount; }
    }

    // EN: Gets the work radius for detecting refill sources.
    // VI: Lấy bán kính làm việc để phát hiện nguồn nạp.
    public float WorkRadius {
        get { return workRadius; }
    }

    // EN: Gets the refill interval.
    // VI: Lấy khoảng thời gian nạp.
    public float RefillInterval {
        get { return refillInterval; }
    }

    // EN: Gets the refill amount per source.
    // VI: Lấy số lượng nạp cho mỗi nguồn.
    public int RefillAmount {
        get { return refillAmount; }
    }

    // EN: Gets the current number of refill sources in range.
    // VI: Lấy số lượng nguồn nạp hiện tại trong phạm vi.
    public int CurrentRefillSources {
        get { return currentRefillSources; }
    }

    // EN: Gets the total number of trees detected at start.
    // VI: Lấy tổng số cây được phát hiện khi bắt đầu.
    public int TotalTree {
        get { return totalTree; }
    }

    // EN: Called when the script instance is being loaded. Initializes runtime variables and starts periodic checks.
    // VI: Được gọi khi instance script đang được tải. Khởi tạo các biến runtime và bắt đầu kiểm tra định kỳ.
    void Awake()
    {
        currentAmount = initialAmount;
        currentInterval = refillInterval;
        currentRefillSources = 1;
        InvokeRepeating("CheckRefillSources", 0, .5f);
        GetTotalTree();
    }

    // EN: Calculates and sets the total number of trees by checking refill sources.
    // VI: Tính toán và đặt tổng số cây bằng cách kiểm tra nguồn nạp.
    void GetTotalTree()
    {
        CheckRefillSources();
        totalTree = currentRefillSources;
    }

    // EN: Called every frame. Decrements the refill timer and refills resources when it reaches zero.
    // VI: Được gọi mỗi frame. Giảm bộ đếm thời gian nạp và nạp tài nguyên khi nó đạt đến 0.
    void Update()
    {
        currentInterval -= Time.deltaTime;
        if (currentInterval <= 0)
        {
            Refill();
            currentInterval = refillInterval;
        }

    }

    // EN: Checks for nearby refill sources (trees) within the work radius.
    //     Counts only objects with IDamageable that are not dead (excludes certain fruit trees).
    // VI: Kiểm tra các nguồn nạp gần đó (cây cối) trong bán kính làm việc.
    //     Chỉ đếm các object có IDamageable và không chết (loại trừ một số cây trái cây).
    void CheckRefillSources()
    {
        Collider[] nearbyTargets = Physics.OverlapSphere(transform.position, workRadius, targetLayerMask);
        int count = 0;
        foreach (var col in nearbyTargets)
        {
            // EN: Only count trees that implement IDamageable (Tree.cs, TreeBarrier, etc.)
            //     This excludes David fruit trees (coconut, durian, rice) which don't implement IDamageable
            // VI: Chỉ đếm các cây có implement IDamageable (Tree.cs, TreeBarrier, v.v.)
            //     Điều này loại trừ các cây trái David (dừa, sầu riêng, lúa) không implement IDamageable
            var damageable = col.GetComponent<IDamageable>();
            if (damageable == null) continue;
            if (damageable.IsDead()) continue;

            count++;
        }
        currentRefillSources = count;
    }

    // EN: Attempts to subtract resources when building a construction. Returns true if successful.
    // VI: Cố gắng trừ tài nguyên khi xây dựng công trình. Trả về true nếu thành công.
    // EN: (Implements ISupply interface)
    // VI: (Implement interface ISupply)
    public bool Supply(int amount)
    {
        if (currentAmount >= amount)
        {
            currentAmount -= amount;
            return true;
        }
        return false;
    }

    // EN: Adds resources based on refill amount multiplied by number of active refill sources.
    // VI: Thêm tài nguyên dựa trên số lượng nạp nhân với số lượng nguồn nạp hoạt động.
    public void Refill()
    {
        currentAmount += refillAmount * currentRefillSources;
    }

    // EN: Checks if the player has no resources left.
    // VI: Kiểm tra xem người chơi có còn tài nguyên nào không.
    public bool IsEmpty()
    {
        return currentAmount <= 0;
    }

    // EN: Draws a wire sphere gizmo in the editor to visualize the work radius when selected.
    // VI: Vẽ gizmo hình cầu dây trong editor để trực quan hóa bán kính làm việc khi được chọn.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        // EN: Draw disable area (likely a typo, means "work area")
        // VI: Vẽ khu vực vô hiệu hóa (có lẽ là lỗi đánh máy, nghĩa là "khu vực làm việc")
        Gizmos.DrawWireSphere(transform.position, workRadius);
    }

}
