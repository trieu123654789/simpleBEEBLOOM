using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// =============================================================================
// Pet_AI - Simple AI behavior for farm animals (chickens, ducks, fish, shrimp).
// Pet_AI - Hành vi AI đơn giản cho vật nuôi nông trại (gà, vịt, cá, tôm).
// 
// This script controls autonomous pet behavior:
// - Random walking within a small radius from starting position
// - Periodically returns to home position
// - Pecking animation (eating)
// 
// Script này điều khiển hành vi tự động của thú cưng:
// - Đi bộ ngẫu nhiên trong bán kính nhỏ từ vị trí bắt đầu
// - Định kỳ quay về vị trí gốc
// - Animation mổ thóc (ăn)
// (Note: Optimized surface tracking for aquatic pets)
// =============================================================================
public class Pet_AI : MonoBehaviour
{
    // =========================================================================
    // REFERENCES
    // =========================================================================
    public Animator _animator;

    // =========================================================================
    // MOVEMENT CONFIGURATION
    // =========================================================================
    [Header("Movement / Di chuyển")]
    [Tooltip("Tốc độ di chuyển")]
    public float moveSpeed = 0.1f;
    
    [Tooltip("Bán kính di chuyển tối đa từ vị trí gốc")]
    public float wanderRadius = 2f;
    
    [Tooltip("Thời gian đi bộ mỗi lần (giây)")]
    public float walkTime = 2f;
    
    [Tooltip("Thời gian chờ giữa các lần đi (giây)")]
    public float waitTime = 3f;
    
    [Tooltip("Cơ hội quay về vị trí gốc (0-1)")]
    [Range(0f, 1f)]
    public float returnHomeChance = 0.3f;

    // Countdown timers
    public float walkCounter;
    public float waitCounter;
    public bool isWalking;

    // =========================================================================
    // INTERNAL STATE
    // =========================================================================
    private int _walkDirection;
    private Vector3 _homePosition;  // Starting position
    private bool _isReturningHome;
    
    // Pecking state
    private bool _isPecking;
    private float _peckCounter;

    // =========================================================================
    // Start
    // =========================================================================
    private void Start()
    {
        // Save home position
        _homePosition = transform.position;
        
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        walkCounter = walkTime;
        waitCounter = waitTime;

        ChooseDirection();
    }

    // =========================================================================
    // Update
    // =========================================================================
    private void Update()
    {
        if (isWalking)
        {
            walkCounter -= Time.deltaTime;

            if (_isReturningHome)
            {
                // Move towards home
                MoveTowardsHome();
            }
            else
            {
                // Normal random walk
                switch (_walkDirection)
                {
                    case 0: transform.localRotation = Quaternion.Euler(0f, 0f, 0f); break;
                    case 1: transform.localRotation = Quaternion.Euler(0f, 90f, 0f); break;
                    case 2: transform.localRotation = Quaternion.Euler(0f, -90f, 0f); break;
                    case 3: transform.localRotation = Quaternion.Euler(0f, 180f, 0f); break;
                }
                WalkDirection();
            }

            if (walkCounter <= 0)
            {
                isWalking = false;
                _isReturningHome = false;
                waitCounter = Random.Range(waitTime * 0.5f, waitTime * 1.5f);
            }
        }
        else if (_isPecking)
        {
            _peckCounter -= Time.deltaTime;
            if (_peckCounter <= 0)
            {
                _isPecking = false;
                waitCounter = waitTime;
            }
        }
        else
        {
            waitCounter -= Time.deltaTime;

            if (waitCounter <= 0)
            {
                // 40% peck, 30% return home, 30% random walk
                float roll = Random.value;
                if (roll < 0.4f)
                {
                    StartPecking();
                }
                else if (roll < 0.4f + returnHomeChance)
                {
                    ReturnHome();
                }
                else
                {
                    ChooseDirection();
                }
            }
        }
    }

    // =========================================================================
    // ChooseDirection - Random direction, check if within radius
    // =========================================================================
    private void ChooseDirection()
    {
        _walkDirection = Random.Range(0, 4);
        _isReturningHome = false;
        isWalking = true;
        walkCounter = Random.Range(walkTime * 0.5f, walkTime);
    }

    // =========================================================================
    // ReturnHome - Start moving back to home position
    // =========================================================================
    private void ReturnHome()
    {
        _isReturningHome = true;
        isWalking = true;
        walkCounter = walkTime * 2f;  // More time to get home
    }

    // =========================================================================
    // MoveTowardsHome - Move towards starting position
    // =========================================================================
    private void MoveTowardsHome()
    {
        Vector3 dirToHome = (_homePosition - transform.position);
        dirToHome.y = 0;  // Keep on same Y level
        
        float dist = dirToHome.magnitude;
        
        if (dist < 0.1f)
        {
            // Arrived home
            isWalking = false;
            _isReturningHome = false;
            waitCounter = waitTime;
            return;
        }
        
        // Face home direction
        if (dirToHome.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(dirToHome.normalized);
        }
        
        // Move towards home
        transform.position += dirToHome.normalized * (moveSpeed * Time.deltaTime);
    }

    // =========================================================================
    // WalkDirection - Move forward, but stay within wander radius
    // =========================================================================
    private void WalkDirection()
    {
        // Calculate next position
        Vector3 nextPos = transform.position + transform.forward * (moveSpeed * Time.deltaTime);
        
        // Check distance from home
        float distFromHome = Vector3.Distance(nextPos, _homePosition);
        
        if (distFromHome > wanderRadius)
        {
            // Too far - stop and wait, will return home next
            isWalking = false;
            waitCounter = 0.5f;  // Short wait then return
            return;
        }
        
        // Check for obstacles
        if (!Physics.Raycast(transform.position, transform.forward, 0.15f))
        {
            transform.position = nextPos;
        }
        else
        {
            // Hit obstacle - change direction
            isWalking = false;
            waitCounter = 0.5f;
            ChooseDirection();
        }
    }

    // =========================================================================
    // StartPecking
    // =========================================================================
    private void StartPecking()
    {
        _isPecking = true;
        _peckCounter = Random.Range(2f, 3f);
        // _animator.SetInteger("animation", 4);
    }

    // =========================================================================
    // OnDrawGizmosSelected - Visualize wander radius
    // =========================================================================
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = Application.isPlaying ? _homePosition : transform.position;
        Gizmos.DrawWireSphere(center, wanderRadius);
    }
}