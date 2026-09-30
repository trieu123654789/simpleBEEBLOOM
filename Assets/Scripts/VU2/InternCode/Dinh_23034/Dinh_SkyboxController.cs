using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dinh_SkyboxController : MonoBehaviour
{
    private static readonly int Rotation = Shader.PropertyToID("_Rotation");

    // Tốc độ mây bay, bạn có thể chỉnh giá trị này trong Inspector
    [Tooltip("Tốc độ xoay của skybox, giá trị càng lớn mây bay càng nhanh.")]
    public float rotationSpeed = 0.5f;

    void Update()
    {
        // Lấy giá trị xoay hiện tại của skybox và cộng thêm một chút mỗi frame
        // Time.time sẽ giúp việc xoay mượt mà và không phụ thuộc vào frame rate
        RenderSettings.skybox.SetFloat(Rotation, Time.time * rotationSpeed);
    }
}