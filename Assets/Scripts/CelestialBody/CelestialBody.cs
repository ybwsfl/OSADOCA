using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Mathd_Lib;

public class CelestialBody: MonoBehaviour, FlyingObjCommonParams
{
    public CelestialBodySettings settings;
    //=========================================
    public GameObject _gameObject { get{return this.gameObject;} set{_gameObject=this.gameObject;} }

    [HideInInspector, SerializeField]
    private Orbit _orbit;
    public Orbit orbit
    {
        get {
            return _orbit;
        }
        set {
            _orbit=value;
        }
    }

    [HideInInspector, SerializeField]
    private OrbitalPredictor _predictor;
    public OrbitalPredictor predictor
    {
        get {
            return _predictor;
        }
        set {
            _predictor=value;
        }
    }

    public OrbitalParams _orbitalParams;
    public OrbitalParams orbitalParams
    {
        get {
            return _orbitalParams;
        }
        set {
            _orbitalParams=value;
        }
    }

    [HideInInspector]
    public Vector3d _realPosition;
    public Vector3d realPosition
    {
        get {
            return _realPosition;
        }
        set {
            _realPosition=value;
        }
    }

    [HideInInspector]
    public Vector3d _orbitedBodyRelativeAcc;
    public Vector3d orbitedBodyRelativeAcc
    {
        get {
            return _orbitedBodyRelativeAcc;
        }
        set {
            _orbitedBodyRelativeAcc=value;
        }
    }

    [HideInInspector]
    public Vector3d _orbitedBodyRelativeVelIncr;
    public Vector3d orbitedBodyRelativeVelIncr
    {
        get {
            return _orbitedBodyRelativeVelIncr;
        }
        set {
            _orbitedBodyRelativeVelIncr=value;
        }
    }

    [HideInInspector]
    public Vector3d _orbitedBodyRelativeVel;
    public Vector3d orbitedBodyRelativeVel
    {
        get {
            return _orbitedBodyRelativeVel;
        }
        set {
            _orbitedBodyRelativeVel=value;
        }
    }
    //=========================================
    public bool spawnAsSimpleSphere = false; // For the UI, will spawn a simple sphere (no LOD, culling etc.). For the simlation, will spawn the complex one
    //=========================================
    // Bools for editor
    [HideInInspector] public bool showCelestialBodyInfoPanel;
    [HideInInspector] public bool planetEditorFoldout;
    
    // Active Camera and distance
    private Transform universePlayerCamera;
    [HideInInspector] public float distanceToPlayer;
    [HideInInspector] public float distanceToPlayerPow2;
    
    // Related to CelestialBody Generation
    MeshFilter[] meshFilters;
    TerrainFace[] terrainFaces;

    void Awake()
    {
        if(GameObject.Find("UniverseRunner") == null)
        {
            // Can not find any UniverseRunner in the scene, thus it is a celetialBody in a UI menu
            spawnAsSimpleSphere = true; // Enforcing simple sphere rendering as no universePlayerCamera will be found
        }

        if(orbitalParams.orbitedBodyName.Equals("None")) {
            orbitalParams.orbitedBody = null;
        }
        else {
            string suffix = spawnAsSimpleSphere ? "Planet_UI" : "";
            orbitalParams.orbitedBody = GameObject.Find(orbitalParams.orbitedBodyName+suffix).GetComponent<CelestialBody>();
        }
    }

    public void AwakeCelestialBody(Dictionary<string,double> refDictOrbParams)
    {
        AssignRefDictOrbitalParams(refDictOrbParams);
        if(spawnAsSimpleSphere) { 
            gameObject.GetComponent<MeshRenderer>().material = settings.bodyMaterial;
            InitializeBodyParameters();
            return; // Early exit if it's a UI celestialBody
        }

        // Get playerCamera defined in the UniverseRunner Instance
        UniverseRunner verse = GameObject.Find("UniverseRunner").GetComponent<UniverseRunner>();
        universePlayerCamera = verse.playerCamera.transform;
        
        InitializeBodyParameters();

        if (gameObject.GetComponent<Presets>() == null)
        {
            Presets presetScript = gameObject.AddComponent<Presets>();
        }

        // InitializeOrbitalParams cannot be here as the CreateComponent function will not be called at this point. Must be called after the Awake() and Start()
        GetDistancesToCamera();
        GeneratePlanet();
        ApplyFlatenningScale();
        CreateAssignSunPointLight();
        StartCoroutine(PlanetGenerationLoop());
    }

    private void GetDistancesToCamera()
    {
        distanceToPlayer = Vector3.Distance(transform.position, universePlayerCamera.position);
        distanceToPlayerPow2 = distanceToPlayer * distanceToPlayer;
    }

    private void ApplyFlatenningScale()
    {
        // Apply oblateness to the planet
        float flatenningVal = (float)settings.planetBaseParamsDict[CelestialBodyParamsBase.planetaryParams.inverseFlattening.ToString()];
        float flatenningScale = 1f;
        if(!UsefulFunctions.FloatsAreEqual(flatenningVal, 0f))
        {
            flatenningScale = 1f - 1f/flatenningVal;
        }
        Vector3 bodyScale = new Vector3(1f, flatenningScale, 1f);
        gameObject.transform.localScale = bodyScale;
    }

