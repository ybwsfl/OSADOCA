using System;
using System.Collections;
using UnityEngine;
using Mathd_Lib;
using System.Collections.Generic;

public class OrbitalPredictor
{
    FlyingObjCommonParams bodyToPredict;
    //Spaceship spaceship;
    CelestialBody orbitedBody;
    Orbit linkedOrbit; // initial orbit that the predictor will use to predict its evolution 
    public Orbit predictedOrbit; // orbit that the predictor computed

    public OrbitalPredictor(FlyingObjCommonParams predictedBody, CelestialBody body, Orbit orbit)
    {
        bodyToPredict = predictedBody;
        orbitedBody = body;
        linkedOrbit = orbit;
        smartPredictor();
    }

    [System.Obsolete("Obselete function as it is not optimized. Will considerably slow down the game engine")]
    public void RawPredictor()
    {
        if(bodyToPredict == null || orbitedBody == null) return;

        double speedMagn = bodyToPredict.orbitedBodyRelativeVel.magnitude;
        double circularSpeed = linkedOrbit.GetCircularOrbitalSpeed();
        double liberationSpeed = Mathd.Sqrt(2d) * circularSpeed;
        // Calculate orbital speed and compare it to the one needed to reach a circular orbit
        if(speedMagn < circularSpeed)
        {
            // Orbit is decaying
            int nbMaxIterations = 150;
            int nbCurIter = 0;
            double dstP2Surf = bodyToPredict.orbit.GetAltitude();
            Vector3d tmpVelocityIncr = Vector3d.zero;
            Vector3d tmpVelocity = bodyToPredict.orbitedBodyRelativeVel;
            Vector3d acc = Vector3d.zero;
            List<Vector3> pos = new List<Vector3>();
            double timestep = 2d; // second
            Vector3d lastPos = new Vector3d(bodyToPredict._gameObject.transform.position);// + universeRunner.universeOffset;
            while((nbCurIter < nbMaxIterations) && (dstP2Surf > 1d))  
            {
                // While predicted point did not reach Earth'surface
                Vector3d dir = new Vector3d(orbitedBody.transform.position) - lastPos;
                Vector3d accDirection = dir.normalized;
                double dstSquared = (dir*UniCsts.u2pl).sqrMagnitude / Mathd.Pow(10,7);
                acc =  accDirection * orbitedBody.settings.planetBaseParamsDict[CelestialBodyParamsBase.planetaryParams.mu.ToString()] / dstSquared; // m.s-2

                tmpVelocityIncr = timestep * acc;
                tmpVelocity += tmpVelocityIncr;

                lastPos += timestep * (tmpVelocity * UniCsts.m2km * UniCsts.pl2u);
                pos.Add((Vector3)lastPos);

                dstP2Surf = bodyToPredict.orbit.GetAltitude(lastPos);
                nbCurIter += 1;
            }

            Vector3[] posLR = pos.ToArray();
            GameObject dirGO = UsefulFunctions.CreateAssignGameObject("RawOrbitPredictor" + linkedOrbit.suffixGO);
            dirGO.layer = 10; // 'Orbit' layer
            LineRenderer dirLR = (LineRenderer) UsefulFunctions.CreateAssignComponent(typeof(LineRenderer), dirGO);
            dirGO.transform.parent = GameObject.Find(linkedOrbit.suffixGO).transform;

            dirLR.useWorldSpace = true;
            dirLR.positionCount = posLR.Length;
            dirLR.SetPositions(posLR);
            dirLR.widthCurve = AnimationCurve.Constant(0f, 1f, 100f);
            dirLR.sharedMaterial = Resources.Load("OrbitMaterial", typeof(Material)) as Material;
        }
        else if(speedMagn < liberationSpeed)
        {
            // Orbit will remain elliptic
            Debug.Log("Orbit is to remain elliptic. Function not imlemented");
        }
        else { 
            // Orbit is hyperbolic
            Debug.Log("Orbit is hyperbolic. Function not imlemented");
        }
    }

