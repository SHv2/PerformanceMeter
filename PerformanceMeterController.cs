﻿/*
 * PerformanceMeterController.cs
 * PerformanceMeter
 *
 * This file defines the main functionality of PerformanceMeter.
 *
 * This code is licensed under the MIT license.
 * Copyright (c) 2021 JackMacWindows.
 */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PerformanceMeter {
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
    public class PerformanceMeterController : MonoBehaviour {
        public static PerformanceMeterController instance { get; private set; }
        List<float> energyList = new List<float>();
        float averageHitValue = 0.0f;
        int averageHitValueSize = 0;
        ScoreController scoreController;
        GameEnergyCounter energyCounter;
        RelativeScoreAndImmediateRankCounter rankCounter;
        ResultsViewController resultsController;
        GameObject panel;
        ILevelEndActions endActions;
        bool levelOk = false;

        public void ShowResults() {
            if (!levelOk) return;
            levelOk = false;
            Logger.log.Debug("Found " + energyList.Count() + " notes");

            panel = new GameObject("PerformanceMeter");
            panel.transform.Rotate(22.5f, 0, 0, Space.World);
            Canvas canvas = panel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.scaleFactor = 0.01f;
            canvas.GetComponent<RectTransform>().localPosition = new Vector3(0.0f, 0.4f, 2.25f);
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1.0f, 0.5f);
            canvas.transform.Rotate(22.5f, 0, 0, Space.World);
            panel.AddComponent<CanvasRenderer>();
            panel.AddComponent<HMUI.CurvedCanvasSettings>();

            GameObject imageObj = new GameObject("Background");
            imageObj.transform.SetParent(canvas.transform);
            imageObj.transform.Rotate(22.5f, 0, 0, Space.World);
            Image img = imageObj.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.25f);
            img.GetComponent<RectTransform>().localPosition = new Vector3(0.0f, 0.0f, 0.0f);
            img.GetComponent<RectTransform>().sizeDelta = new Vector2(1.0f, 0.6f);

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(canvas.transform);
            textObj.transform.Rotate(22.5f, 0, 0, Space.World);
            HMUI.CurvedTextMeshPro text = textObj.AddComponent<HMUI.CurvedTextMeshPro>();
            text.font = Instantiate(Resources.FindObjectsOfTypeAll<TMP_FontAsset>().First(t => t.name == "Teko-Medium SDF No Glow"));
            text.fontSize = 9.0f;
            text.alignment = TextAlignmentOptions.Right;
            text.text = "Performance";
            text.enableAutoSizing = true;
            text.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);
            text.GetComponent<RectTransform>().localPosition = new Vector3(-0.3f, -0.25f, 0.1f);
            text.GetComponent<RectTransform>().sizeDelta = new Vector2(75.0f, 25.0f);

            GameObject graphObj = new GameObject("GraphContainer");
            graphObj.AddComponent<RectTransform>().localPosition = new Vector3(0.0f, 0.45f, 2.275f);
            graphObj.GetComponent<RectTransform>().sizeDelta = new Vector2(1.0f, 0.45f);
            graphObj.transform.Rotate(22.5f, 0, 0, Space.World);
            graphObj.transform.SetParent(panel.transform);
            graphObj.transform.name = "GraphContainer";
            WindowGraph graph = panel.AddComponent<WindowGraph>();
            graph.ShowGraph(energyList, false, true, true);

            StartCoroutine(WaitForMenu());
        }

        #region Monobehaviour Messages
        IEnumerator WaitForMenu() {
            bool loaded = false;
            while (!loaded) {
                if (resultsController == null) resultsController = Resources.FindObjectsOfTypeAll<ResultsViewController>().FirstOrDefault();
                if (resultsController != null) loaded = true;
                else yield return new WaitForSeconds(0.1f);
            }

            yield return new WaitForSeconds(0.1f);
            resultsController.continueButtonPressedEvent += DismissGraph;
            resultsController.restartButtonPressedEvent += DismissGraph;
            Logger.log.Debug("PerformanceMeter menu created successfully");
        }

        void DismissGraph(ResultsViewController vc) {
            if (panel != null) {
                Destroy(panel);
                panel = null;
                resultsController = null;
                scoreController = null;
                energyCounter = null;
                rankCounter = null;
                endActions = null;
                averageHitValue = 0.0f;
                averageHitValueSize = 0;
            }
        }

        public void GetControllers() {
            averageHitValue = 0.0f;
            averageHitValueSize = 0;
            energyList.Clear();
            if (PluginConfig.Instance.GetMode() == PluginConfig.MeasurementMode.Energy) energyList.Add(0.5f);

            scoreController = Resources.FindObjectsOfTypeAll<ScoreController>().LastOrDefault();
            energyCounter = Resources.FindObjectsOfTypeAll<GameEnergyCounter>().FirstOrDefault();
            rankCounter = Resources.FindObjectsOfTypeAll<RelativeScoreAndImmediateRankCounter>().FirstOrDefault();
            endActions = Resources.FindObjectsOfTypeAll<StandardLevelGameplayManager>().FirstOrDefault();
            if (endActions == null) endActions = Resources.FindObjectsOfTypeAll<MissionLevelGameplayManager>().FirstOrDefault();

            if (scoreController != null && energyCounter != null && rankCounter != null && endActions != null) {
                scoreController.noteWasCutEvent += NoteHit;
                scoreController.noteWasMissedEvent += NoteMiss;
                endActions.levelFinishedEvent += LevelFinished;
                endActions.levelFailedEvent += LevelFinished;
                Logger.log.Debug("PerformanceMeter reloaded successfully");
            } else {
                Logger.log.Error("Could not reload PerformanceMeter");
                scoreController = null;
                energyCounter = null;
                rankCounter = null;
                endActions = null;
                averageHitValue = 0.0f;
                averageHitValueSize = 0;
            }
        }

        private void RecordHitValue(CutScoreBuffer score) {
            float newEnergy;
            switch (PluginConfig.Instance.GetMode()) {
                case PluginConfig.MeasurementMode.Energy: newEnergy = energyCounter.energy; break;
                case PluginConfig.MeasurementMode.PercentModified: newEnergy = (float)scoreController.prevFrameModifiedScore / (float)scoreController.immediateMaxPossibleRawScore; break;
                case PluginConfig.MeasurementMode.PercentRaw: newEnergy = rankCounter.relativeScore; break;
                case PluginConfig.MeasurementMode.CutValue: if (score == null) return; newEnergy = score.scoreWithMultiplier / 115.0f; break;
                case PluginConfig.MeasurementMode.AvgCutValue:
                    if (score == null) return;
                    if (averageHitValueSize == 0) { averageHitValue = score.scoreWithMultiplier / 115.0f; averageHitValueSize++; }
                    else averageHitValue = ((averageHitValue * averageHitValueSize) + score.scoreWithMultiplier / 115.0f) / ++averageHitValueSize;
                    newEnergy = averageHitValue;
                    break;
                default: Logger.log.Error("An invalid mode was specified! PerformanceMeter will not record scores, resulting in a blank graph. Check the readme for the valid modes."); return;
            }
            energyList.Add(newEnergy);
        }

        private void NoteHit(NoteData data, NoteCutInfo info, int score) {
            (new CutScoreBuffer(info, 1)).didFinishEvent += RecordHitValue;
        }

        private void NoteMiss(NoteData data, int score) {
            RecordHitValue(null);
        }

        private void LevelFinished() {
            if (scoreController != null && energyCounter != null && rankCounter != null && endActions != null) levelOk = true;
        }

        /// <summary>
        /// Only ever called once, mainly used to initialize variables.
        /// </summary>
        private void Awake() {
            // For this particular MonoBehaviour, we only want one instance to exist at any time, so store a reference to it in a static property
            //   and destroy any that are created while one already exists.
            if (instance != null) {
                Logger.log?.Warn($"Instance of {this.GetType().Name} already exists, destroying.");
                DestroyImmediate(this);
                return;
            }
            DontDestroyOnLoad(this); // Don't destroy this object on scene changes
            instance = this;
            Logger.log?.Debug($"{name}: Awake()");
        }
      
        /// <summary>
        /// Called when the script is being destroyed.
        /// </summary>
        private void OnDestroy() {
            Logger.log?.Debug($"{name}: OnDestroy()");
            instance = null; // This MonoBehaviour is being destroyed, so set the static instance property to null.
        }
        #endregion
    }
}
