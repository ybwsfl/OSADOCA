﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mathd_Lib;

public interface FlyingObjCommonParams
{
    // Either 'Spaceship' or 'CelestialBody'
    // Interface for the shared variables between 'Spaceship' and 'CelestialBody' objects

    //CelestialBody orbitedBody {get; set;} MOVED IN THE ORBITAL PARAMS FILE
    Orbit orbit {get; set;}
    OrbitalPredictor predictor {get; set;}
    OrbitalParams orbitalParams {get; set;}

    Vector3d orbitedBodyRelativeAcc {get; set;}
    Vector3d orbitedBodyRelativeVelIncr {get; set;}
    Vector3d orbitedBodyRelativeVel {get; set;}
    Vector3d realPosition {get; set;}

    Vector3d GetRelativeRealWorldPosition();
    Vector3d GetRelativeVelocity();
    double GetRelativeVelocityMagnitude();

    GameObject _gameObject {get; set;}
}

public interface FlyingObjSettings
{
    // Either 'SpaceshipSettings' or 'CelestialBodySettings'
}