    public void smartPredictor()
    {
        if(bodyToPredict == null || orbitedBody == null) { return; }

        double speedMagn = bodyToPredict.GetRelativeVelocityMagnitude();
        double circularSpeed = linkedOrbit.GetCircularOrbitalSpeed();
        double liberationSpeed = Mathd.Sqrt(2d) * circularSpeed;
        // Calculate orbital speed and compare it to the one needed to reach a circular orbit

        double µ = orbitedBody.settings.planetBaseParamsDict[CelestialBodyParamsBase.planetaryParams.mu.ToString()];
        Vector3d rVec = bodyToPredict.GetRelativeRealWorldPosition()*UniCsts.km2m;
        double r = rVec.magnitude;
        Vector3d velocityVec = bodyToPredict.GetRelativeVelocity();

        // Computing semi-major axis length
        double a = r * µ*Mathd.Pow(10,13) / (2*µ*Mathd.Pow(10,13) - r*Mathd.Pow(speedMagn,2)) * UniCsts.m2km;
        // Computing specific angular momentum vector, in m2.s-1 (perpendicular to the orbit plane)
        Vector3d h = Vector3d.Cross(rVec, velocityVec);
        // Computing eccentricity vector, pointing from the apoapsis to the periapsis
        Vector3d eVec = Vector3d.Cross(velocityVec, h) / (µ*UniCsts.µExponent) - rVec/r;

        double e = eVec.magnitude;
        double p = a*(1-e*e);
        
        OrbitalParams predOrbitParams = OrbitalParams.CreateInstance<OrbitalParams>();
        predOrbitParams.orbitRealPredType = OrbitalParams.typeOfOrbit.predictedOrbit;
        predOrbitParams.orbitDefType = OrbitalParams.orbitDefinitionType.pe;

        switch(UsefulFunctions.CastStringToGoTags(bodyToPredict._gameObject.tag))
        {
            case UniverseRunner.goTags.Spaceship:
                predOrbitParams.orbParamsUnits = OrbitalParams.orbitalParamsUnits.km_degree;
                break;
            
            case UniverseRunner.goTags.Planet:
                predOrbitParams.orbParamsUnits = OrbitalParams.orbitalParamsUnits.AU_degree;
                break;
        }

        predOrbitParams.p = p;
        predOrbitParams.e = e;
        predOrbitParams.i = linkedOrbit.param.i;
        predOrbitParams.lAscN = linkedOrbit.param.lAscN;
        predOrbitParams.omega = linkedOrbit.param.omega;
        predOrbitParams.nu = linkedOrbit.param.nu;
        predOrbitParams.bodyPosType = OrbitalParams.bodyPositionType.nu;
        predOrbitParams.orbitDrawingResolution = 300;
        predOrbitParams.drawOrbit = true;
        predOrbitParams.drawDirections = true;
        predOrbitParams.selectedVectorsDir = (OrbitalParams.typeOfVectorDir)254;

        predictedOrbit = new Orbit(predOrbitParams, orbitedBody, bodyToPredict._gameObject);

        if(predOrbitParams.drawDirections)
        {
            foreach(OrbitalParams.typeOfVectorDir vectorDir in Enum.GetValues(typeof(OrbitalParams.typeOfVectorDir)))
            {
                if (predOrbitParams.selectedVectorsDir.HasFlag(vectorDir))
                {
                    if(vectorDir.Equals(OrbitalParams.typeOfVectorDir.radialVec) || vectorDir.Equals(OrbitalParams.typeOfVectorDir.tangentialVec))
                    {
                        predictedOrbit.DrawDirection(vectorDir, 0.1f, 50f, bodyToPredict._gameObject.transform.position);
                    }
                    else{
                        predictedOrbit.DrawDirection(vectorDir, 0.1f, 50f);
                    }
                }
            }
        }
    }   
}