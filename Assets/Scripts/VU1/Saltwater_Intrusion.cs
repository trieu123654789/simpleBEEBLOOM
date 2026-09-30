// using UnityEngine;
//
// public class Saltwater_Intrusion : MonoBehaviour
// {
//     public Vector3 pointA;
//     public Vector3 pointB;
//     public float moveTime = 3f;
//
//     private float timer;
//     public bool moving;
//
//     public void StartMove()
//     {
//         timer = 0f;
//         moving = true;
//         transform.position = pointA; // bắt đầu tại A
//     }
//
//     void Update()
//     {
//         if (!moving) return;
//
//         timer += Time.deltaTime;
//         float t = timer / moveTime;
//
//         transform.position = Vector3.Lerp(pointA, pointB, t);
//
//         if (t >= 1f)
//             moving = false;
//     }
// }
