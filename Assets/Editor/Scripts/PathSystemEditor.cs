using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


#region Globals
public enum PointsToolsCreationMode
{
    OnEnd,
    OnAheadSelectedObject,
    OnBehindSelectedObject
}

public class PointsToolsEditorGlobals
{
    public static PathPoint selectedPoint = null;
    public static PathSystem usedPointSystem = null;
    public static bool makeCurveOnCreate = true;
    public static bool pickObjectOnPosChange = false;
    public static bool createByEdCameraPosition = false;
    public static PointsToolsCreationMode creationMode = PointsToolsCreationMode.OnEnd;
    public static Vector3 createVec = new Vector3(-1, 0, 0);
    public static bool showHandles = true;
    public static bool showOnlyLine = false;
    public static bool showCurveHandle = true;

    public static bool CheckPathSystem()
    {
        if (usedPointSystem == null)
        {
            usedPointSystem = SceneView.FindObjectOfType<PathSystem>();
            Debug.LogWarning("Selected first founded path system because previously non any has been path system selected!");
        }

        return usedPointSystem != null;
    }

    public static void Separator(int i_height = 1)
    {
        Rect rect = EditorGUILayout.GetControlRect(false, i_height);
        rect.height = i_height;
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
    }

    public static void GoToGameObject()
    {
        if (selectedPoint != null)
            Selection.activeObject = selectedPoint;
    }

    //TODO: Make a better function 
    public static Vector2 CalcCurvePoint(Vector2 p1, Vector2 p2)
    {
        return (p1 - p2);
    }

    public static void Create(Vector3 example_new_position, PathPoint example = null)
    {
        if (selectedPoint == null)
            example = SceneView.FindObjectOfType<PathPoint>();
        else
            example = selectedPoint;


        if (example == null)
        {
            Debug.LogError("No example found for creation");
            return;
        }

        if (usedPointSystem == null)
        {
            usedPointSystem = SceneView.FindObjectOfType<PathSystem>();
            Debug.LogWarning("Selected first founded path system because previously non any has been path system selected!");
        }


        //We are want to create new PathPoint
        PathPoint point = SceneView.Instantiate(example, example_new_position, Quaternion.identity, usedPointSystem == null ? null :
            usedPointSystem.gameObject.transform);

        //Set new name 
        point.name = usedPointSystem.pathPointsList[0].name + "_" + usedPointSystem.pathPointsList.Count;

        point.useCurve = makeCurveOnCreate;

        List<PathPoint> pathlist = usedPointSystem.pathPointsList;

        if (makeCurveOnCreate)
        {
            if (pathlist.Count != 1 && pathlist.IndexOf(selectedPoint) == pathlist.Count)
                point.curvePoint = CalcCurvePoint(pathlist[pathlist.IndexOf(selectedPoint)].transform.position, pathlist[pathlist.IndexOf(selectedPoint) + 1].transform.position);
        }

        switch (creationMode)
        {
            case PointsToolsCreationMode.OnEnd:
                pathlist.Add(point);
                break;
            case PointsToolsCreationMode.OnBehindSelectedObject:
                pathlist.Insert(pathlist.IndexOf(selectedPoint), point);
                break;
            case PointsToolsCreationMode.OnAheadSelectedObject:
                pathlist.Insert(pathlist.IndexOf(selectedPoint) + 1, point);
                break;
        }

    }
}
#endregion

#region Path Point Editor
[CustomEditor(typeof(PathPoint), true), CanEditMultipleObjects]
public class PathPointEditor : Editor
{
    protected virtual void OnSceneGUI()
    {
        PathPoint point = (PathPoint)target;

        //Push object to globals if we are select the point 
        PointsToolsEditorGlobals.selectedPoint = point;
        if (PointsToolsEditorGlobals.usedPointSystem == null)
            PointsToolsEditorGlobals.usedPointSystem = PointsToolsEditorGlobals.selectedPoint.GetComponentInParent<PathSystem>();
    }
}
#endregion

public class PathSystemEditorWindow : EditorWindow
{
    private bool _groupEnabled;
    private GUIStyle buttonToolsGUIStyle;
    private GUIStyle buttonSelectionGUIStyle;
    private GUIStyle HeaderTextGUIStyle;
    private Camera editorCamera;

    [MenuItem("Window/Path System")]
    static void Init()
    {
        PathSystemEditorWindow window = (PathSystemEditorWindow)EditorWindow.GetWindow(typeof(PathSystemEditorWindow));
        GUIContent guiContent = new GUIContent();
        guiContent.text = "Path System";
        //TODO: Icon
        window.titleContent = guiContent;
        window.autoRepaintOnSceneChange = true;
        window.Show();
    }
    private void OnGotoSystemButtonClick()
    {
        PointsToolsEditorGlobals.CheckPathSystem();
        Selection.activeObject = PointsToolsEditorGlobals.usedPointSystem;
    }

    private void OnSwitchCreationModeClick(PointsToolsCreationMode mode)
    {
        PointsToolsEditorGlobals.creationMode = mode;
    }

