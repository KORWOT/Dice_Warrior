using System;
using System.IO;
using UnityEngine;

namespace FateDice
{
    public sealed class GameApplication : MonoBehaviour
    {
        public RunUIController controller;
        public SceneFlowController sceneFlow;
        static GameApplication current;
        public static GameApplication Current => current ? current : null;

        public static GameApplication Bootstrap(GameApplication prefab, IRunStore store = null, ISeedSource seedSource = null)
        {
            if (current)
            {
                if (store != null && !ReferenceEquals(store, current.controller.Store))
                    throw new InvalidOperationException("The running application already owns a different save store.");
                if (seedSource != null && !ReferenceEquals(seedSource, current.controller.SeedSource))
                    throw new InvalidOperationException("The running application already owns a different seed source.");
                return current;
            }
            if (!prefab) throw new ArgumentNullException(nameof(prefab));
            if (prefab.gameObject.activeSelf)
                throw new InvalidOperationException("The GameApplication prefab must be authored inactive.");
            if (!prefab.controller || !prefab.sceneFlow ||
                prefab.controller.gameObject != prefab.gameObject || prefab.sceneFlow.gameObject != prefab.gameObject)
                throw new InvalidOperationException("GameApplication requires its controller and scene flow on its own root.");

            GameApplication instance = null;
            try
            {
                instance = Instantiate(prefab);
                instance.controller.initializeOnAwake = false;
                // Inject all dependencies before activating any controller, UI or EventSystem.
                instance.sceneFlow.Initialize(instance.controller);
                instance.controller.Initialize(store ?? new LocalRunStore(Path.Combine(
                    Application.persistentDataPath, "FateDiceLocal", "run.json")), instance.sceneFlow, seedSource);
                DontDestroyOnLoad(instance.gameObject);
                current = instance;
                instance.gameObject.SetActive(true);
                return instance;
            }
            catch
            {
                if (current == instance) current = null;
                if (instance)
                {
                    instance.gameObject.SetActive(false);
                    Destroy(instance.gameObject);
                }
                throw;
            }
        }

        public void EnterScene(GameSceneRole role) => sceneFlow.EnterScene(role);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetLifetime()
        {
            // Static references survive when domain reload is disabled. This only cleans up;
            // production SceneEntry remains the sole place that requests bootstrap.
            GameApplication previous = current;
            current = null;
            if (!previous) return;
            previous.gameObject.SetActive(false);
            Destroy(previous.gameObject);
        }

        void OnDestroy()
        {
            if (current == this) current = null;
        }
    }
}
