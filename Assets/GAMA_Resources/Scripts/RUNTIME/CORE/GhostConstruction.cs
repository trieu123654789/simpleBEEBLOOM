using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostConstruction : MonoBehaviour
{
    [SerializeField] LayerMask collideLayerMask;
    [SerializeField] private LayerMask connectorLayerMask;
    [SerializeField] private float connectorCheckRadius = 1;
    [SerializeField] private bool useIdentityRotation = false;

    [Header("Ghost Construction Material Settings")]
    [SerializeField] private Material validMaterial;
    [SerializeField] private Material invalidMaterial;


    private bool buildable;
    // EN: Set of obstacle colliders currently overlapping the ghost trigger.
    //     Using a HashSet (instead of a single bool) prevents flicker when
    //     multiple obstacles enter/exit at different frames — the original
    //     `collide` boolean would be wrongly cleared by ANY OnTriggerExit
    //     even when other obstacles were still inside.
    // VI: Tập các collider chướng ngại đang chồng lên trigger ghost.
    //     Dùng HashSet (thay vì 1 boolean) tránh nhấp nháy khi nhiều vật cùng
    //     vào/ra ở các frame khác nhau — biến `collide` cũ sẽ bị reset sai
    //     bởi MỘT OnTriggerExit dù các vật khác vẫn còn bên trong.
    private readonly HashSet<Collider> _overlappingObstacles = new HashSet<Collider>();
    private bool collide => _overlappingObstacles.Count > 0;

    // Getter
    public bool IsBuildable {
        get { return buildable; }
    }
    public bool UseIdentityRotation {
        get { return useIdentityRotation; }
    }

    void Start()
    {
        ghostifyModel(gameObject, invalidMaterial);
    }

    void Update()
    {
        UpdateConstructionValidity();
        if (buildable) ghostifyModel(gameObject, validMaterial);
        else ghostifyModel(gameObject, invalidMaterial);
    }

    void Awake()
    {
        _overlappingObstacles.Clear();
    }

    private bool IsObstacleLayer(int layer)
    {
        return collideLayerMask == (collideLayerMask | (1 << layer));
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsObstacleLayer(other.gameObject.layer))
            _overlappingObstacles.Add(other);
    }

    void OnTriggerStay(Collider other)
    {
        if (IsObstacleLayer(other.gameObject.layer))
            _overlappingObstacles.Add(other);
    }

    void OnTriggerExit(Collider other)
    {
        if (IsObstacleLayer(other.gameObject.layer))
            _overlappingObstacles.Remove(other);
    }

    void OnDisable()
    {
        // Reset state khi ghost bị tắt/destroy để tránh giữ tham chiếu cũ.
        _overlappingObstacles.Clear();
    }

    private void UpdateConstructionValidity(){
        Collider[] colliders = Physics.OverlapSphere(transform.position, connectorCheckRadius, connectorLayerMask);
        if(colliders.Length > 0){
            FindConnector(colliders);
        }
        else
        {
            buildable = false;
        }
    }

    private void FindConnector(Collider[] colliders){
        Connector bestsurfaceConnector = null;
        foreach(Collider collider in colliders){
            Connector connector = collider.GetComponent<Connector>();
            if(connector.canConnectTo && !connector.transform.IsChildOf(transform)){
                bestsurfaceConnector = connector;
                break;
            }
        }
        if (bestsurfaceConnector)
        {
            var constructionConnector = GetComponentInChildren<Connector>();
            SnapTwoConnector(bestsurfaceConnector, constructionConnector);
        }
        else
        {
            buildable = false;
        }
    }

    private void SnapTwoConnector(Connector surfaceConnector, Connector constructionConnector){  
        if (surfaceConnector.Type == constructionConnector.Type)
        {
            transform.position = surfaceConnector.transform.position - (constructionConnector.transform.position - transform.position);
            // useIdentityRotation → giữ nguyên rotation gốc của prefab (vd: cây đứng thẳng)
            if (!useIdentityRotation)
                transform.rotation = surfaceConnector.transform.rotation;
            buildable = !collide;
        }
    }

    // update ghost construction material
    private void ghostifyModel(GameObject model, Material ghostMaterial = null){
        if(ghostMaterial != null){
            MeshRenderer[] meshRenderers = model.GetComponentsInChildren<MeshRenderer>();
            foreach(MeshRenderer meshRenderer in meshRenderers){
                meshRenderer.material = ghostMaterial;
            }
        }
    }

}
