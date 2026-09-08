using System;
using UnityEngine;

namespace FateDice
{
    public sealed class SceneEntry : MonoBehaviour
    {
        public GameSceneRole role;
        public GameApplication applicationPrefab;

        void Start()
        {
            try
            {
                GameApplication application = GameApplication.Bootstrap(applicationPrefab);
                application.EnterScene(role);
            }
            catch (Exception error)
            {
                Debug.LogError("Scene initialization failed: " + error.Message, this);
            }
        }
    }
}
