using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SectionObject
{
    public GameObject trigger;
    public GameObject plane;
    public GameObject interiorObjectsRoot;
    public List<GameObject> interiorObjects = new List<GameObject>();
    public Vector3 planeEndPoint;
    public Vector3 planeMiddlePoint;
    public int index;
}

public class Background : MonoBehaviour
{
    private const int COUNT_OF_SECTIONS = 4;

    [SerializeField]
    private GameObject _root;
    [SerializeField]
    private List<SectionObject> _sectionObjects = new List<SectionObject>();

    protected void OnGUI()
    {
        for (int i = 0; i < _sectionObjects.Count; i++)
        {
            SectionObject so = _sectionObjects[i];
            if(so != null)
                GUILayout.Label($"{i} {so.trigger.GetComponent<BackgroundResetTrigger>().backgroundObject == null}{so.planeEndPoint} {so.planeMiddlePoint} {so.plane.name}");
        }
    }

    protected void OnDrawGizmos()
    {
        foreach (SectionObject section in _sectionObjects)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(section.planeMiddlePoint, 1);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(section.planeEndPoint, 2);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(section.plane.transform.position, Vector3.one);

        }
    }

    protected void Start()
    {
        //Create a root for sections
        _root = new GameObject();
        _root.name = $"Root{_root.GetInstanceID()}";
        _root.transform.position = transform.position;

        for (int i = 0; i < COUNT_OF_SECTIONS; i++)
        {
            //Debug.Log(i);

            //Create new section
            SectionObject section = new SectionObject();

            //1 Step
            //Create plane 
            Vector3 planeMiddlePoint = Vector3.zero;

            section.plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            section.plane.name = $"Plane{section.plane.GetInstanceID()}";
            section.plane.transform.parent = _root.transform;
            section.plane.transform.localScale = new Vector3(3.0f, 1.0f, 20.0f);

            planeMiddlePoint = new Vector3(section.plane.transform.position.x,
                section.plane.transform.position.y,
                section.plane.transform.position.z + (section.plane.transform.localScale.z * 10 * i));

            section.plane.transform.position = planeMiddlePoint;

            BackgroundMovement movement = section.plane.AddComponent<BackgroundMovement>();
            movement.backgroundObject = section;
            movement.speed = 20.0f;

            //2 Step
            //Create trigger
            section.planeEndPoint = new Vector3(section.plane.transform.position.x,
                section.plane.transform.position.y,
                (section.plane.transform.localScale.z * 5 * (i + 1)) + (i * 100)); //Idk but it's works

            section.trigger = new GameObject($"Trigger{section.plane.name}");
            section.trigger.transform.parent = section.plane.transform;
            section.trigger.transform.position = section.planeEndPoint;

            section.planeMiddlePoint = planeMiddlePoint;

            BackgroundResetTrigger resetTrigger = section.trigger.AddComponent<BackgroundResetTrigger>();
            resetTrigger.backgroundPoint = section.planeEndPoint;

            BoxCollider boxCollider = section.trigger.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(30.0f, 50.0f, 10.0f);
            boxCollider.isTrigger = true;

            section.interiorObjectsRoot = new GameObject($"interiorObjectsRoot{section.plane.GetInstanceID()}");
            section.interiorObjectsRoot.transform.parent = section.plane.transform;
            //section.interiorObjectsRoot.transform.position = -section.planeEndPoint;
            section.interiorObjectsRoot.transform.localScale = Vector3.one;

            //3 Step
            //Random cubes
            /*  
              for (int s = 1; s <= 3; s++)
              {
                  for (int j = 0; j <= (int)section.plane.transform.localScale.z / 2; j++)
                  {
                      GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                      cube.name = $"CubeL{section.plane.GetInstanceID()}{j}";
                      cube.transform.parent = section.interiorObjectsRoot.transform;
                      cube.transform.localScale = new Vector3(2.0f, Random.Range(16.0f, 45.0f * s), Random.Range(0.5f, 1.0f));
                      cube.transform.localPosition = new Vector3(section.plane.transform.localScale.x * 2 * s, (section.plane.transform.parent.position.y / 2) * s, j);
                      section.interiorObjects.Add(cube);
                  }

                  for (int j = 0; j <= (int)section.plane.transform.localScale.z / 2; j++)
                  {
                      GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                      cube.name = $"CubeR{section.plane.GetInstanceID()}{j}";
                      cube.transform.parent = section.interiorObjectsRoot.transform;
                      cube.transform.localScale = new Vector3(2.0f, Random.Range(16.0f, 45.0f * s), Random.Range(0.5f, 1.0f));
                      cube.transform.localPosition = new Vector3(-section.plane.transform.localScale.x * 2 * s, (section.plane.transform.parent.position.y / 2) * s, j);
                      section.interiorObjects.Add(cube);
                  }
              }
            */

            section.index = i;

            _sectionObjects.Add(section);

            if (i % 2 == 0 && i != 0)
                resetTrigger.backgroundObject = (i + 1) > _sectionObjects.Count - 1 ? _sectionObjects[0] : _sectionObjects[i + 1];           
            else if (i % 2 != 0)
                resetTrigger.backgroundObject = _sectionObjects[i];
            
        }
    }

    protected void Update()
    {
        
    }
}
