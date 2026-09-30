using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Tree : MonoBehaviour, IDamageable
{
    /* 
    Tree: vn() --> Cây 
    Manage the response of plants when affected by the external environment. 
    ----------------------------------
    Message By Hồng Sơn: 
    In the future, we want to expand further on the salinity tolerance of different crop species.
    */

    [Header("Stats")]
    [Tooltip("Máu khởi tạo của cây. Mặc định 200.\nInitial HP per tree.")]
    [SerializeField] private int initialHealth = 200;

    // runtime privates
    // FIX (VU2): chuyển từ `static` → instance để mỗi cây có HP riêng.
    // Trước đây static khiến TẤT CẢ cây chia sẻ chung 1 thanh máu —
    // Enemy đánh 1 cây thì cả vườn cùng héo/chết → bug "tác động độ mặn game 1".
    // FIX (VU2): changed from static to instance so each tree has its own HP.
    private int currentHealh;
    public Animator anim;
    AnimatorStateInfo stateInfo;
    public TextMeshProUGUI hp;
    private int count;
    public AudioSource crackSound;
    private bool hasFallen = false;
    // private int condition = 0;

    [Header("Shadow Color (Bad State)")]
    private static readonly int ShadowColorID = Shader.PropertyToID("_Shadow_Color");
    private Renderer[] treeRenderers;
    private Dictionary<Material, Color> originalShadowColors = new Dictionary<Material, Color>();
    private bool hasSwitchedToBad = false;

    // Getters
    public int Health
    {
        get { return currentHealh; }
    }

    void Start()
    {
        //currentHealh = health;
        currentHealh = initialHealth;
        anim = GetComponent<Animator>();
        stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        // Debug.Log("Tree_currentHealh"+ currentHealh);

        // Tự động lấy AudioSource gắn trên GameObject
        crackSound = GetComponent<AudioSource>();
        crackSound.volume = 0.1f;  // Âm lượng từ 0.0 (im lặng) đến 1.0 (to nhất)
        crackSound.spatialBlend = 1.0f;       // 3D âm thanh
        crackSound.minDistance = 5f;          // Dưới 5m: âm thanh rõ
        crackSound.maxDistance = 30f;         // Trên 30m: hầu như im lặng
        crackSound.rolloffMode = AudioRolloffMode.Logarithmic;
        // Kiểm tra có gắn AudioSource không
        if (crackSound == null)
        {
            Debug.LogWarning("Không tìm thấy AudioSource trên " + gameObject.name);
        }

        // Cache tất cả Renderer và lưu Shadow_Color gốc
        treeRenderers = GetComponentsInChildren<Renderer>();
        foreach (var rend in treeRenderers)
        {
            foreach (var mat in rend.materials)
            {
                if (mat.HasProperty(ShadowColorID) && !originalShadowColors.ContainsKey(mat))
                {
                    originalShadowColors[mat] = mat.GetColor(ShadowColorID);
                }
            }
        }
    }
    // private bool created = false;
    // int tick=0;

    public void TakeDamage(int damage)
    {
        currentHealh -= damage;
        //Debug.Log("TakeDamage_currentHealh: " + currentHealh);
        //hp.text = currentHealh.ToString();
    
        if (currentHealh <= -20)
        {
            
            Dictionary<string, string> args = new Dictionary<string, string> {
                {"idP", ConnectionManager.Instance.GetConnectionId()},
                {"idT", gameObject.GetInstanceID()+"" }};

                ConnectionManager.Instance.SendExecutableAsk("delete_tree", args);

            // Debug.Log("currentHealh < 0: ");
            // anim.Play("Tree_Die", -1,0f);

            if (GameUI.Instance != null  && gameObject != null)
            {
                GameUI.Instance.DeletePlayer(gameObject);
            }
            Die();
        }
        // return currentHealh;
    }

    void Update()
    {           
        count = currentHealh;
        //Debug.Log("VoidUpdate_currentHealh: " + count);
        // if (count > 0)
        // {
        //     hp.text = count.ToString();
        // }
        // else hp.text = "Tree Die";

        // Test Animation
        if(Input.GetKeyDown("1"))
        {
        anim.Play("Tree_Good", -1,0f);
        }
        if(Input.GetKeyDown("2"))
        {
        anim.Play("Tree_Bad", -1,0f);
        StartCoroutine(PlayPartOfAudio(0f, 2.0f));
        }
        if(Input.GetKeyDown("3"))
        {
        anim.Play("Tree_Die", -1,0f);
        StartCoroutine(PlayPartOfAudio(0f, 2.0f));
        }


        // Check Condition Tree
        if (count < 150 && count > 120)
        {
            //condition = 1; 
            // Debug.Log("khoi dong animation Tree Bad: ");
            if (!stateInfo.IsName("Tree_Bad"))
            {
                anim.Play("Tree_Bad");
                Debug.Log("Active Animation Tree_Bad");
                StartCoroutine(PlayPartOfAudio(0f, 2.0f));
            }

            // Đổi Shadow_Color sang #744A4A khi vào trạng thái bad
            if (!hasSwitchedToBad)
            {
                SetShadowColor(new Color(0.455f, 0.290f, 0.290f, 0f)); // #744A4A
                hasSwitchedToBad = true;
            }
            //anim.Play("Tree_Bad");
        }
        if (count < 1 && count > -20)
        {
            //condition = 2;
            // Debug.Log("khoi dong animation Tree die: ");
            if (!stateInfo.IsName("Tree_Die"))
            {
                anim.Play("Tree_Die");
                Debug.Log("Active Animation Tree_Die");
                StartCoroutine(PlayPartOfAudio(0f, 2.0f));
            }
            //anim.Play("Tree_Die",-1,0f);

        }
        
        // Control animation 
        // if(condition==1)
        // {
        //     Debug.Log("khoi dong animation Tree Bad: ");
        //     anim.Play("Tree_Bad", -1,0f);
        // }
        // if(condition==2)
        // {
        //     Debug.Log("khoi dong animation Tree die: ");
        //     anim.Play("Tree_Die", -1,0f);
        // }

        //  Debug.Log("sent to GAMA: ");
        // tick++;
        // if ( GameUI.Instance != null && gameObject != null)
        // {    
        //     // tick=0;        
        //     // Debug.Log("sent to GAMA: " + gameObject);

        //     GameUI.Instance.UpdateConstructionPosition(gameObject);
        //     // created = true;
        // }
        
    }

   
    /// <summary>
    /// Đổi thuộc tính _Shadow_Color trên tất cả material của cây.
    /// </summary>
    private void SetShadowColor(Color color)
    {
        if (treeRenderers == null) return;
        foreach (var rend in treeRenderers)
        {
            if (rend == null) continue;
            foreach (var mat in rend.materials)
            {
                if (mat.HasProperty(ShadowColorID))
                {
                    mat.SetColor(ShadowColorID, color);
                }
            }
        }
    }

    public void Die()
    {
        // Debug.Log("Xoa Cay: ");
        if ( GameUI.Instance != null )
        {     
            GameUI.Instance.CountDeadTree(); 
        }
        // Thống kê: cây ăn quả/lương thực chết (PFB_Rice, PFB_Coconut, PFB_Banana, PFB_Orange...).
        // Statistics: a fruit/crop tree died.
        if (StatisticsManager.Instance != null)
            StatisticsManager.Instance.IncreaseFruitTreeDeathCount();
        Destroy(gameObject);
    }

    public bool IsDead()
    {
        // Debug.Log("Tree mau ve khong: ");
        return currentHealh <= 0;
    }

    public void Fall()
    {   //Kiểm tra và phát âm thanh gãy đổ
        if (hasFallen || crackSound == null) return;

        // Gọi animation đổ cây (nếu có)
        // GetComponent<Animator>().SetTrigger("Fall");

        // Phát âm thanh gãy đổ
        //crackSound.Play();
        StartCoroutine(PlayPartOfAudio(0f, 2.0f));
        hasFallen = true;
    }

    IEnumerator PlayPartOfAudio(float startTime, float duration)
    { 
        //Phát âm thanh chỉ trong khoảng thời gian ngắn
        crackSound.time = startTime;
        crackSound.Play();

        yield return new WaitForSeconds(duration);
        crackSound.Stop();
    }
}
