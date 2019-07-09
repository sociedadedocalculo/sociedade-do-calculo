#if !(UNITY_4_3 || UNITY_4_5)
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// Controls for UnityUIDialogueUI's alert message.
    /// </summary>
    [System.Serializable]
    public class UnityUIAlertControls : AbstractUIAlertControls
    {

        /// <summary>
        /// The panel containing the alert controls. A panel is optional, but you may want one
        /// so you can include a background image, panel-wide effects, etc.
        /// </summary>
        [Tooltip("Optional panel containing the alert line; can contain other doodads and effects, too")]
        public UnityEngine.UI.Graphic panel;

        /// <summary>
        /// The label used to show the alert message text.
        /// </summary>
        [Tooltip("Shows the alert message text")]
        public UnityEngine.UI.Text line;

        /// <summary>
        /// Optional continue button to close the alert immediately.
        /// </summary>
        [Tooltip("Optional continue button; configure OnClick to invoke dialogue UI's OnContinue method")]
        public UnityEngine.UI.Button continueButton;

        [Serializable]
        public class AnimationTransitions
        {
            public string showTrigger = "Show";
            public string hideTrigger = "Hide";
        }

        [Tooltip("Wait for previous alerts to finish before showing new alert; if unticked, new alerts replace old")]
        public bool queueAlerts = false;

        [Tooltip("Wait for the previous alert's Hide animation to finish before showing the next queued alert")]
        public bool waitForHideAnimation = false;

        [Tooltip("Optional animation transitions; panel should have an Animator")]
        public AnimationTransitions animationTransitions = new AnimationTransitions();

        private bool isVisible = false;

        private bool isHiding = false;

        /// <summary>
        /// Is an alert currently showing?
        /// </summary>
        /// <value>
        /// <c>true</c> if showing; otherwise, <c>false</c>.
        /// </value>
        public override bool IsVisible { get { return isVisible; } }

        public bool IsHiding { get { return isHiding; } }

        private UIShowHideController showHideController = null;

        /// <summary>
        /// Sets the alert controls active. If a hide animation is available, this method
        /// depends on the hide animation to hide the controls.
        /// </summary>
        /// <param name='value'>
        /// <c>true</c> for active.
        /// </param>
        public override void SetActive(bool value)
        {
            try
            {
                if (value == true)
                {
                    ShowPanel();
                }
                else
                {
                    HidePanel();
                }
            }
            finally
            {
                isVisible = value;
            }

        }

        private void ShowPanel()
        {
            ShowControls();
            CheckShowHideController();
            showHideController.ClearTrigger(animationTransitions.hideTrigger);
            showHideController.Show(animationTransitions.showTrigger, false, null);
            isVisible = true;
        }

        private void HidePanel()
        {
            CheckShowHideController();
            showHideController.ClearTrigger(animationTransitions.showTrigger);
            if (isVisible)
            {
                isHiding = true;
                showHideController.Hide(animationTransitions.hideTrigger, HideControls);
            }
            else
            {
                HideControls();
            }
        }

        private void CheckShowHideController()
        {
            if (showHideController == null)
            {
                showHideController = new UIShowHideController(null, panel);
            }
        }

        private void ShowControls()
        {
            Tools.SetGameObjectActive(panel, true);
            Tools.SetGameObjectActive(line, true);
        }

        private void HideControls()
        {
            isHiding = false;
            Tools.SetGameObjectActive(panel, false);
            Tools.SetGameObjectActive(line, false);
        }

        /// <summary>
        /// Sets the alert message UI Text.
        /// </summary>
        /// <param name='message'>
        /// Alert message.
        /// </param>
        /// <param name='duration'>
        /// Duration to show message.
        /// </param>
        public override void SetMessage(string message, float duration)
        {
            if (line != null) line.text = FormattedText.Parse(message, DialogueManager.MasterDatabase.emphasisSettings).text;
        }

        /// <summary>
        /// Auto-focuses the continue button. Useful for gamepads.
        /// </summary>
        public void AutoFocus(bool allowStealFocus = true)
        {
            UITools.Select(continueButton, allowStealFocus);
        }

    }

}
#endif