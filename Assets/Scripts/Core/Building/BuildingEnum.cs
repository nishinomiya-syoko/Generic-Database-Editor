using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Top
{
    
    public interface IMoveable
    {
        void MoveTo(Vector3 destination);

        Vector3 GetCurrentPosition();
        Vector2 GetCurrentGridPosition();
    }
}