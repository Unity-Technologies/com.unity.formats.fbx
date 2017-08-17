// ***********************************************************************
// Copyright (c) 2017 Unity Technologies. All rights reserved.
//
// Licensed under the ##LICENSENAME##.
// See LICENSE.md file in the project root for full license information.
// ***********************************************************************

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FbxExporters.Review
{
    public class RotateModel : MonoBehaviour
    {

        [Tooltip ("Rotation speed in degrees/second")]
        [SerializeField]
        private float speed = 10f;

        public float GetSpeed()
        {
            return speed;
        }

        void Update ()
        {
            transform.Rotate (Vector3.up, speed * Time.deltaTime, Space.World);	
        }
    }
}