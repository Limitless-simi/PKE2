﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class Settings : MonoBehaviour
{
    private float startTime = 0;
    private void Start()
    {
        startTime = Time.time;
    }

    private void Update()
    {
        // to make sure that settings are already set by other scripts used by oculus plugin.
      if ( Time.time > startTime + 3f) return;

       if (QualitySettings.antiAliasing != 8 || XRSettings.eyeTextureResolutionScale != 1.5f)
       {
           QualitySettings.antiAliasing = 8;
           XRDevice.UpdateEyeTextureMSAASetting();
           XRSettings.eyeTextureResolutionScale = 1.5f;
       }
    }
}