    private void OnGotoGameObjectButtonClick()
    {
        PointsToolsEditorGlobals.GoToGameObject();
    }

    private void OnDeleteButtonClick()
    {
        EditorGUI.BeginChangeCheck();
        if (PointsToolsEditorGlobals.selectedPoint == null || !PointsToolsEditorGlobals.CheckPathSystem())
            return;

        //Remove object from list
        PointsToolsEditorGlobals.usedPointSystem.pathPointsList.Remove(PointsToolsEditorGlobals.selectedPoint);
        //Remove it from scene
        DestroyImmediate(PointsToolsEditorGlobals.selectedPoint.gameObject);
        //Select previous object from list if we can
        PointsToolsEditorGlobals.selectedPoint = PointsToolsEditorGlobals.usedPointSystem.pathPointsList[PointsToolsEditorGlobals.usedPointSystem.pathPointsList.Count - 1];

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(PointsToolsEditorGlobals.selectedPoint, "Deleted point");
            Undo.RecordObject(PointsToolsEditorGlobals.usedPointSystem, "Changed array in point system");
            EditorUtility.SetDirty(PointsToolsEditorGlobals.selectedPoint);
        }
    }

    private void SearchEditorCamera()
    {
        if (SceneView.currentDrawingSceneView)
            editorCamera = SceneView.currentDrawingSceneView.camera;
    }

    private void OnCreateButtonClick()
    {
        Vector3 position = PointsToolsEditorGlobals.selectedPoint != null ? PointsToolsEditorGlobals.selectedPoint.transform.position + PointsToolsEditorGlobals.createVec : Vector3.zero;

        if (PointsToolsEditorGlobals.createByEdCameraPosition)
        {
            SearchEditorCamera();
            if (editorCamera != null)
                position = new Vector3(editorCamera.transform.position.x, editorCamera.transform.position.y, 0.0f);
            else
                Debug.LogError("editorCamera is null! Can't set position from editor camera");
        }

        PointsToolsEditorGlobals.Create(position);
    }

    protected void OnGUI()
    {
        HeaderTextGUIStyle = new GUIStyle(GUI.skin.label);
        HeaderTextGUIStyle.fontSize = 14;
        
        buttonToolsGUIStyle = new GUIStyle(GUI.skin.button);
        buttonToolsGUIStyle.fixedHeight = 22;
        buttonToolsGUIStyle.fixedWidth = 36;

        buttonSelectionGUIStyle = new GUIStyle(GUI.skin.button);
        GUILayout.Label($"Tools:", HeaderTextGUIStyle);
        GUILayout.BeginHorizontal();
        {
            if (GUILayout.Button($"C", buttonToolsGUIStyle))
                OnCreateButtonClick();
            if (GUILayout.Button($"D", buttonToolsGUIStyle))
                OnDeleteButtonClick();
            if (GUILayout.Button($"TS", buttonToolsGUIStyle))
                OnGotoSystemButtonClick();
            if (GUILayout.Button($"TGO", buttonToolsGUIStyle))
                OnGotoGameObjectButtonClick();
        }
        GUILayout.EndHorizontal();

        PointsToolsEditorGlobals.Separator();

        GUILayout.Label($"Creation Mode:", HeaderTextGUIStyle);
        string[] creationToolbarElements = { "End", "Ahead", "Behind" };
        PointsToolsEditorGlobals.creationMode = (PointsToolsCreationMode)GUILayout.Toolbar((int)PointsToolsEditorGlobals.creationMode, creationToolbarElements);
        
        PointsToolsEditorGlobals.Separator();
        GUILayout.Label($"Scene stats:", HeaderTextGUIStyle);
        GUILayout.Label($"Path systems:{ FindObjectsOfType<PathSystem>(true).Length }");
        GUILayout.Label($"Points:{ FindObjectsOfType<PathPoint>(true).Length }");
        GUILayout.Label($"Active path systems:{ FindObjectsOfType<PathSystem>().Length }");
        GUILayout.Label($"Active points:{ FindObjectsOfType<PathPoint>().Length }");

        PointsToolsEditorGlobals.Separator();
        GUILayout.Label($"Options:", HeaderTextGUIStyle);
        //Some custom options
        {
            PointsToolsEditorGlobals.showOnlyLine = GUILayout.Toggle(PointsToolsEditorGlobals.showOnlyLine, "Show only lines");
            PointsToolsEditorGlobals.showHandles = GUILayout.Toggle(PointsToolsEditorGlobals.showHandles, "Show handles");
            PointsToolsEditorGlobals.showCurveHandle = GUILayout.Toggle(PointsToolsEditorGlobals.showCurveHandle, "Show curve handle");
            PointsToolsEditorGlobals.createByEdCameraPosition = GUILayout.Toggle(PointsToolsEditorGlobals.createByEdCameraPosition, "Create by editor camera position");
            PointsToolsEditorGlobals.pickObjectOnPosChange = GUILayout.Toggle(PointsToolsEditorGlobals.pickObjectOnPosChange, "Pick object when handle is used");
            PointsToolsEditorGlobals.makeCurveOnCreate = GUILayout.Toggle(PointsToolsEditorGlobals.makeCurveOnCreate, "Make curve on create object");
            PointsToolsEditorGlobals.Separator();
            PointsToolsEditorGlobals.createVec = EditorGUILayout.Vector2Field("Addition Vector (Addition to create point)", PointsToolsEditorGlobals.createVec);
        }

        PointsToolsEditorGlobals.Separator();

        PathSystem pathSystem = PointsToolsEditorGlobals.usedPointSystem;
        if (pathSystem)
        {
            GUILayout.Label($"Path system:", HeaderTextGUIStyle);
            GUILayout.Label($"Name:{pathSystem.name} ID:{pathSystem.GetInstanceID()}\n" +
                $"Position:{pathSystem.transform.position }");
            PointsToolsEditorGlobals.Separator();
        }


        PathPoint point = PointsToolsEditorGlobals.selectedPoint;
        if (point)
        {
            GUILayout.Label($"Current point:", HeaderTextGUIStyle);
            GUILayout.Label($"Name:{point.name}\n" +
                $"Position:{point.transform.position }");
            GUILayout.Label($"eventOnEndForAll: {point.eventOnEndForAll}");

            if (point.eventSpecial.GetPersistentEventCount() != 0)
                for (int i = 0; i <= point.eventSpecial.GetPersistentEventCount() - 1; i++)
                    GUILayout.Label($"OnEnd:{point.eventSpecial.GetPersistentMethodName(i)}");


            point.useCurve = GUILayout.Toggle(point.useCurve, "Use curve");
        }
    }
}

