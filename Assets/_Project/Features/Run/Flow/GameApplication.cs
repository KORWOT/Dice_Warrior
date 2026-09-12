using System;
using System.IO;
using UnityEngine;

namespace FateDice
{
    public sealed class GameApplication : MonoBehaviour
    {
        public RunUIController controller;
        public SceneFlowController sceneFlow;
        public MetaProgressionConfig metaConfig;
        static GameApplication current;
        public static GameApplication Current => current ? current : null;

        public static GameApplication Bootstrap(GameApplication prefab, IRunStore store = null, ISeedSource seedSource = null,
            IMetaProgressionService metaService = null)
        {
            if (current)
            {
                if (store != null && !ReferenceEquals(store, current.controller.Store))
                    throw new InvalidOperationException("The running application already owns a different save store.");
                if (seedSource != null && !ReferenceEquals(seedSource, current.controller.SeedSource))
                    throw new InvalidOperationException("The running application already owns a different seed source.");
                if (metaService != null && !ReferenceEquals(metaService, current.controller.Meta))
                    throw new InvalidOperationException("The running application already owns a different meta service.");
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
                if (store == null && metaService == null)
                {
                    if (!instance.metaConfig) throw new InvalidOperationException("GameApplication requires its authored meta progression config.");
                    const string profileId = "local-development";
                    metaService = new LocalMetaProgressionService(new LocalPlayerDataStore(LocalPlayerDataStore.PlayerPath(
                        Path.Combine(Application.persistentDataPath, "FateDiceMeta"), profileId), profileId),
                        () => instance.controller.config.Snapshot(), instance.metaConfig, seedSource,
                        new LocalRunStore(Path.Combine(Application.persistentDataPath, "FateDiceLocal", "run.json")));
                }
                if (store != null && metaService != null && !ReferenceEquals(store, metaService.RunStore))
                    throw new InvalidOperationException("Meta progression and run checkpoints must belong to the same player store.");
                // Inject all dependencies before activating any controller, UI or EventSystem.
                instance.sceneFlow.Initialize(instance.controller);
                instance.controller.Initialize(store ?? metaService.RunStore, instance.sceneFlow, seedSource, metaService);
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
