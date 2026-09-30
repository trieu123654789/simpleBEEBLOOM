using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Thuan_23127_GhostConstruction : MonoBehaviour
{
    [Header("Cài đặt va chạm")]
    [SerializeField] private LayerMask obstacleLayerMask; 
    
    [Header("Màu sắc hiển thị")]
    [SerializeField] private Material validMaterial;   
    [SerializeField] private Material invalidMaterial; 

    private bool isColliding = false;
    private MeshRenderer[] meshRenderers;

    public bool IsBuildable 
    {
        get { return !isColliding; } 
    }
    // ----------------------------------------------

    private void Awake()
    {
        meshRenderers = GetComponentsInChildren<MeshRenderer>();
    }

    private void OnEnable()
    {
        isColliding = false;
        UpdateMaterial();
    }

    private void Update()
    {
        UpdateMaterial();
    }

    private void OnTriggerStay(Collider other)
    {
        // Kiểm tra xem vật va chạm có nằm trong Layer vật cản không
        if (((1 << other.gameObject.layer) & obstacleLayerMask) != 0)
        {
            isColliding = true;
            
            // --- THÊM DÒNG NÀY ĐỂ SOI LỖI ---
            // Nó sẽ in tên vật cản ra màn hình Console cho bạn biết
            Debug.Log("Ghost đang bị vướng vào: " + other.gameObject.name); 
            // --------------------------------
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & obstacleLayerMask) != 0)
        {
            isColliding = false;
        }
    }

    private void UpdateMaterial()
    {
        Material targetMat = IsBuildable ? validMaterial : invalidMaterial;
        
        if (meshRenderers != null)
        {
            foreach (var mr in meshRenderers)
            {
                if (mr.material != targetMat) 
                {
                    mr.material = targetMat;
                }
            }
        }
    }
}