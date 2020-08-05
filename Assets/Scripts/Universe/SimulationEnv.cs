using UnityEngine;
using System;

public enum SimSettingCategory { UI, NBodyEngine, Physics };

[CreateAssetMenu(), Serializable]
public class SimulationEnv : ScriptableObject
{
    public static string[] simSettingCategoryLabels = new string[] {"UI", "NBody Simulator Engine", "Physics"};
    //=============================================
    //=============================================
    //=============================================
    // Class containing every variables needed to define the simulation environment
    public SimSettingBool simulateGravity = new SimSettingBool(true, true, "Simulate gravity", SimSettingCategory.NBodyEngine, SimSettings_InfoList.simulateGravity);
    // Number of celestialBodies to compute the gravitational acc for each celestialBody
    public SimSettingInt NBODYSIM_NB_BODY = new SimSettingInt(3, 3, "Number of body to consider for the N-Body sim computations", SimSettingCategory.NBodyEngine, 1, 10);
    //=============================================
    //=============================================
    //=============================================
    public SimSettingBool useTargetFrameRate = new SimSettingBool(false, false, "Enable FPS targetting", SimSettingCategory.Physics);
    public SimSettingInt targetFrameRate = new SimSettingInt(60, 60, "Target FPS value", SimSettingCategory.Physics, 1, 100);
    public SimSettingInt physicsUpdateRate = new SimSettingInt(50, 50, "Physics update frequency", SimSettingCategory.Physics, 1, 100);
    public SimSettingFloat timeScale = new SimSettingFloat(1f, 1f, "Time scale", SimSettingCategory.Physics, 0.1f, 10f, SimSettings_InfoList.simulateGravity);
    //=============================================
    //=============================================
    //=============================================
    //public UniverseClock masterClock;
    //=============================================
    //=============================================
    //=============================================
    //================================================
    public static bool ObjectIsSimSetting(object obj)
    {
        if(obj is SimSettingBool || obj is SimSettingInt || obj is SimSettingFloat) { return true; }
        else { return false; }
    }
}

[System.Serializable]
public class SimSettingGeneric<T1> : SimSettingInterface<T1>
{
    [SerializeField]
    private T1 _value;
    public T1 value
    {
        get {
            return _value;
        }
        set {
            _value=value;
        }
    }

    [SerializeField]
    private T1 _default_value;
    public T1 default_value
    {
        get {
            return _default_value;
        }
        set {
            _default_value=value;
        }
    }

    [SerializeField]
    private string _type;
    public string type
    {
        get {
            return _type;
        }
        set {
            _type=value;
        }
    }

    [SerializeField]
    private string _displayName;
    public string displayName
    {
        get {
            return _displayName;
        }
        set {
            _displayName=value;
        }
    }

    [SerializeField]
    private SimSettingCategory _category;
    public SimSettingCategory category
    {
        get {
            return _category;
        }
        set {
            _category=value;
        }
    }

    [SerializeField]
    private SimSettings_Info _simSettings_Info;
    public SimSettings_Info simSettings_Info
    {
        get {
            return _simSettings_Info;
        }
        set {
            _simSettings_Info=value;
        }
    }

    public SimSettingGeneric(T1 variableValue, T1 default_value, string nameToDisplay, SimSettingCategory settingCategory,
                             SimSettings_Info _simSettings_Info=null)
    {
        this.displayName = nameToDisplay;
        this.value = variableValue;
        this.category = settingCategory;
        this.type = variableValue.GetType().ToString();
        this.simSettings_Info = _simSettings_Info;
    }
}

[Serializable]
public class SimSettingInt : SimSettingGeneric<int>, SimSettingInterface<int>
{
    public int minValue;
    public int maxValue;
    public SimSettingInt(int variableValue, int defaultValue, string nameToDisplay, SimSettingCategory settingCategory,
                         int minVal, int maxVal, SimSettings_Info _simSettings_Info=null) 
                        : base(variableValue, defaultValue, nameToDisplay, settingCategory, _simSettings_Info)
    {
        this.minValue = minVal;
        this.maxValue = maxVal;
    }
}

[Serializable]
public class SimSettingFloat : SimSettingGeneric<float>, SimSettingInterface<float>
{
    public float minValue;
    public float maxValue;
    public SimSettingFloat(float variableValue, float defaultValue, string nameToDisplay, SimSettingCategory settingCategory,
                           float minVal, float maxVal, SimSettings_Info _simSettings_Info=null) 
                           : base(variableValue, defaultValue, nameToDisplay, settingCategory, _simSettings_Info)
    {
        this.minValue = minVal;
        this.maxValue = maxVal;
    }
}

[Serializable]
public class SimSettingBool : SimSettingGeneric<bool>, SimSettingInterface<bool>
{
    public SimSettingBool(bool variableValue, bool defaultValue, string nameToDisplay, SimSettingCategory settingCategory,
                          SimSettings_Info _simSettings_Info=null)
                          : base(variableValue, defaultValue, nameToDisplay, settingCategory, _simSettings_Info) { }
}