    private void InitializeBodyParameters()
    {
        // RadiusU
        settings.radiusU = settings.planetBaseParamsDict[CelestialBodyParamsBase.planetaryParams.radius.ToString()] * UniCsts.pl2u; // km
        
        // Rotation Speed
        double siderealPeriod = settings.planetBaseParamsDict[CelestialBodyParamsBase.planetaryParams.siderealRotPeriod.ToString()];
        if(!UsefulFunctions.DoublesAreEqual(siderealPeriod, 0d))
        {
            settings.rotationSpeed = (double)Time.fixedDeltaTime * 360d / siderealPeriod; // in °.s-1

        }
        else {
            settings.rotationSpeed = 0d;
        }

        // Initializing equatorial plane, making sure it is back at its default values
        settings.equatorialPlane.forwardVec = new Vector3d(1d, 0d, 0d);
        settings.equatorialPlane.rightVec = new Vector3d(0d, 0d, 1d);
        settings.equatorialPlane.normal = new Vector3d(0d, 1d, 0d);
        settings.equatorialPlane.point = new Vector3d(0d, 0d, 0d);
    }

    public void GeneratePlanet()
    {
        InitializePlanet();
        GenerateMesh();
    }

    private void InitializePlanet()
    {   
        if(meshFilters == null || meshFilters.Length == 0)
        {
            meshFilters = new MeshFilter[6];
        }

        Dictionary<Vector3, string> directionsDict = new Dictionary<Vector3, string>()
        {
            {Vector3.up, "up"},
            {Vector3.down, "down"},
            {Vector3.left, "left"},
            {Vector3.right, "right"},
            {Vector3.forward, "forward"},
            {Vector3.back, "back"}
        };

        terrainFaces = new TerrainFace[6];
        if(settings.heightMap == null) {
            // Set the variable to the default (all black) Texture2D
            settings.heightMap = (Texture2D)Resources.Load("CelestialBodies/TextureFiles/Default/default_heightMap");
        }
        if(!settings.planetBaseParamsDict.ContainsKey(CelestialBodyParamsBase.biomeParams.highestBumpAlt.ToString()))
        {
            // Set variable to a default key
            settings.planetBaseParamsDict.Add(CelestialBodyParamsBase.biomeParams.highestBumpAlt.ToString(), 0f);
        }
        settings.heightMap = UsefulFunctions.RotateTexture180(settings.heightMap);

        for(int i=0; i<6; i++)
        {
            if(meshFilters[i] == null)
            {
                GameObject meshObj = new GameObject(directionsDict[directionsDict.Keys.ElementAt(i)]);
                meshObj.transform.parent = transform;

                meshObj.AddComponent<MeshRenderer>().sharedMaterial = settings.bodyMaterial;
                InitMeshColliders(meshObj);
                meshFilters[i] = meshObj.AddComponent<MeshFilter>();
                meshFilters[i].sharedMesh = new Mesh();
                meshFilters[i].sharedMesh.name = directionsDict[directionsDict.Keys.ElementAt(i)].ToString() + "_mesh";
                meshObj.layer = 9;
            }
            terrainFaces[i] = new TerrainFace(meshFilters[i].sharedMesh, directionsDict.Keys.ElementAt(i), this, settings.heightMap, universePlayerCamera);
        }
    }

    private void GenerateMesh()
    {
        int idx = 0;
        foreach(TerrainFace face in terrainFaces)
        {
            face.ConstructTree();
            MeshCollider mCollider = meshFilters[idx].gameObject.GetComponent<MeshCollider>();
            mCollider.sharedMesh = face.mesh;
            idx += 1;
        }
    }
    //=========================================
    public void InitializeAxialTilt()
    {
        float axialTitleAngle = (float)settings.planetBaseParamsDict[CelestialBodyParamsBase.planetaryParams.axialTilt.ToString()];
        // Rotation vector is in the orbital plane and perpendicular to the radial vector
        Vector3 tangentialVec = (Vector3)orbit.ComputeDirectionVector(OrbitalTypes.typeOfVectorDir.tangentialVec);

        Quaternion tiltRotation = Quaternion.AngleAxis(axialTitleAngle, -tangentialVec);
        gameObject.transform.rotation *= tiltRotation;
    }