#region Path System Editor 
[CustomEditor(typeof(PathSystem), true), CanEditMultipleObjects]
public class PathSystemEditor : Editor
{
    [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
    static void DrawCustomGizmos(PathSystem path, GizmoType gizmoType)
    {
        PathPoint current_point = null;
        PathPoint prev_point = null;

        foreach (var point in path.pathPointsList)
        {
            if (point == null)
                continue;

            current_point = point;

            if (prev_point != null)
            {
                Gizmos.color = path.lineColor;

                if (prev_point.useCurve)
                {
                    //PathSystem.CubeFollowedByPath(prev_point.transform.position, current_point.curvePoint, current_point.transform.position);
                    Gizmos.color = path.lineColor;
                    PathSystem.LineFollowedByPath(prev_point.transform.position, current_point.curvePoint, current_point.transform.position);
                }
                else
                    Gizmos.DrawLine(prev_point.transform.position, current_point.transform.position);
            }

            prev_point = current_point;
        }
    }

    public virtual void OnSceneGUI()
    {
        PathSystem path = (PathSystem)target;
        if (!path.pathPointsList.Any())
            return;

        if (!PointsToolsEditorGlobals.showOnlyLine)
        {
            PathPoint current_point = null;
            PathPoint prev_point = null;

            foreach (var point in path.pathPointsList)
            {
                if (point == null)
                    continue;

                current_point = point;

                if (prev_point != null)
                {
                    if ((PointsToolsEditorGlobals.selectedPoint == prev_point || PointsToolsEditorGlobals.selectedPoint == point) && prev_point.useCurve && PointsToolsEditorGlobals.showCurveHandle)
                    {
                        point.curvePoint = Handles.DoPositionHandle(point.curvePoint, Quaternion.identity);
                        Handles.color = Color.gray;
                        Handles.DrawLine(prev_point.transform.position, point.curvePoint);
                        Handles.DrawLine(point.curvePoint, point.transform.position);
                    }
                }

                EditorGUI.BeginChangeCheck();
                {
                    Vector3 position = Vector3.zero;

                    if (PointsToolsEditorGlobals.showHandles)
                        position = Handles.DoPositionHandle(point.transform.position, Quaternion.identity);
                    else
                        position = point.transform.position;

                    //FIXME: Don't show if we are hold shift key                    
                    Handles.color = PointsToolsEditorGlobals.selectedPoint != point ? Color.white : Color.red;
                    float zoom = SceneView.currentDrawingSceneView.camera.orthographicSize;
                    if (Handles.Button(position, Quaternion.identity, 0.05f * zoom, 0.07f * zoom, Handles.RectangleHandleCap))
                    {
                        PointsToolsEditorGlobals.selectedPoint = point;
                        PointsToolsEditorGlobals.usedPointSystem = point.GetComponentInParent<PathSystem>();
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        //TODO: Working Undo Redo
                        //      With working hot keys
                        Undo.RecordObject(point.gameObject, "Changed point position");
                        EditorUtility.SetDirty(point);

                        //TODO: Recalculate position for curve
                        point.transform.position = position;

                        //Set selected object
                        if (PointsToolsEditorGlobals.pickObjectOnPosChange)
                            PointsToolsEditorGlobals.selectedPoint = point;
                    }
                }

                prev_point = current_point;

            }
        }
    }
}
#endregion

