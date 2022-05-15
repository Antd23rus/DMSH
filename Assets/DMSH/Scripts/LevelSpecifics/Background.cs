using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SectionObject
{
    public GameObject trigger;
    public GameObject plane;
    public List<GameObject> interiorObjects;
}

public class Background : MonoBehaviour
{
    private const int COUNT_OF_SECTIONS = 2;

    [SerializeField]
    private GameObject _root;
    [SerializeField]
    private List<SectionObject> _sectionObjects = new List<SectionObject>();
    
    protected void Start()
    {
        _root = new GameObject("Root");
        _root.name = $"Root{_root.GetInstanceID()}";
        _root.transform.position = transform.position;

        for (int i = 0; i < COUNT_OF_SECTIONS; i++)
        {
            SectionObject section = new SectionObject();
            //1 Step
            //Create place 
            section.plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            section.plane.name = "Plane" + _root.name;
            section.plane.transform.parent = _root.transform;
            section.plane.transform.localScale = new Vector3(3.0f, 1.0f, 20.0f);
            section.plane.transform.position = new Vector3(section.plane.transform.position.x,
                section.plane.transform.position.y,
                section.plane.transform.position.z + (section.plane.transform.localScale.z * 10 * i));

            //2 Step
            //Create trigger
            Vector3 end = new Vector3(section.plane.transform.position.x,
                section.plane.transform.position.y,
                (section.plane.transform.localScale.z * 5 * (i + 1)) + (i * 100)); //Idk but it's works

            section.trigger = new GameObject("Trigger" + section.plane.name);
            section.trigger.transform.parent = section.plane.transform;
            section.trigger.transform.position = end;

            BackgroundResetTrigger resetTrigger = section.trigger.AddComponent<BackgroundResetTrigger>();
            resetTrigger.backgroundObject = section.plane;
            resetTrigger.backgroundPoint = end;

            BoxCollider boxCollider = section.trigger.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(30.0f, 50.0f, 10.0f);
            boxCollider.isTrigger = true;

            _sectionObjects.Add(section);
        }

    }

    protected void Update()
    {
        
    }
}
