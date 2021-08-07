using Jundroo.SimplePlanes.ModTools;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MapToDesigner : MonoBehaviour
{
    [SerializeField] private Transform CopiedObjectParent;
    [SerializeField] private Transform ButtonsParent;

    private GameObject BackdropObject;
    private GameObject BackdropWaterObject;
    private GameObject BackdropSkyObject;
    private string BackdropMapName;

    public Vector3 BackdropOffset = Vector3.zero;
    public Vector3 BackdropAngles = Vector3.zero;
    public float BackdropRenderDistance = 50000f;

    private bool InLevelPrevious = false;
    private bool InLevelCurrent = false;
    private bool InDesignerPrevious = false;
    private bool InDesignerCurrent = false;
    private bool PausedPrevious = false;
    private bool PausedCurrent = false;

    // Settings
    private bool AutosaveMap = true;

    // File paths
    private string PathNachSave;
    private string PathMapToDes;
    private string PathAutosave;
    private string NameNachSave = "NACHSAVE";
    private string NameMapToDes = "MAPTODES";
    private string NameAutosave = "AUTOSAVE";

    private string MtdHeader = "NACHSAVEMTDSET";

    private void Awake()
    {
        ServiceProvider.Instance.DevConsole.RegisterCommand("MapToDes_SaveMap", SaveMap);
        ServiceProvider.Instance.DevConsole.RegisterCommand("MapToDes_ToggleButtons", ToggleButtons);

        // File paths
        PathNachSave = Path.Combine(Application.persistentDataPath, NameNachSave);
        PathMapToDes = Path.Combine(PathNachSave, NameMapToDes);
        PathAutosave = Path.Combine(PathMapToDes, NameAutosave);

        // Create NACHSAVE folder (does nothing if it already exists)
        Directory.CreateDirectory(PathNachSave);

        if (Directory.Exists(PathMapToDes))
        {
            if (File.Exists(PathAutosave))
                AutosaveMap = true;
            else
                AutosaveMap = false;
        }
        else    // First run
        {
            Directory.CreateDirectory(PathMapToDes);
            File.Create(PathAutosave);
            AutosaveMap = true;
            Debug.Log("First run of MapToDesigner");
        }

        Debug.Log("Initialized MapToDesigner with AutosaveMap = " + AutosaveMap);
    }

    private void Update()
    {
        InLevelCurrent = ServiceProvider.Instance.GameState.IsInLevel;
        InDesignerCurrent = ServiceProvider.Instance.GameState.IsInDesigner;

        if (!InLevelPrevious && InLevelCurrent)
        {
            // Load designer setting for map
            LoadMtdSettings();
        }

        if (InLevelCurrent)
        {
            if (InDesignerCurrent)
            {
                if (!InDesignerPrevious)    // Entering the designer
                {
                    LoadMtdSettings();

                    ButtonsParent.gameObject.SetActive(true);

                    if (BackdropObject != null)
                    {
                        BackdropObject.SetActive(true);
                        BackdropSkyObject.SetActive(true);
                        BackdropWaterObject.SetActive(true);
                    }
                }

                foreach (Camera c in Camera.allCameras)
                {
                    c.farClipPlane = BackdropRenderDistance;
                }

                // Set map transform
                if (BackdropObject != null)
                {
                    BackdropObject.transform.localPosition = -BackdropOffset;
                    BackdropObject.transform.localEulerAngles = -BackdropAngles;

                    BackdropWaterObject.transform.position = new Vector3(0f, -BackdropOffset.y, 0f);
                }
            }
            else    // In sandbox
            {
                PausedCurrent = ServiceProvider.Instance.GameState.IsPaused;

                if (InDesignerPrevious) // Exiting the designer
                {
                    // Prevents previously copied maps from showing in the sandbox
                    if (BackdropObject != null)
                    {
                        BackdropObject.SetActive(false);
                        BackdropSkyObject.SetActive(false);
                        BackdropWaterObject.SetActive(false);
                    }

                    ButtonsParent.gameObject.SetActive(false);
                }

                if (AutosaveMap && !PausedPrevious && PausedCurrent)
                {
                    SaveMap();
                }

                PausedPrevious = PausedCurrent;
            }
        }
        
        if (InLevelPrevious && !InLevelCurrent)
        {
            // Save designer setting for map
            SaveMtdSettings();

            if (BackdropObject != null)
            {
                BackdropObject.SetActive(false);
                BackdropSkyObject.SetActive(false);
                BackdropWaterObject.SetActive(false);
            }

            ButtonsParent.gameObject.SetActive(false);
        }

        InLevelPrevious = InLevelCurrent;
        InDesignerPrevious = InDesignerCurrent;
    }

    private void LoadMtdSettings()
    {
        string infoName = BackdropMapName + ".MTD";
        string infoPath = Path.Combine(PathMapToDes, infoName);
        if (File.Exists(infoPath))
        {
            Stream stream = File.OpenRead(infoPath);
            using (BinaryReader reader = new BinaryReader(stream))
            {
                reader.ReadString();
                BackdropOffset = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                BackdropAngles = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                BackdropRenderDistance = reader.ReadSingle();
            }
        }
        else    // Use default values
        {
            BackdropOffset = Vector3.zero;
            BackdropAngles = Vector3.zero;
            BackdropRenderDistance = 50000f;
        }
    }

    private void SaveMtdSettings()
    {
        string infoName = BackdropMapName + ".MTD";
        string infoPath = Path.Combine(PathMapToDes, infoName);
        Stream stream = File.Create(infoPath);
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(MtdHeader);
            writer.Write(BackdropOffset.x);
            writer.Write(BackdropOffset.y);
            writer.Write(BackdropOffset.z);
            writer.Write(BackdropAngles.x);
            writer.Write(BackdropAngles.y);
            writer.Write(BackdropAngles.z);
            writer.Write(BackdropRenderDistance);
        }
    }

    private void SaveMap()
    {
        Map MapRoot = FindObjectOfType<Map>();
        if (MapRoot == null)
        {
            Debug.LogError("No ModTools Map component found! Can only perform SaveMap in mod maps.");
        }

        // Remove previous copied maps
        DestroyChildObjects(CopiedObjectParent);

        GameObject MapRootObject = MapRoot.gameObject;
        BackdropObject = Instantiate(MapRootObject, CopiedObjectParent);

        // Set layer to Terrain for lighting
        BackdropObject.layer = 20;
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.transform.IsChildOf(BackdropObject.transform))
            {
                obj.layer = 20;
            }
        }

        // Remove components that may cause issues
        Destroy(BackdropObject.GetComponent<Map>());
        DestroyChildUserScripts(BackdropObject);

        // Hide copied object
        BackdropObject.SetActive(false);

        // Copy TasharenWater and TOD_Sky
        Component[] components = FindObjectsOfType<Component>();
        foreach (Component c in components)
        {
            string cTypeName = c.GetType().Name;
            if (cTypeName == "TasharenWater")
            {
                BackdropWaterObject = Instantiate(c.gameObject, CopiedObjectParent);
                BackdropWaterObject.SetActive(false);
            }
            else if (cTypeName == "TOD_Sky")
            {
                BackdropSkyObject = Instantiate(c.gameObject, CopiedObjectParent);
                BackdropSkyObject.SetActive(false);
            }
        }

        // Set the name identifier for transform storage
        BackdropMapName = ServiceProvider.Instance.GameState.CurrentMapName;

        Debug.Log("Copied Map Object: " + BackdropObject.name);
    }

    private void ToggleButtons()
    {
        ButtonsParent.gameObject.SetActive(!ButtonsParent.gameObject.activeInHierarchy);
    }

    private void DestroyChildObjects(Transform transform)
    {
        if (transform.childCount == 0)
            return;

        Transform[] childTransforms = new Transform[transform.childCount];

        for (int i = 0; i < childTransforms.Length; i++)
        {
            childTransforms[i] = transform.GetChild(i);
        }

        foreach (Transform t in childTransforms)
        {
            Destroy(t.gameObject);
        }
    }

    private void DestroyChildUserScripts(GameObject parent)
    {
        Component[] childComponents = parent.GetComponentsInChildren(typeof(Component), true);
        foreach (Component c in childComponents)
        {
            string cTypeNamespace = c.GetType().Namespace;
            if (cTypeNamespace != "UnityEngine")
            {
                Destroy(c);
            }
        }
    }
}
