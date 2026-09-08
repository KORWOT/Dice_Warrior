using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FateDice
{
    public sealed class SceneFlowController : MonoBehaviour, IRunSceneNavigation
    {
        public string titleScenePath = "Assets/_Project/Scenes/Title.unity";
        public string lobbyScenePath = "Assets/_Project/Scenes/Lobby.unity";
        public string inGameScenePath = "Assets/_Project/Scenes/InGame.unity";
        public bool IsTransitioning { get; private set; }
        public GameSceneRole? CurrentRole { get; private set; }

        const float EntryTimeoutSeconds = 5f;
        RunUIController controller;
        GameSceneRole pendingRole;
        bool accepted, fallbackRequested;
        string entryError;

        public void Initialize(RunUIController owner)
        {
            if (!owner) throw new ArgumentNullException(nameof(owner));
            if (controller && controller != owner)
                throw new InvalidOperationException("Scene flow already belongs to another controller.");
            controller = owner;
        }

        public void EnterScene(GameSceneRole role)
        {
            if (!controller) throw new InvalidOperationException("Initialize scene flow before entering a scene.");
            if (IsTransitioning && pendingRole != role)
            {
                entryError = $"Expected {pendingRole}, but the loaded scene entered as {role}.";
                return;
            }
            try
            {
                switch (role)
                {
                    case GameSceneRole.Title:
                        controller.EnterTitle(() => RequestLobby());
                        break;
                    case GameSceneRole.Lobby:
                        controller.EnterLobby();
                        break;
                    case GameSceneRole.InGame:
                        if (!controller.TryEnterInGame())
                        {
                            if (IsTransitioning) fallbackRequested = true;
                            else RequestLobby();
                            return;
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(role));
                }
                CurrentRole = role;
                if (IsTransitioning) accepted = true;
                else controller.SyncInputLock();
            }
            catch (Exception error)
            {
                if (IsTransitioning) entryError = "Could not enter " + role + ": " + error.Message;
                else ReportError("Could not enter " + role + ": " + error.Message);
            }
        }

        public bool RequestLobby() => Request(GameSceneRole.Lobby, lobbyScenePath);
        public bool RequestInGame() => Request(GameSceneRole.InGame, inGameScenePath);

        bool Request(GameSceneRole role, string path)
        {
            if (!controller || !isActiveAndEnabled || IsTransitioning || controller.Busy) return false;
            if (CurrentRole == role && string.Equals(SceneManager.GetActiveScene().path, path, StringComparison.Ordinal))
                return false;
            if (!Available(path))
            {
                ReportError("Scene is unavailable in the build: " + path);
                return false;
            }
            try
            {
                IsTransitioning = true;
                PrepareEntry(role);
                controller.SyncInputLock();
                AsyncOperation operation = SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
                if (operation == null) throw new InvalidOperationException("Unity did not start the scene load.");
                StartCoroutine(CompleteTransition(operation));
                return true;
            }
            catch (Exception error)
            {
                FinishTransition("Scene navigation failed: " + error.Message);
                return false;
            }
        }

        IEnumerator CompleteTransition(AsyncOperation operation)
        {
            bool usedFallback = false;
            while (true)
            {
                // Unity scene loads cannot be cancelled. Keep the lock while a real load
                // is outstanding; only waiting for its explicit SceneEntry is bounded.
                while (!operation.isDone) yield return null;
                float deadline = Time.realtimeSinceStartup + EntryTimeoutSeconds;
                while (!accepted && !fallbackRequested && entryError == null && Time.realtimeSinceStartup < deadline)
                    yield return null;
                if (entryError != null)
                {
                    FinishTransition(entryError);
                    yield break;
                }
                if (fallbackRequested && !usedFallback)
                {
                    usedFallback = true;
                    string failure = null;
                    try
                    {
                        if (!Available(lobbyScenePath))
                            throw new InvalidOperationException("The Lobby scene is unavailable in the build.");
                        PrepareEntry(GameSceneRole.Lobby);
                        operation = SceneManager.LoadSceneAsync(lobbyScenePath, LoadSceneMode.Single);
                        if (operation == null) throw new InvalidOperationException("Unity did not start the Lobby load.");
                    }
                    catch (Exception error) { failure = "Could not return to Lobby: " + error.Message; }
                    if (failure != null)
                    {
                        FinishTransition(failure);
                        yield break;
                    }
                    continue;
                }
                FinishTransition(accepted ? null : "The loaded scene did not accept its entry role.");
                yield break;
            }
        }

        void PrepareEntry(GameSceneRole role)
        {
            pendingRole = role;
            accepted = false;
            fallbackRequested = false;
            entryError = null;
        }

        static bool Available(string path) =>
            !string.IsNullOrWhiteSpace(path) && Application.CanStreamedLevelBeLoaded(path);

        void FinishTransition(string error)
        {
            IsTransitioning = false;
            accepted = false;
            fallbackRequested = false;
            entryError = null;
            try { if (controller) controller.SyncInputLock(); }
            catch (Exception syncError) { error = (error == null ? "" : error + " ") + syncError.Message; }
            if (error != null) ReportError(error);
        }

        void ReportError(string error)
        {
            if (!controller) return;
            try { controller.ReportNavigationError(error); }
            catch (Exception displayError) { Debug.LogException(displayError, this); }
        }

        void OnDestroy()
        {
            StopAllCoroutines();
            IsTransitioning = false;
        }
    }
}