    private IEnumerator PlanetGenerationLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(settings.planetRefreshPeriod);
            UpdatePlanet();
        }
    }

    public void UpdatePlanet()
    {
        foreach (TerrainFace face in terrainFaces)
        {
            face.UpdateTree();
        }
    }
    //=========================================
    void FixedUpdate()
    {
        if(!spawnAsSimpleSphere)
        {
            distanceToPlayer = Vector3.Distance(transform.position, universePlayerCamera.position);
            distanceToPlayerPow2 = distanceToPlayer * distanceToPlayer;
            if(!UsefulFunctions.DoublesAreEqual(settings.rotationSpeed, 0d))
            {
                RotatePlanet();
            }
        }
        else {
            
        }
    }

    private void RotatePlanet()
    {
        Vector3 positivePoleVec = (Vector3)settings.equatorialPlane.normal;        
        gameObject.transform.rotation = Quaternion.AngleAxis((float)-settings.rotationSpeed, positivePoleVec) * transform.rotation;  // Sign "-" for the prograde rotation
    }
    //=========================================
    private void AssignRefDictOrbitalParams(Dictionary<string,double> refDictOrbParams)
    {
        settings.planetBaseParamsDict = refDictOrbParams;
        if(settings.usePredifinedPlanets)
        {
            // Copying orbital values from the planet dictionary to the orbitalParams scriptable object
            InitializeOrbitalParameters();
        }
        // if 'usePredefinedPlanet' is false, then the orbital values are directly user input in the scriptable object 'orbitalParams' file
    }

    private void InitializeOrbitalParameters()
    {
        // Called only if 'usePredefinedPlanet' is true
        // Copying orbital values from the planet dictionary to the orbitalParams scriptable object
        orbitalParams.orbitRealPredType = OrbitalTypes.typeOfOrbit.realOrbit;
        orbitalParams.orbitDefType = OrbitalTypes.orbitDefinitionType.rarp;
        orbitalParams.orbParamsUnits = OrbitalTypes.orbitalParamsUnits.AU_degree;
        orbitalParams.bodyPosType = OrbitalTypes.bodyPositionType.nu;

        orbitalParams.ra = settings.planetBaseParamsDict[CelestialBodyParamsBase.orbitalParams.aphelion.ToString()];
        orbitalParams.rp = settings.planetBaseParamsDict[CelestialBodyParamsBase.orbitalParams.perihelion.ToString()];
        orbitalParams.i = settings.planetBaseParamsDict[CelestialBodyParamsBase.orbitalParams.i.ToString()];
        orbitalParams.lAscN = settings.planetBaseParamsDict[CelestialBodyParamsBase.orbitalParams.longAscendingNode.ToString()];
        orbitalParams.omega = settings.planetBaseParamsDict[CelestialBodyParamsBase.orbitalParams.perihelionArg.ToString()];
        orbitalParams.nu = settings.planetBaseParamsDict[CelestialBodyParamsBase.orbitalParams.trueAnomaly.ToString()];
    }

    public void InitializeOrbitalPredictor()
    {
        predictor = new OrbitalPredictor(this, orbitalParams.orbitedBody.GetComponent<CelestialBody>(), orbit);
    }


    public void CreateAssignSunPointLight()
    {
        if(UsefulFunctions.CastStringToGoTags(gameObject.tag).Equals(UniverseRunner.goTags.Star))
        {
            GameObject sunPointLightGO = UsefulFunctions.CreateAssignGameObject("sunPointLight", typeof(Light));
            UsefulFunctions.parentObj(sunPointLightGO, gameObject);
            Light sunPointLight = sunPointLightGO.GetComponent<Light>();
            sunPointLight.type = LightType.Point;
            sunPointLight.range = Mathf.Pow(10, 5);
            sunPointLight.color = Color.white;
            sunPointLight.shadows = LightShadows.Soft;
            sunPointLight.cullingMask |= 1 << LayerMask.NameToLayer("Everything");
            sunPointLight.cullingMask &=  ~(1 << LayerMask.NameToLayer("Orbit"));
            sunPointLight.lightmapBakeType = LightmapBakeType.Baked;
            sunPointLight.intensity = 1f;
        }
    }


    public static bool CelestialBodyHasTagName(CelestialBody body, string tagName)
    {
        if(body.tag == tagName) { return true; }
        else{ return false; }
    }

    public Vector3d GetRelativeRealWorldPosition()
    {
        return new Vector3d(transform.position - orbitalParams.orbitedBody.transform.position) * UniCsts.u2pl;
    }

    public Vector3d GetRelativeVelocity()
    {
        return orbitedBodyRelativeVel;
    }

    public double GetRelativeVelocityMagnitude()
    {
        return orbitedBodyRelativeVel.magnitude;
    }

    public Vector3d GetWorldPositionFromGroundStart()
    {
        Debug.Log(gameObject.name + " - " + settings.radiusU);
        return -settings.radiusU * Vector3d.forward;
    }

    public void InitMeshColliders(GameObject faceGO)
    {
        MeshCollider meshCollider = (MeshCollider)UsefulFunctions.CreateAssignComponent(typeof(MeshCollider), faceGO);
        meshCollider.convex = true;

        PhysicMaterial sp_c_Material = new PhysicMaterial();
        sp_c_Material.bounciness = 0f;
        sp_c_Material.dynamicFriction = 1f;
        sp_c_Material.staticFriction = 1f;
        sp_c_Material.frictionCombine = PhysicMaterialCombine.Average;
        sp_c_Material.bounceCombine = PhysicMaterialCombine.Average;
        meshCollider.material = sp_c_Material;
    }

}